#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GameLogic.Editor
{
    /// <summary>
    /// AnimationClip Transform 关键帧导出工具。
    /// 将指定节点组（Transform 层级路径）的关键帧导出为 JSON。
    /// 支持：
    /// 1. 多选 .anim 文件、FBX 内嵌动画、文件夹（可递归）；
    /// 2. 按「节点组」配置的完整路径（如 Root/Hips）归类输出；
    /// 3. 位置 XYZ 按层级累加为以动画根原点为基准的世界坐标（父级旋转/缩放参与计算），
    ///    欧拉角 XYZ（度）保持相对父节点的局部角度；
    /// 4. Project 窗口右键直接导出。
    /// </summary>
    public class AnimationTransformKeyframeExporter : EditorWindow
    {
        private const string PrefsKey = "GameLogic.Editor.AnimTransformKeyframeExporter.Settings";
        private const string WindowMenu = "Tools/动画/Transform 关键帧导出 (JSON)";
        private const string ContextMenu = "Assets/动画/导出 Transform 关键帧到 JSON";

        private static readonly string[] XyzAxisNames = { "x", "y", "z" };

        #region 数据结构

        [Serializable]
        private class NodeGroup
        {
            public string name = "全部";

            /// <summary>完整 Transform 路径，每行一个（如 Root/Hips）；留空或填 * 表示全部节点</summary>
            public string patterns = string.Empty;

            public bool foldout = true;
        }

        [Serializable]
        private class Settings
        {
            // 源
            public List<string> folders = new List<string>();
            public List<string> files = new List<string>();
            public bool recursive = true;
            public bool foldersFoldout = true;
            public bool filesFoldout = true;

            // 节点组
            public List<NodeGroup> groups = new List<NodeGroup> { new NodeGroup() };
            public bool firstMatchOnly = true;

            // 导出格式
            public int precision = 5;
            public bool indentJson = true;

            // 输出
            public string outputFolder = "Assets/AssetRaws/AnimKeyframes";
            public bool keepSubFolders = false;
            public bool overwrite = true;

            public void Validate()
            {
                folders ??= new List<string>();
                files ??= new List<string>();
                groups ??= new List<NodeGroup>();
                groups.RemoveAll(g => g == null);

                if (string.IsNullOrEmpty(outputFolder))
                    outputFolder = "Assets/AssetRaws/AnimKeyframes";

                precision = Mathf.Clamp(precision, 0, 8);

                if (groups.Count == 0)
                    groups.Add(new NodeGroup());

                foreach (var g in groups)
                {
                    g.patterns ??= string.Empty;
                    g.name ??= string.Empty;
                }
            }
        }

        private class ClipEntry
        {
            public string Key;
            public string AssetPath;
            public AnimationClip Clip;
        }

        private enum TransformCurveKind
        {
            Unknown = 0,
            Position = 1,
            Quaternion = 2,
            Euler = 3,
            Scale = 4,
        }

        private class NodeCurves
        {
            public string Path;

            /// <summary>仅作为层级中间节点存在（自身没有曲线），用于累加世界坐标</summary>
            public bool IsPlaceholder;

            /// <summary>位置曲线：axis(x/y/z) → Curve</summary>
            public readonly Dictionary<string, AnimationCurve> Position =
                new Dictionary<string, AnimationCurve>(StringComparer.Ordinal);

            /// <summary>欧拉角曲线：axis(x/y/z) → Curve</summary>
            public readonly Dictionary<string, AnimationCurve> Euler =
                new Dictionary<string, AnimationCurve>(StringComparer.Ordinal);

            /// <summary>缩放曲线：axis(x/y/z) → Curve（参与世界坐标换算，不导出）</summary>
            public readonly Dictionary<string, AnimationCurve> Scale =
                new Dictionary<string, AnimationCurve>(StringComparer.Ordinal);

            /// <summary>四元数曲线：axis(x/y/z/w) → Curve，用于在缺少欧拉角曲线时换算欧拉角</summary>
            public readonly Dictionary<string, AnimationCurve> Quaternion =
                new Dictionary<string, AnimationCurve>(StringComparer.Ordinal);

            /// <summary>局部欧拉角关键帧（度）：axis(x/y/z) → keys</summary>
            public Dictionary<string, List<Keyframe>> EulerKeys;

            /// <summary>本节点（含所有祖先）的关键时间点</summary>
            public List<float> Times;

            /// <summary>换算到世界空间后的位置关键帧</summary>
            public List<Keyframe> WorldX;
            public List<Keyframe> WorldY;
            public List<Keyframe> WorldZ;
        }

        private class GroupResult
        {
            public string Name;
            public List<NodeCurves> Nodes;
        }

        private class ExportResult
        {
            public int Written;
            public int Skipped;
            public int Nodes;
            public readonly List<string> Files = new List<string>();
            public readonly List<string> Failed = new List<string>();
        }

        /// <summary>
        /// 手写 JSON 输出器：控制精度、缩进与字段裁剪，避免 JsonUtility 无法格式化的问题。
        /// </summary>
        private sealed class JsonWriter
        {
            private readonly StringBuilder _sb = new StringBuilder(8192);
            private readonly List<bool> _hasItems = new List<bool>();
            private readonly bool _pretty;
            private readonly int _digits;
            private int _depth;

            public JsonWriter(bool pretty, int digits)
            {
                _pretty = pretty;
                _digits = Mathf.Clamp(digits, 0, 12);
            }

            public override string ToString() => _sb.ToString();

            public void BeginObject(string name = null) => Begin('{', name);
            public void BeginArray(string name = null) => Begin('[', name);
            public void EndObject() => End('}');
            public void EndArray() => End(']');

            public void Value(string name, int value)
            {
                Separate(name);
                _sb.Append(value.ToString(CultureInfo.InvariantCulture));
            }

            public void Value(string name, bool value)
            {
                Separate(name);
                _sb.Append(value ? "true" : "false");
            }

            public void Value(string name, float value)
            {
                Separate(name);
                _sb.Append(Number(value));
            }

            public void Value(string name, string value)
            {
                Separate(name);
                _sb.Append('"').Append(Escape(value ?? string.Empty)).Append('"');
            }

            private void Begin(char c, string name)
            {
                Separate(name);
                _sb.Append(c);
                _depth++;
                _hasItems.Add(false);
            }

            private void End(char c)
            {
                int last = _hasItems.Count - 1;
                bool had = _hasItems[last];
                _hasItems.RemoveAt(last);
                _depth--;

                if (had && _pretty)
                    _sb.Append('\n').Append(' ', _depth * 2);

                _sb.Append(c);
            }

            private void Separate(string name)
            {
                if (_hasItems.Count > 0)
                {
                    int last = _hasItems.Count - 1;
                    if (_hasItems[last]) _sb.Append(',');
                    _hasItems[last] = true;

                    if (_pretty)
                        _sb.Append('\n').Append(' ', _depth * 2);
                }

                if (name == null) return;

                _sb.Append('"').Append(Escape(name)).Append('"').Append(_pretty ? ": " : ":");
            }

            private string Number(float value)
            {
                if (float.IsNaN(value) || float.IsInfinity(value)) return "0";

                double rounded = Math.Round(value, _digits, MidpointRounding.AwayFromZero);
                if (rounded == 0d) return "0";

                string format = _digits > 0 ? "0." + new string('#', _digits) : "0";
                return rounded.ToString(format, CultureInfo.InvariantCulture);
            }

            private static string Escape(string s)
            {
                var sb = new StringBuilder(s.Length + 8);
                foreach (char ch in s)
                {
                    switch (ch)
                    {
                        case '\\': sb.Append("\\\\"); break;
                        case '"': sb.Append("\\\""); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        default:
                            if (ch < 0x20) sb.Append("\\u").Append(((int)ch).ToString("x4"));
                            else sb.Append(ch);
                            break;
                    }
                }
                return sb.ToString();
            }
        }

        #endregion

        #region 窗口状态

        private Settings _settings = new Settings();
        private readonly List<ClipEntry> _clips = new List<ClipEntry>();
        private readonly Dictionary<string, bool> _selected = new Dictionary<string, bool>();

        private Vector2 _windowScroll;
        private Vector2 _folderScroll;
        private Vector2 _fileScroll;
        private Vector2 _clipScroll;

        private string _status = string.Empty;
        private bool _scanDirty = true;

        [MenuItem(WindowMenu, false, 100)]
        public static void ShowWindow()
        {
            var win = GetWindow<AnimationTransformKeyframeExporter>("Transform 关键帧导出");
            win.minSize = new Vector2(620, 520);
            win.Show();
        }

        private void OnEnable()
        {
            _settings = LoadSettings();
            _scanDirty = true;
        }

        private void OnDisable()
        {
            SaveSettings(_settings);
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            _windowScroll = EditorGUILayout.BeginScrollView(_windowScroll);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("AnimationClip → Transform 关键帧 JSON", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "导出 AnimationClip 中指定节点（Transform）的关键帧为 JSON。\n" +
                "展开「文件夹组 / 文件组」抽屉后可直接把 Project 中的文件夹或 .anim / FBX 拖入；\n" +
                "节点按「节点组」配置的完整路径（如 Root/Hips）归类输出。",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            DrawSourceSection();
            EditorGUILayout.Space(6);
            DrawGroupSection();
            EditorGUILayout.Space(6);
            DrawOptionSection();
            if (EditorGUI.EndChangeCheck())
                MarkDirty();

            EditorGUILayout.Space(6);
            DrawScanSection();
            EditorGUILayout.Space(6);
            DrawExportSection();
            EditorGUILayout.Space(6);

            EditorGUILayout.EndScrollView();
        }

        private void DrawSourceSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("源（文件夹 / 指定文件）", EditorStyles.boldLabel);

                DrawSourceDrawer(ref _settings.foldersFoldout, "文件夹组", _settings.folders.Count, true);

                EditorGUILayout.Space(4);

                DrawSourceDrawer(ref _settings.filesFoldout, "文件组", _settings.files.Count, false);
            }
        }

        /// <summary>
        /// 抽屉式的文件夹组 / 文件组：折叠时只显示标题栏，展开后可拖拽资源进来。
        /// </summary>
        private void DrawSourceDrawer(ref bool foldout, string title, int count, bool folderMode)
        {
            List<string> list = folderMode ? _settings.folders : _settings.files;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    foldout = EditorGUILayout.Foldout(foldout, $"{title}（{count}）", true);
                    GUILayout.FlexibleSpace();

                    if (folderMode)
                    {
                        if (GUILayout.Button("添加选中文件夹", EditorStyles.miniButtonLeft, GUILayout.Width(100)))
                            AddSelectedFolders();
                        if (GUILayout.Button("…", EditorStyles.miniButtonMid, GUILayout.Width(24)))
                            AddFolderViaPanel();
                    }
                    else
                    {
                        if (GUILayout.Button("添加选中文件", EditorStyles.miniButtonLeft, GUILayout.Width(100)))
                            AddSelectedFiles();
                        if (GUILayout.Button("…", EditorStyles.miniButtonMid, GUILayout.Width(24)))
                            AddFileViaPanel();
                    }

                    using (new EditorGUI.DisabledScope(list.Count == 0))
                    {
                        if (GUILayout.Button("清空", EditorStyles.miniButtonRight, GUILayout.Width(44)))
                        {
                            list.Clear();
                            MarkDirty();
                        }
                    }
                }

                if (!foldout) return;

                Rect dropArea = GUILayoutUtility.GetRect(0f, 30f, GUILayout.ExpandWidth(true));
                bool hover = IsDragHover(dropArea, folderMode);

                GUI.Box(dropArea,
                    folderMode ? "把 Project 中的文件夹拖到这里" : "把 Project 中的 .anim / FBX 拖到这里",
                    EditorStyles.helpBox);

                if (hover)
                    EditorGUI.DrawRect(dropArea, new Color(0.24f, 0.52f, 0.9f, 0.18f));

                HandleDragAndDrop(dropArea, folderMode);

                Vector2 scroll = folderMode ? _folderScroll : _fileScroll;
                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(folderMode ? 90f : 120f));

                if (list.Count == 0)
                {
                    EditorGUILayout.LabelField("（空）", EditorStyles.miniLabel);
                }
                else
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(list[i], EditorStyles.miniLabel);
                            GUILayout.FlexibleSpace();

                            if (folderMode)
                            {
                                if (GUILayout.Button("定位", EditorStyles.miniButtonLeft, GUILayout.Width(38)))
                                {
                                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(list[i]);
                                    Selection.activeObject = obj;
                                    EditorGUIUtility.PingObject(obj);
                                }
                            }

                            if (GUILayout.Button("×", EditorStyles.miniButtonRight, GUILayout.Width(22)))
                            {
                                list.RemoveAt(i);
                                MarkDirty();
                            }
                        }
                    }
                }

                EditorGUILayout.EndScrollView();

                if (folderMode)
                    _settings.recursive = EditorGUILayout.ToggleLeft("文件夹递归（包含所有子文件夹）", _settings.recursive);
                else
                    EditorGUILayout.LabelField("支持 .anim 文件与 FBX 内嵌动画", EditorStyles.miniLabel);

                if (folderMode) _folderScroll = scroll;
                else _fileScroll = scroll;
            }
        }

        private void DrawGroupSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("节点组（Transform 层级路径）", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("添加分组", EditorStyles.miniButtonLeft, GUILayout.Width(70)))
                    {
                        _settings.groups.Add(new NodeGroup
                        {
                            name = "分组" + (_settings.groups.Count + 1),
                            patterns = string.Empty,
                        });
                        MarkDirty();
                    }
                    if (GUILayout.Button("添加“全部”", EditorStyles.miniButtonRight, GUILayout.Width(80)))
                    {
                        _settings.groups.Add(new NodeGroup { name = "全部", patterns = string.Empty });
                        MarkDirty();
                    }
                }

                EditorGUILayout.LabelField("每行一个完整路径，如 Root/Hips；留空表示该组包含全部节点", EditorStyles.miniLabel);

                for (int i = 0; i < _settings.groups.Count; i++)
                {
                    NodeGroup g = _settings.groups[i];

                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            g.foldout = EditorGUILayout.Foldout(
                                g.foldout,
                                string.IsNullOrEmpty(g.name) ? "(未命名)" : g.name,
                                true);

                            GUILayout.FlexibleSpace();

                            using (new EditorGUI.DisabledScope(i == 0))
                            {
                                if (GUILayout.Button("↑", EditorStyles.miniButtonLeft, GUILayout.Width(22)))
                                {
                                    (_settings.groups[i - 1], _settings.groups[i]) = (_settings.groups[i], _settings.groups[i - 1]);
                                    MarkDirty();
                                }
                            }
                            using (new EditorGUI.DisabledScope(i == _settings.groups.Count - 1))
                            {
                                if (GUILayout.Button("↓", EditorStyles.miniButtonMid, GUILayout.Width(22)))
                                {
                                    (_settings.groups[i + 1], _settings.groups[i]) = (_settings.groups[i], _settings.groups[i + 1]);
                                    MarkDirty();
                                }
                            }
                            if (GUILayout.Button("×", EditorStyles.miniButtonRight, GUILayout.Width(22)))
                            {
                                _settings.groups.RemoveAt(i);
                                MarkDirty();
                                break;
                            }
                        }

                        if (!g.foldout) continue;

                        using (new EditorGUI.IndentLevelScope())
                        {
                            g.name = EditorGUILayout.TextField("分组名", g.name);
                            EditorGUILayout.LabelField("完整路径（每行一个，如 Root/Hips）", EditorStyles.miniLabel);
                            g.patterns = EditorGUILayout.TextArea(g.patterns ?? string.Empty, GUILayout.Height(44));
                        }
                    }
                }

                _settings.firstMatchOnly = EditorGUILayout.ToggleLeft(
                    "同一节点只归属首个匹配的分组（避免数据重复）", _settings.firstMatchOnly);
            }
        }

        private void DrawOptionSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("格式", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("位置为世界坐标；欧拉角为相对父节点的局部角度（度）", EditorStyles.miniLabel);

                _settings.precision = EditorGUILayout.IntSlider("数值精度（小数位）", _settings.precision, 0, 8);
                _settings.indentJson = EditorGUILayout.ToggleLeft("JSON 缩进格式化（关闭则输出单行）", _settings.indentJson);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("输出设置", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("输出文件夹", GUILayout.Width(70));
                    _settings.outputFolder = EditorGUILayout.TextField(_settings.outputFolder);
                    if (GUILayout.Button("…", GUILayout.Width(28)))
                    {
                        string start = ToAbsolute(_settings.outputFolder);
                        string abs = EditorUtility.OpenFolderPanel("选择输出文件夹", start, string.Empty);
                        if (!string.IsNullOrEmpty(abs))
                            _settings.outputFolder = FromAbsolute(abs);
                    }
                }

                _settings.keepSubFolders = EditorGUILayout.ToggleLeft(
                    "按源文件夹保留相对子目录", _settings.keepSubFolders);
                _settings.overwrite = EditorGUILayout.ToggleLeft(
                    "覆盖已存在的同名 JSON（不勾选则跳过）", _settings.overwrite);
            }
        }

        private void DrawScanSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"扫描结果：{_clips.Count} 个 AnimationClip", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("重新扫描", GUILayout.Width(80)))
                        Scan();
                }

                if (_clips.Count > 0)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("全选", EditorStyles.miniButtonLeft, GUILayout.Width(60)))
                            SetAllSelected(true);
                        if (GUILayout.Button("全不选", EditorStyles.miniButtonMid, GUILayout.Width(60)))
                            SetAllSelected(false);

                        int selectedCount = CountSelected();
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.LabelField($"{selectedCount} / {_clips.Count}", GUILayout.Width(80));
                    }

                    _clipScroll = EditorGUILayout.BeginScrollView(_clipScroll, GUILayout.Height(200));
                    foreach (ClipEntry entry in _clips)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            bool cur = _selected.TryGetValue(entry.Key, out bool v) && v;
                            bool next = EditorGUILayout.Toggle(cur, GUILayout.Width(18));
                            if (next != cur) _selected[entry.Key] = next;

                            EditorGUILayout.LabelField(entry.Clip.name, EditorStyles.miniLabel, GUILayout.Width(180));
                            var style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.6f, 0.65f, 0.7f) } };
                            EditorGUILayout.LabelField(entry.AssetPath, style);
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    EditorGUILayout.LabelField("（未扫描）点击“重新扫描”收集源中的 AnimationClip", EditorStyles.miniLabel);
                }
            }
        }

        private void DrawExportSection()
        {
            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, MessageType.None);

            int selectedCount = CountSelected();

            using (new EditorGUI.DisabledScope(selectedCount == 0))
            {
                if (GUILayout.Button($"导出选中关键帧（{selectedCount}）", GUILayout.Height(36)))
                    ExportSelected();
            }
        }

        #endregion

        #region 源收集

        private static bool IsSupportedAsset(string path)
        {
            return path.EndsWith(".anim", StringComparison.OrdinalIgnoreCase)
                   || path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase);
        }

        private void AddSelectedFolders()
        {
            int added = 0;
            foreach (var obj in Selection.objects)
            {
                string p = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(p) || !AssetDatabase.IsValidFolder(p)) continue;
                if (_settings.folders.Contains(p)) continue;
                _settings.folders.Add(p);
                added++;
            }
            if (added == 0) _status = "没有在 Project 中选中文件夹。";
            MarkDirty();
        }

        private void AddSelectedFiles()
        {
            int added = 0;
            foreach (var obj in Selection.objects)
            {
                string p = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(p) || !IsSupportedAsset(p)) continue;
                if (_settings.files.Contains(p)) continue;
                _settings.files.Add(p);
                added++;
            }
            if (added == 0) _status = "没有在 Project 中选中 .anim / FBX 文件。";
            MarkDirty();
        }

        private void AddFolderViaPanel()
        {
            string abs = EditorUtility.OpenFolderPanel("添加源文件夹", Directory.GetCurrentDirectory(), string.Empty);
            if (string.IsNullOrEmpty(abs)) return;

            string assetPath = ToAssetPath(abs);
            if (string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.DisplayDialog("错误", "只能添加 Assets 目录下的文件夹。", "确定");
                return;
            }

            if (!_settings.folders.Contains(assetPath))
                _settings.folders.Add(assetPath);
            MarkDirty();
        }

        private void AddFileViaPanel()
        {
            string start = _settings.folders.Count > 0
                ? Path.GetFullPath(_settings.folders[0])
                : Directory.GetCurrentDirectory();

            string abs = EditorUtility.OpenFilePanelWithFilters(
                "添加 AnimationClip 文件", start, new[] { "AnimationClip", "anim,fbx,FBX" });
            if (string.IsNullOrEmpty(abs)) return;

            string assetPath = ToAssetPath(abs);
            if (string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.DisplayDialog("错误", "只能添加 Assets 目录下的文件。", "确定");
                return;
            }

            if (_settings.files.Contains(assetPath) == false)
                _settings.files.Add(assetPath);
            MarkDirty();
        }

        /// <summary>当前拖拽是否停留在指定抽屉的投放区，并携带该区接受的资源。</summary>
        private static bool IsDragHover(Rect area, bool folderMode)
        {
            Event evt = Event.current;
            if (evt == null) return false;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return false;
            if (!area.Contains(evt.mousePosition)) return false;

            return GetDraggedPaths(folderMode).Count > 0;
        }

        /// <summary>取拖拽内容中该抽屉可接受的资源（文件夹组只收文件夹，文件组只收 .anim / FBX）。</summary>
        private static List<string> GetDraggedPaths(bool folderMode)
        {
            var result = new List<string>();

            string[] paths = DragAndDrop.paths;
            if (paths == null) return result;

            for (int i = 0; i < paths.Length; i++)
            {
                string path = (paths[i] ?? string.Empty).Replace('\\', '/');
                if (string.IsNullOrEmpty(path)) continue;

                if (folderMode)
                {
                    if (AssetDatabase.IsValidFolder(path) && !result.Contains(path))
                        result.Add(path);
                }
                else if (IsSupportedAsset(path) && !result.Contains(path))
                {
                    result.Add(path);
                }
            }

            return result;
        }

        private void HandleDragAndDrop(Rect area, bool folderMode)
        {
            Event evt = Event.current;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return;
            if (!area.Contains(evt.mousePosition)) return;

            List<string> paths = GetDraggedPaths(folderMode);
            if (paths.Count == 0) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type != EventType.DragPerform) return;

            DragAndDrop.AcceptDrag();

            List<string> target = folderMode ? _settings.folders : _settings.files;
            for (int i = 0; i < paths.Count; i++)
            {
                if (!target.Contains(paths[i]))
                    target.Add(paths[i]);
            }

            MarkDirty();
            evt.Use();
        }

        private static List<string> CollectAssets(
            IEnumerable<string> folders, IEnumerable<string> files, bool recursive)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            SearchOption search = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            if (folders != null)
            {
                foreach (string raw in folders)
                {
                    string folder = (raw ?? string.Empty).Replace('\\', '/').TrimEnd('/');
                    if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder)) continue;

                    string absFolder = Path.GetFullPath(folder);
                    if (!Directory.Exists(absFolder)) continue;

                    foreach (string pattern in new[] { "*.anim", "*.fbx" })
                    {
                        foreach (string abs in Directory.GetFiles(absFolder, pattern, search))
                        {
                            string assetPath = ToAssetPath(abs);
                            if (!string.IsNullOrEmpty(assetPath)) set.Add(assetPath);
                        }
                    }
                }
            }

            if (files != null)
            {
                foreach (string raw in files)
                {
                    string path = (raw ?? string.Empty).Replace('\\', '/');
                    if (string.IsNullOrEmpty(path)) continue;
                    if (File.Exists(Path.GetFullPath(path))) set.Add(path);
                }
            }

            return set.OrderBy(p => p, StringComparer.Ordinal).ToList();
        }

        private static List<AnimationClip> LoadClips(string assetPath)
        {
            var result = new List<AnimationClip>();

            if (assetPath.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
                if (clip != null) result.Add(clip);
                return result;
            }

            if (assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                {
                    if (obj is AnimationClip clip &&
                        !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                    {
                        result.Add(clip);
                    }
                }
            }

            return result;
        }

        private static List<ClipEntry> BuildClipEntries(IEnumerable<string> assetPaths)
        {
            var entries = new List<ClipEntry>();
            foreach (string assetPath in assetPaths)
            {
                int index = 0;
                foreach (AnimationClip clip in LoadClips(assetPath))
                {
                    entries.Add(new ClipEntry
                    {
                        AssetPath = assetPath,
                        Clip = clip,
                        Key = $"{assetPath}::{clip.name}#{index++}",
                    });
                }
            }
            return entries;
        }

        #endregion

        #region 扫描

        private void MarkDirty()
        {
            _scanDirty = true;
            SaveSettings(_settings);
        }

        private void Scan()
        {
            _clips.Clear();
            _selected.Clear();

            List<string> assets = CollectAssets(_settings.folders, _settings.files, _settings.recursive);

            try
            {
                for (int i = 0; i < assets.Count; i++)
                {
                    EditorUtility.DisplayProgressBar(
                        "扫描 AnimationClip", assets[i], (float)i / Mathf.Max(1, assets.Count));

                    int index = 0;
                    foreach (AnimationClip clip in LoadClips(assets[i]))
                    {
                        var entry = new ClipEntry
                        {
                            AssetPath = assets[i],
                            Clip = clip,
                            Key = $"{assets[i]}::{clip.name}#{index++}",
                        };
                        _clips.Add(entry);
                        _selected[entry.Key] = true;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            _scanDirty = false;
            _status = $"扫描完成：{assets.Count} 个资源，{_clips.Count} 个 AnimationClip。";
            Repaint();
        }

        private int CountSelected()
        {
            return _clips.Count(c => _selected.TryGetValue(c.Key, out bool v) && v);
        }

        private void SetAllSelected(bool value)
        {
            foreach (ClipEntry entry in _clips)
                _selected[entry.Key] = value;
        }

        #endregion

        #region 导出

        private void ExportSelected()
        {
            if (_scanDirty || _clips.Count == 0)
                Scan();

            var entries = _clips.Where(c => _selected.TryGetValue(c.Key, out bool v) && v).ToList();
            if (entries.Count == 0) return;

            if (string.IsNullOrEmpty(_settings.outputFolder))
            {
                EditorUtility.DisplayDialog("错误", "请先设置输出文件夹。", "确定");
                return;
            }

            SaveSettings(_settings);
            ExportResult result = Export(entries, _settings);

            _status = BuildSummary(result);
            foreach (string failed in result.Failed)
                Debug.LogWarning("[AnimTransformKeyframeExporter] " + failed);

            EditorUtility.DisplayDialog(
                "导出完成", _status + "\n输出目录：" + _settings.outputFolder, "确定");
            Repaint();
        }

        private static ExportResult Export(List<ClipEntry> entries, Settings settings)
        {
            var result = new ExportResult();
            string root = (settings.outputFolder ?? string.Empty).Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(root)) return result;

            var usedNames = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            try
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    ClipEntry entry = entries[i];

                    if (EditorUtility.DisplayCancelableProgressBar(
                            "导出 Transform 关键帧",
                            $"{entry.Clip.name}  ({i + 1}/{entries.Count})",
                            (float)i / entries.Count))
                        break;

                    try
                    {
                        string outFolder = root;
                        if (settings.keepSubFolders)
                        {
                            string rel = GetRelativeSubFolder(entry.AssetPath, settings);
                            if (!string.IsNullOrEmpty(rel)) outFolder = outFolder + "/" + rel;
                        }
                        EnsureOutputFolder(outFolder);

                        if (!usedNames.TryGetValue(outFolder, out HashSet<string> used))
                        {
                            used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            usedNames[outFolder] = used;
                        }

                        string baseName = SanitizeFileName(entry.Clip.name);
                        string fileName = baseName;
                        int suffix = 1;
                        while (!used.Add(fileName))
                            fileName = baseName + "_" + suffix++;

                        string assetPath = outFolder + "/" + fileName + ".json";
                        string absPath = Path.GetFullPath(assetPath);

                        if (File.Exists(absPath) && !settings.overwrite)
                        {
                            result.Skipped++;
                            continue;
                        }

                        string json = BuildClipJson(entry, settings, out int nodeCount);
                        if (string.IsNullOrEmpty(json))
                        {
                            result.Skipped++;
                            result.Failed.Add(entry.AssetPath + " -> 没有匹配到任何 Transform 关键帧（检查节点组规则）");
                            continue;
                        }

                        File.WriteAllText(absPath, json + Environment.NewLine, new UTF8Encoding(false));

                        if (IsAssetsPath(assetPath))
                            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

                        result.Written++;
                        result.Nodes += nodeCount;
                        result.Files.Add(assetPath);
                    }
                    catch (Exception e)
                    {
                        result.Failed.Add(entry.AssetPath + " -> " + e.Message);
                        Debug.LogException(e);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();
            return result;
        }

        private static string BuildSummary(ExportResult result)
        {
            string summary = $"完成：输出 {result.Written} 个 JSON，共 {result.Nodes} 个节点；跳过 {result.Skipped} 个。";
            if (result.Failed.Count > 0)
                summary += $"\n失败 {result.Failed.Count} 个（详见 Console）。";
            return summary;
        }

        private static string BuildClipJson(ClipEntry entry, Settings settings, out int nodeCount)
        {
            nodeCount = 0;

            Dictionary<string, NodeCurves> nodeMap = CollectNodeCurves(entry.Clip);
            if (nodeMap.Count == 0) return null;

            BuildWorldKeys(nodeMap);

            List<GroupResult> groups = AssignGroups(nodeMap, settings);
            if (groups.Count == 0) return null;

            AnimationClip clip = entry.Clip;
            var writer = new JsonWriter(settings.indentJson, settings.precision);

            writer.BeginObject();
            writer.Value("Name", clip.name);
            writer.Value("Length", clip.length);

            writer.BeginArray("Groups");
            foreach (GroupResult group in groups)
            {
                writer.BeginObject();
                writer.Value("Name", group.Name);

                writer.BeginArray("Nodes");
                foreach (NodeCurves node in group.Nodes)
                {
                    WriteNode(writer, node);
                    nodeCount++;
                }
                writer.EndArray();

                writer.EndObject();
            }
            writer.EndArray();

            writer.EndObject();
            return writer.ToString();
        }

        private static void WriteNode(JsonWriter writer, NodeCurves node)
        {
            writer.BeginObject();
            writer.Value("Path", node.Path);

            writer.BeginObject("Position");
            WriteKeys(writer, "X", node.WorldX);
            WriteKeys(writer, "Y", node.WorldY);
            WriteKeys(writer, "Z", node.WorldZ);
            writer.EndObject();

            // 欧拉角保持相对父节点的局部角度（父级只用于位置换算），
            // 避免把 Armature 这类导入轴转换的 -90° 混进骨骼角度里。
            if (node.EulerKeys != null && node.EulerKeys.Count > 0)
            {
                writer.BeginObject("Euler");
                WriteEulerAxis(writer, "x", "X", node.EulerKeys);
                WriteEulerAxis(writer, "y", "Y", node.EulerKeys);
                WriteEulerAxis(writer, "z", "Z", node.EulerKeys);
                writer.EndObject();
            }

            writer.EndObject();
        }

        private static void WriteEulerAxis(
            JsonWriter writer, string axisKey, string outputName, Dictionary<string, List<Keyframe>> axes)
        {
            if (axes.TryGetValue(axisKey, out List<Keyframe> keys) && keys != null && keys.Count > 0)
            {
                WriteKeys(writer, outputName, keys);
                return;
            }

            writer.BeginArray(outputName);
            writer.BeginObject();
            writer.Value("T", 0f);
            writer.Value("V", 0f);
            writer.EndObject();
            writer.EndArray();
        }

        private static void WriteKeys(JsonWriter writer, string name, IList<Keyframe> keys)
        {
            writer.BeginArray(name);
            for (int i = 0; i < keys.Count; i++)
            {
                writer.BeginObject();
                writer.Value("T", keys[i].time);
                writer.Value("V", keys[i].value);
                writer.EndObject();
            }
            writer.EndArray();
        }

        private static Dictionary<string, NodeCurves> CollectNodeCurves(AnimationClip clip)
        {
            var map = new Dictionary<string, NodeCurves>(StringComparer.Ordinal);

            EditorCurveBinding[] bindings;
            try
            {
                bindings = AnimationUtility.GetCurveBindings(clip);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AnimTransformKeyframeExporter] 读取曲线绑定失败：{clip.name}\n{e}");
                return map;
            }

            foreach (EditorCurveBinding binding in bindings)
            {
                if (binding.type != typeof(Transform)) continue;
                if (!TryParseTransformCurve(binding.propertyName, out TransformCurveKind kind, out string axis)) continue;
                if (string.IsNullOrEmpty(axis)) continue;

                AnimationCurve curve;
                try
                {
                    curve = AnimationUtility.GetEditorCurve(clip, binding);
                }
                catch
                {
                    continue;
                }

                if (curve == null || curve.length == 0) continue;

                string path = string.IsNullOrEmpty(binding.path) ? "<Root>" : binding.path;

                if (!map.TryGetValue(path, out NodeCurves node))
                {
                    node = new NodeCurves { Path = path };
                    map[path] = node;
                }

                switch (kind)
                {
                    case TransformCurveKind.Position:
                        if (IsXyzAxis(axis) && !node.Position.ContainsKey(axis))
                            node.Position[axis] = curve;
                        break;

                    case TransformCurveKind.Quaternion:
                        if (IsXyzwAxis(axis) && !node.Quaternion.ContainsKey(axis))
                            node.Quaternion[axis] = curve;
                        break;

                    case TransformCurveKind.Euler:
                        if (IsXyzAxis(axis) && !node.Euler.ContainsKey(axis))
                            node.Euler[axis] = curve;
                        break;

                    case TransformCurveKind.Scale:
                        if (IsXyzAxis(axis) && !node.Scale.ContainsKey(axis))
                            node.Scale[axis] = curve;
                        break;
                }
            }

            foreach (NodeCurves node in map.Values)
                node.EulerKeys = BuildEuler(node);

            return map;
        }

        private static bool IsXyzAxis(string axis)
        {
            return axis == "x" || axis == "y" || axis == "z";
        }

        private static bool IsXyzwAxis(string axis)
        {
            return axis == "x" || axis == "y" || axis == "z" || axis == "w";
        }

        /// <summary>
        /// 取欧拉角（度）：优先使用欧拉角曲线，
        /// 否则用四元数曲线在关键时间点还原四元数后换算。
        /// 返回 axis(x/y/z) → 关键帧列表；没有任何旋转曲线时返回 null。
        /// </summary>
        private static Dictionary<string, List<Keyframe>> BuildEuler(NodeCurves node)
        {
            if (node.Euler.Count > 0)
            {
                var fromCurves = new Dictionary<string, List<Keyframe>>(StringComparer.Ordinal);
                for (int i = 0; i < XyzAxisNames.Length; i++)
                {
                    string axis = XyzAxisNames[i];

                    if (node.Euler.TryGetValue(axis, out AnimationCurve curve) &&
                        curve != null && curve.length > 0)
                    {
                        var keys = new List<Keyframe>(curve.keys);
                        UnwrapAngles(keys);
                        fromCurves[axis] = keys;
                    }
                    else
                    {
                        fromCurves[axis] = new List<Keyframe> { new Keyframe(0f, 0f) };
                    }
                }

                return fromCurves;
            }

            if (node.Quaternion.Count == 0) return null;

            var times = new List<float>();
            foreach (AnimationCurve curve in node.Quaternion.Values)
            {
                if (curve == null) continue;

                Keyframe[] quatKeys = curve.keys;
                for (int i = 0; i < quatKeys.Length; i++)
                    times.Add(quatKeys[i].time);
            }

            if (times.Count == 0) return null;
            times.Sort();

            var xs = new List<Keyframe>(times.Count);
            var ys = new List<Keyframe>(times.Count);
            var zs = new List<Keyframe>(times.Count);

            float lastTime = float.NegativeInfinity;
            float lastX = 0f, lastY = 0f, lastZ = 0f;

            for (int i = 0; i < times.Count; i++)
            {
                float time = times[i];

                // 各轴关键时间点合并后去重
                if (time - lastTime < 1e-4f) continue;
                lastTime = time;

                float qx = SampleAxis(node.Quaternion, "x", time, 0f);
                float qy = SampleAxis(node.Quaternion, "y", time, 0f);
                float qz = SampleAxis(node.Quaternion, "z", time, 0f);
                float qw = SampleAxis(node.Quaternion, "w", time, 1f);

                float magnitude = Mathf.Sqrt(qx * qx + qy * qy + qz * qz + qw * qw);
                if (magnitude < 1e-6f) continue;

                var rotation = new Quaternion(qx / magnitude, qy / magnitude, qz / magnitude, qw / magnitude);
                Vector3 euler = rotation.eulerAngles;

                float ex = euler.x;
                float ey = euler.y;
                float ez = euler.z;

                if (xs.Count > 0)
                {
                    ex = UnwrapAngle(lastX, ex);
                    ey = UnwrapAngle(lastY, ey);
                    ez = UnwrapAngle(lastZ, ez);
                }

                xs.Add(new Keyframe(time, ex));
                ys.Add(new Keyframe(time, ey));
                zs.Add(new Keyframe(time, ez));

                lastX = ex;
                lastY = ey;
                lastZ = ez;
            }

            if (xs.Count == 0) return null;

            return new Dictionary<string, List<Keyframe>>(StringComparer.Ordinal)
            {
                { "x", xs },
                { "y", ys },
                { "z", zs },
            };
        }

        /// <summary>
        /// 把各节点的局部关键帧按层级累加成以动画根原点为基准的世界坐标与欧拉角。
        /// 父级旋转与缩放都会参与换算（缩放只参与计算，不输出）。
        /// </summary>
        private static void BuildWorldKeys(Dictionary<string, NodeCurves> map)
        {
            // 1. 补全没有曲线的中间层节点，保证层级链完整
            var paths = new List<string>(map.Keys);
            for (int i = 0; i < paths.Count; i++)
            {
                string parent = ParentPath(paths[i]);
                while (!string.IsNullOrEmpty(parent) && !map.ContainsKey(parent))
                {
                    map[parent] = new NodeCurves { Path = parent, IsPlaceholder = true };
                    parent = ParentPath(parent);
                }
            }

            var timesCache = new Dictionary<string, List<float>>(StringComparer.Ordinal);

            // 2. 每个节点在其自身与祖先的关键时间点上采样世界变换
            foreach (NodeCurves node in map.Values)
            {
                if (node.IsPlaceholder) continue;

                node.Times = GetTimes(map, node.Path, timesCache);

                int count = node.Times.Count;
                node.WorldX = new List<Keyframe>(count);
                node.WorldY = new List<Keyframe>(count);
                node.WorldZ = new List<Keyframe>(count);

                for (int i = 0; i < count; i++)
                {
                    float time = node.Times[i];

                    var cache = new Dictionary<string, WorldTransform>(StringComparer.Ordinal);
                    WorldTransform world = SampleWorld(map, node.Path, time, cache);
                    Vector3 position = world.Matrix.GetPosition();

                    node.WorldX.Add(new Keyframe(time, position.x));
                    node.WorldY.Add(new Keyframe(time, position.y));
                    node.WorldZ.Add(new Keyframe(time, position.z));
                }
            }
        }

        private struct WorldTransform
        {
            public Matrix4x4 Matrix;
        }

        /// <summary>递归累加父级变换，得到节点在动画根空间（原点为根）的世界变换。</summary>
        private static WorldTransform SampleWorld(
            Dictionary<string, NodeCurves> map, string path, float time,
            Dictionary<string, WorldTransform> cache)
        {
            if (cache.TryGetValue(path, out WorldTransform cached)) return cached;

            NodeCurves node = map[path];

            var localPosition = new Vector3(
                SampleAxis(node.Position, "x", time, 0f),
                SampleAxis(node.Position, "y", time, 0f),
                SampleAxis(node.Position, "z", time, 0f));

            var localEuler = new Vector3(
                SampleEulerAxis(node.EulerKeys, "x", time),
                SampleEulerAxis(node.EulerKeys, "y", time),
                SampleEulerAxis(node.EulerKeys, "z", time));

            Quaternion localRotation = Quaternion.Euler(localEuler);

            var localScale = new Vector3(
                SampleAxis(node.Scale, "x", time, 1f),
                SampleAxis(node.Scale, "y", time, 1f),
                SampleAxis(node.Scale, "z", time, 1f));

            Matrix4x4 localMatrix = Matrix4x4.TRS(localPosition, localRotation, localScale);

            WorldTransform result;
            string parent = ParentPath(path);

            if (!string.IsNullOrEmpty(parent) && map.ContainsKey(parent))
                result.Matrix = SampleWorld(map, parent, time, cache).Matrix * localMatrix;
            else
                result.Matrix = localMatrix;

            cache[path] = result;
            return result;
        }

        /// <summary>线性采样局部欧拉角（度）。</summary>
        private static float SampleEulerAxis(
            Dictionary<string, List<Keyframe>> axes, string axis, float time)
        {
            if (axes == null || !axes.TryGetValue(axis, out List<Keyframe> keys) || keys == null || keys.Count == 0)
                return 0f;

            if (time <= keys[0].time) return keys[0].value;

            int last = keys.Count - 1;
            if (time >= keys[last].time) return keys[last].value;

            int lo = 0;
            int hi = last;
            while (lo + 1 < hi)
            {
                int mid = (lo + hi) >> 1;
                if (keys[mid].time <= time) lo = mid;
                else hi = mid;
            }

            Keyframe k0 = keys[lo];
            Keyframe k1 = keys[lo + 1];
            float dt = k1.time - k0.time;
            return dt <= 1e-6f ? k1.value : k0.value + (k1.value - k0.value) * ((time - k0.time) / dt);
        }

        /// <summary>自身与所有祖先的关键时间点并集，保证世界坐标插值正确。</summary>
        private static List<float> GetTimes(
            Dictionary<string, NodeCurves> map, string path, Dictionary<string, List<float>> cache)
        {
            if (cache.TryGetValue(path, out List<float> cached)) return cached;

            var times = new List<float>();
            AddOwnTimes(times, map[path]);

            string parent = ParentPath(path);
            if (!string.IsNullOrEmpty(parent) && map.ContainsKey(parent))
                times.AddRange(GetTimes(map, parent, cache));

            times.Sort();

            var result = new List<float>(times.Count);
            for (int i = 0; i < times.Count; i++)
            {
                if (result.Count > 0 && times[i] - result[result.Count - 1] < 1e-4f) continue;
                result.Add(times[i]);
            }

            if (result.Count == 0) result.Add(0f);

            cache[path] = result;
            return result;
        }

        private static void AddOwnTimes(List<float> times, NodeCurves node)
        {
            AddCurveTimes(times, node.Position.Values);
            AddCurveTimes(times, node.Scale.Values);

            if (node.EulerKeys == null) return;

            foreach (List<Keyframe> keys in node.EulerKeys.Values)
            {
                if (keys == null) continue;

                for (int i = 0; i < keys.Count; i++)
                    times.Add(keys[i].time);
            }
        }

        private static void AddCurveTimes(
            List<float> times, Dictionary<string, AnimationCurve>.ValueCollection curves)
        {
            foreach (AnimationCurve curve in curves)
            {
                if (curve == null) continue;

                Keyframe[] keys = curve.keys;
                for (int i = 0; i < keys.Length; i++)
                    times.Add(keys[i].time);
            }
        }

        private static string ParentPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            int slash = path.LastIndexOf('/');
            return slash < 0 ? null : path.Substring(0, slash);
        }

        private static float SampleAxis(
            Dictionary<string, AnimationCurve> curves, string axis, float time, float defaultValue)
        {
            return curves.TryGetValue(axis, out AnimationCurve curve) && curve != null && curve.length > 0
                ? curve.Evaluate(time)
                : defaultValue;
        }

        /// <summary>消除 359° → 1° 这类跨圈跳变，保证线性插值不会绕远路。</summary>
        private static void UnwrapAngles(List<Keyframe> keys)
        {
            for (int i = 1; i < keys.Count; i++)
            {
                Keyframe key = keys[i];
                key.value = UnwrapAngle(keys[i - 1].value, key.value);
                keys[i] = key;
            }
        }

        private static float UnwrapAngle(float previous, float value)
        {
            while (value - previous > 180f) value -= 360f;
            while (value - previous < -180f) value += 360f;
            return value;
        }

        private static bool TryParseTransformCurve(
            string propertyName, out TransformCurveKind kind, out string axis)
        {
            kind = TransformCurveKind.Unknown;
            axis = null;
            if (string.IsNullOrEmpty(propertyName)) return false;

            string head = propertyName;
            int dot = propertyName.LastIndexOf('.');
            if (dot >= 0)
            {
                head = propertyName.Substring(0, dot);
                axis = propertyName.Substring(dot + 1).ToLowerInvariant();
            }

            switch (head)
            {
                case "m_LocalPosition": kind = TransformCurveKind.Position; break;
                case "m_LocalRotation": kind = TransformCurveKind.Quaternion; break;
                case "localEulerAnglesRaw":
                case "localEulerAngles":
                case "m_LocalEulerAngles":
                case "m_LocalEulerAnglesRaw": kind = TransformCurveKind.Euler; break;
                case "m_LocalScale": kind = TransformCurveKind.Scale; break;
                default: return false;
            }

            return true;
        }

        private static List<GroupResult> AssignGroups(
            Dictionary<string, NodeCurves> nodeMap, Settings settings)
        {
            var result = new List<GroupResult>();
            var assigned = new HashSet<string>(StringComparer.Ordinal);

            if (settings.groups == null || settings.groups.Count == 0)
            {
                List<NodeCurves> all = nodeMap.Values
                    .Where(n => !n.IsPlaceholder)
                    .OrderBy(n => n.Path, StringComparer.Ordinal)
                    .ToList();

                if (all.Count > 0)
                    result.Add(new GroupResult { Name = "all", Nodes = all });

                return result;
            }

            foreach (NodeGroup group in settings.groups)
            {
                if (group == null) continue;

                List<string> paths = ParsePaths(group.patterns);

                var nodes = new List<NodeCurves>();
                foreach (KeyValuePair<string, NodeCurves> pair in nodeMap)
                {
                    if (pair.Value.IsPlaceholder) continue;
                    if (settings.firstMatchOnly && assigned.Contains(pair.Key)) continue;
                    if (!MatchPath(pair.Key, paths)) continue;

                    nodes.Add(pair.Value);
                    assigned.Add(pair.Key);
                }

                if (nodes.Count == 0) continue;

                nodes.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
                result.Add(new GroupResult
                {
                    Name = string.IsNullOrEmpty(group.name) ? "group" : group.name,
                    Nodes = nodes,
                });
            }

            return result;
        }

        /// <summary>解析每行一个的完整路径列表（支持 ; 分隔，忽略空行与 # 注释）。</summary>
        private static List<string> ParsePaths(string raw)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(raw)) return list;

            foreach (string line in raw.Split(new[] { '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string path = line.Trim().Replace('\\', '/').TrimEnd('/');
                if (path.Length == 0 || path.StartsWith("#", StringComparison.Ordinal)) continue;
                list.Add(path);
            }

            return list;
        }

        /// <summary>按完整路径匹配（如 Root/Hips）；未配置路径或填 * 表示全部节点。</summary>
        private static bool MatchPath(string path, List<string> paths)
        {
            if (paths == null || paths.Count == 0) return true;

            for (int i = 0; i < paths.Count; i++)
            {
                if (paths[i] == "*") return true;
                if (string.Equals(path, paths[i], StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        private static string GetRelativeSubFolder(string assetPath, Settings settings)
        {
            string dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(dir) || settings.folders == null) return null;

            string best = null;
            foreach (string raw in settings.folders)
            {
                string folder = (raw ?? string.Empty).Replace('\\', '/').TrimEnd('/');
                if (string.IsNullOrEmpty(folder)) continue;

                if (dir.Equals(folder, StringComparison.OrdinalIgnoreCase) ||
                    dir.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
                {
                    if (best == null || folder.Length > best.Length) best = folder;
                }
            }

            if (best == null) return null;
            return dir.Substring(best.Length).TrimStart('/');
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "clip";

            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (char ch in name)
                sb.Append(Array.IndexOf(invalid, ch) >= 0 ? '_' : ch);

            string result = sb.ToString().Trim();
            return string.IsNullOrEmpty(result) ? "clip" : result;
        }

        #endregion

        #region 右键菜单

        [MenuItem(ContextMenu, true)]
        private static bool ExportFromSelectionValidate()
        {
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;
                if (AssetDatabase.IsValidFolder(path) || IsSupportedAsset(path)) return true;
            }
            return false;
        }

        [MenuItem(ContextMenu, false, 2000)]
        private static void ExportFromSelection()
        {
            var folders = new List<string>();
            var files = new List<string>();

            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;

                if (AssetDatabase.IsValidFolder(path)) folders.Add(path);
                else if (IsSupportedAsset(path)) files.Add(path);
            }

            if (folders.Count == 0 && files.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "请在 Project 中选中 .anim / FBX 文件或文件夹。", "确定");
                return;
            }

            Settings settings = LoadSettings();
            List<string> assets = CollectAssets(folders, files, settings.recursive);
            List<ClipEntry> entries = BuildClipEntries(assets);

            if (entries.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "选中项中没有找到 AnimationClip。", "确定");
                return;
            }

            ExportResult result = Export(entries, settings);
            foreach (string failed in result.Failed)
                Debug.LogWarning("[AnimTransformKeyframeExporter] " + failed);

            EditorUtility.DisplayDialog(
                "导出完成",
                BuildSummary(result) + "\n输出目录：" + settings.outputFolder,
                "确定");
        }

        #endregion

        #region 路径与文件

        private static bool IsAssetsPath(string assetPath)
        {
            return assetPath.Equals("Assets", StringComparison.OrdinalIgnoreCase)
                   || assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase);
        }

        private static string ToAssetPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return null;

            absolutePath = absolutePath.Replace('\\', '/').TrimEnd('/');
            string projectRoot = Directory.GetCurrentDirectory().Replace('\\', '/').TrimEnd('/');

            if (!absolutePath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase)) return null;

            string rel = absolutePath.Substring(projectRoot.Length).TrimStart('/');
            if (rel == "Assets" || rel.StartsWith("Assets/", StringComparison.Ordinal))
                return rel;

            return null;
        }

        private static string ToAbsolute(string path)
        {
            if (string.IsNullOrEmpty(path)) return Directory.GetCurrentDirectory();

            string p = path.Replace('\\', '/');
            return IsAssetsPath(p) ? Path.GetFullPath(p) : p;
        }

        private static string FromAbsolute(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return string.Empty;

            string assetPath = ToAssetPath(absolutePath);
            return string.IsNullOrEmpty(assetPath) ? absolutePath.Replace('\\', '/') : assetPath;
        }

        private static void EnsureOutputFolder(string folder)
        {
            folder = (folder ?? string.Empty).Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(folder)) return;

            if (IsAssetsPath(folder))
            {
                EnsureAssetFolder(folder);
                return;
            }

            Directory.CreateDirectory(Path.GetFullPath(folder));
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;

            string[] parts = assetFolder.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        #endregion

        #region 配置持久化

        private static Settings LoadSettings()
        {
            string json = EditorPrefs.GetString(PrefsKey, string.Empty);

            Settings settings = null;
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    settings = JsonUtility.FromJson<Settings>(json);
                }
                catch
                {
                    // 配置损坏时回退到默认值
                }
            }

            settings ??= new Settings();
            settings.Validate();
            return settings;
        }

        private static void SaveSettings(Settings settings)
        {
            if (settings == null) return;
            EditorPrefs.SetString(PrefsKey, JsonUtility.ToJson(settings));
        }

        #endregion
    }
}
#endif
