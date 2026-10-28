using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace GameLogic.Editor
{
    /// <summary>
    /// FBX 动画批量提取工具。
    /// 支持指定多个文件夹（可递归）和多个指定 FBX 文件，
    /// 将 FBX 内嵌的 AnimationClip 提取为独立的 .anim 资源，
    /// 动画文件名与 FBX 文件名保持一致。
    /// </summary>
    public class FBXAnimationExporter : EditorWindow
    {
        private const string PrefsKey = "GameLogic.Editor.FBXAnimationExporter.Settings";

        #region Settings

        [Serializable]
        private class Settings
        {
            public List<string> folders = new List<string>();   // asset 相对路径
            public List<string> files = new List<string>();     // asset 相对路径
            public string targetFolder = "Assets/AssetRaws/Animations";
            public bool recursive = true;
            public bool scanSubFolders = false;
            public bool createAnimController = false;
            public bool overwrite = true;
        }

        private Settings _settings = new Settings();

        private readonly List<string> _fbxList = new List<string>();
        private readonly Dictionary<string, List<string>> _fbxAnimations = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, bool> _selected = new Dictionary<string, bool>();

        private Vector2 _listScroll;
        private Vector2 _folderScroll;
        private string _status = string.Empty;
        private bool _dirty = true;

        [MenuItem("Tools/FBX动画导出器", false, 100)]
        public static void ShowWindow()
        {
            var win = GetWindow<FBXAnimationExporter>("FBX动画导出器");
            win.minSize = new Vector2(560, 620);
            win.Show();
        }

        private void OnEnable()
        {
            LoadSettings();
        }

        private void OnDisable()
        {
            SaveSettings();
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("FBX 动画批量提取", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "提取 FBX 内嵌动画为独立 .anim 文件，动画文件名 = FBX 文件名。\n" +
                "可添加多个文件夹（支持递归）与多个指定 FBX 文件。",
                MessageType.Info);

            DrawSourceSection();
            EditorGUILayout.Space(6);
            DrawOptionSection();
            EditorGUILayout.Space(6);
            DrawScanSection();
            EditorGUILayout.Space(6);
            DrawExportSection();
        }

        private void DrawSourceSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("源（文件夹 / 指定文件）", EditorStyles.boldLabel);

                // 支持把 Project 中的文件夹 / FBX 直接拖进来
                var dropArea = GUILayoutUtility.GetRect(0f, 0f);
                dropArea.height = 1f;
                HandleDragAndDrop(dropArea);

                // ---------- 文件夹 ----------
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("文件夹列表", EditorStyles.miniBoldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("添加选中文件夹", EditorStyles.miniButtonLeft, GUILayout.Width(100)))
                        AddSelectedFolders();
                    if (GUILayout.Button("添加文件夹…", EditorStyles.miniButtonMid, GUILayout.Width(90)))
                        AddFolderViaPanel();
                    if (GUILayout.Button("清空", EditorStyles.miniButtonRight, GUILayout.Width(50)))
                    {
                        _settings.folders.Clear();
                        MarkDirty();
                    }
                }

                _folderScroll = EditorGUILayout.BeginScrollView(_folderScroll, GUILayout.MaxHeight(90));
                if (_settings.folders.Count == 0)
                {
                    EditorGUILayout.LabelField("（空）可直接把 Project 中的文件夹拖到下框", EditorStyles.miniLabel);
                }
                else
                {
                    for (int i = _settings.folders.Count - 1; i >= 0; i--)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(_settings.folders[i], EditorStyles.miniLabel);
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("定位", EditorStyles.miniButtonLeft, GUILayout.Width(38)))
                            {
                                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_settings.folders[i]);
                                Selection.activeObject = obj;
                                EditorGUIUtility.PingObject(obj);
                            }
                            if (GUILayout.Button("×", EditorStyles.miniButtonRight, GUILayout.Width(22)))
                            {
                                _settings.folders.RemoveAt(i);
                                MarkDirty();
                            }
                        }
                    }
                }
                EditorGUILayout.EndScrollView();

                _settings.recursive = EditorGUILayout.ToggleLeft("文件夹递归（包含所有子文件夹）", _settings.recursive);

                EditorGUILayout.Space(4);

                // ---------- 指定文件 ----------
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("指定文件列表", EditorStyles.miniBoldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("添加选中文件", EditorStyles.miniButtonLeft, GUILayout.Width(100)))
                        AddSelectedFiles();
                    if (GUILayout.Button("清空", EditorStyles.miniButtonRight, GUILayout.Width(50)))
                    {
                        _settings.files.Clear();
                        MarkDirty();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("添加单个文件…", EditorStyles.miniButtonLeft))
                        AddFileViaPanel();
                }

                _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.MaxHeight(80));
                if (_settings.files.Count == 0)
                {
                    EditorGUILayout.LabelField("（空）可直接把 Project 中的 FBX 拖到下框", EditorStyles.miniLabel);
                }
                else
                {
                    for (int i = _settings.files.Count - 1; i >= 0; i--)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(_settings.files[i], EditorStyles.miniLabel);
                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(22)))
                            {
                                _settings.files.RemoveAt(i);
                                MarkDirty();
                            }
                        }
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawOptionSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("输出设置", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("目标文件夹", GUILayout.Width(70));
                    _settings.targetFolder = EditorGUILayout.TextField(_settings.targetFolder);
                    if (GUILayout.Button("…", GUILayout.Width(28)))
                    {
                        string abs = ChooseFolderPanel("选择输出文件夹",
                            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), _settings.targetFolder)));
                        if (!string.IsNullOrEmpty(abs))
                        {
                            string assetPath = ToAssetPath(abs);
                            if (string.IsNullOrEmpty(assetPath))
                                EditorUtility.DisplayDialog("错误", "目标文件夹必须位于 Assets 目录下。", "确定");
                            else
                            {
                                _settings.targetFolder = assetPath;
                                MarkDirty();
                            }
                        }
                    }
                }

                _settings.scanSubFolders = EditorGUILayout.ToggleLeft(
                    "保留相对子目录结构（按 FBX 所在子文件夹输出）", _settings.scanSubFolders);
                _settings.createAnimController = EditorGUILayout.ToggleLeft(
                    "为每个 FBX 生成 AnimatorController（同名）", _settings.createAnimController);
                _settings.overwrite = EditorGUILayout.ToggleLeft(
                    "覆盖已存在的同名 .anim（不勾选则跳过）", _settings.overwrite);
            }
        }

        private void DrawScanSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"扫描结果：{_fbxList.Count} 个 FBX", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("重新扫描", GUILayout.Width(80)))
                        Scan();
                }

                if (_fbxList.Count > 0)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("全选", EditorStyles.miniButtonLeft, GUILayout.Width(60)))
                            SetAll(true);
                        if (GUILayout.Button("全不选", EditorStyles.miniButtonMid, GUILayout.Width(60)))
                            SetAll(false);
                        if (GUILayout.Button("仅选有动画的", EditorStyles.miniButtonRight, GUILayout.Width(100)))
                            SelectOnlyWithAnim();
                        GUILayout.FlexibleSpace();
                        int selectedCount = _selected.Count(p => p.Value);
                        EditorGUILayout.LabelField($"{selectedCount} / {_fbxList.Count}", GUILayout.Width(80));
                    }

                    _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.Height(220));
                    foreach (string fbxPath in _fbxList)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            bool cur = _selected.TryGetValue(fbxPath, out var v) && v;
                            bool next = EditorGUILayout.Toggle(cur, GUILayout.Width(18));
                            if (next != cur) _selected[fbxPath] = next;

                            EditorGUILayout.LabelField(fbxPath, EditorStyles.miniLabel);

                            if (_fbxAnimations.TryGetValue(fbxPath, out var anims) && anims.Count > 0)
                            {
                                var style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.3f, 0.85f, 0.4f) } };
                                EditorGUILayout.LabelField($"({anims.Count} 动画)", style, GUILayout.Width(70));
                            }
                            else
                            {
                                var style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.95f, 0.75f, 0.25f) } };
                                EditorGUILayout.LabelField("(无动画)", style, GUILayout.Width(60));
                            }
                        }
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private void DrawExportSection()
        {
            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, MessageType.None);

            bool canExport = _fbxList.Count > 0
                             && !string.IsNullOrEmpty(_settings.targetFolder)
                             && _selected.Count(p => p.Value) > 0;

            using (new EditorGUI.DisabledScope(!canExport))
            {
                if (GUILayout.Button("开始提取动画", GUILayout.Height(36)))
                    Export();
            }
        }

        #endregion

        #region 源收集

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
                if (string.IsNullOrEmpty(p)) continue;
                if (!p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)) continue;
                if (_settings.files.Contains(p)) continue;
                _settings.files.Add(p);
                added++;
            }
            if (added == 0) _status = "没有在 Project 中选中 FBX 文件。";
            MarkDirty();
        }

        private void AddFolderViaPanel()
        {
            string abs = ChooseFolderPanel("添加源文件夹", Directory.GetCurrentDirectory());
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

            string abs = EditorUtility.OpenFilePanelWithFilters("添加 FBX 文件", start, new[] { "FBX", "fbx,FBX" });
            if (string.IsNullOrEmpty(abs)) return;

            string assetPath = ToAssetPath(abs);
            if (string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.DisplayDialog("错误", "只能添加 Assets 目录下的 FBX 文件。", "确定");
                return;
            }
            if (!_settings.files.Contains(assetPath))
                _settings.files.Add(assetPath);
            MarkDirty();
        }

        /// <summary>
        /// 处理拖拽：文件夹加入 folders，.fbx 加入 files。
        /// </summary>
        private void HandleDragAndDrop(Rect area)
        {
            var evt = Event.current;
            if (!area.Contains(evt.mousePosition)) return;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return;

            bool hasValid = DragAndDrop.paths.Any(p =>
                AssetDatabase.IsValidFolder(p) || p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase));
            if (!hasValid) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type != EventType.DragPerform) return;

            DragAndDrop.AcceptDrag();
            foreach (string p in DragAndDrop.paths)
            {
                string path = p.Replace('\\', '/');
                if (AssetDatabase.IsValidFolder(path))
                {
                    if (!_settings.folders.Contains(path)) _settings.folders.Add(path);
                }
                else if (path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                {
                    if (!_settings.files.Contains(path)) _settings.files.Add(path);
                }
            }
            MarkDirty();
            evt.Use();
        }

        private static string ChooseFolderPanel(string title, string startDir)
        {
            // Unity 没有原生“选择文件夹”对话框，改用 OpenFolderPanel 取绝对路径。
            return EditorUtility.OpenFolderPanel(title, startDir, string.Empty);
        }

        private static string ToAssetPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return null;
            absolutePath = absolutePath.Replace('\\', '/').TrimEnd('/');
            string projectRoot = Directory.GetCurrentDirectory().Replace('\\', '/').TrimEnd('/');
            if (!absolutePath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase)) return null;
            string rel = absolutePath.Substring(projectRoot.Length).TrimStart('/');
            if (rel == "Assets" || rel.StartsWith("Assets/", StringComparison.Ordinal)) return rel;
            return null;
        }

        #endregion

        #region 扫描

        private void MarkDirty()
        {
            _dirty = true;
            SaveSettings();
        }

        private void Scan()
        {
            _fbxList.Clear();
            _fbxAnimations.Clear();
            _selected.Clear();

            var set = new HashSet<string>(StringComparer.Ordinal);

            // 1) 文件夹
            foreach (string folder in _settings.folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    _status = $"文件夹无效：{folder}";
                    continue;
                }

                var search = _settings.recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                string absFolder = Path.GetFullPath(folder);
                if (!Directory.Exists(absFolder)) continue;

                foreach (string abs in Directory.GetFiles(absFolder, "*.fbx", search))
                {
                    string assetPath = ToAssetPath(abs);
                    if (!string.IsNullOrEmpty(assetPath)) set.Add(assetPath);
                }
            }

            // 2) 指定文件
            foreach (string file in _settings.files)
            {
                if (File.Exists(Path.GetFullPath(file)))
                    set.Add(file);
            }

            _fbxList.AddRange(set.OrderBy(p => p, StringComparer.Ordinal));

            try
            {
                for (int i = 0; i < _fbxList.Count; i++)
                {
                    string path = _fbxList[i];
                    EditorUtility.DisplayProgressBar("扫描 FBX", path, (float)i / Mathf.Max(1, _fbxList.Count));
                    var clips = GetClips(path);
                    _fbxAnimations[path] = clips.Select(c => c.name).ToList();
                    _selected[path] = clips.Count > 0;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            _status = $"扫描完成：{_fbxList.Count} 个 FBX，" +
                      $"{_fbxAnimations.Values.Sum(v => v.Count)} 个动画。";
            Repaint();
        }

        private static List<AnimationClip> GetClips(string fbxPath)
        {
            var result = new List<AnimationClip>();
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                    result.Add(clip);
            }
            return result;
        }

        private void SetAll(bool value)
        {
            foreach (string k in _fbxList) _selected[k] = value;
        }

        private void SelectOnlyWithAnim()
        {
            foreach (string k in _fbxList)
                _selected[k] = _fbxAnimations.TryGetValue(k, out var v) && v.Count > 0;
        }

        #endregion

        #region 导出

        private void Export()
        {
            if (_dirty || _fbxList.Count == 0)
                Scan();

            var targets = _fbxList.Where(p => _selected.TryGetValue(p, out var v) && v).ToList();
            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有勾选任何 FBX。", "确定");
                return;
            }

            string targetRoot = _settings.targetFolder.Replace('\\', '/').TrimEnd('/');
            EnsureFolder(targetRoot);

            int okFiles = 0, okClips = 0, skipped = 0;
            var failed = new List<string>();

            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    string fbxPath = targets[i];
                    float progress = (float)i / targets.Count;
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "提取动画", $"{fbxPath}  ({i + 1}/{targets.Count})", progress))
                        break;

                    try
                    {
                        int n = ExportSingle(fbxPath, targetRoot, out int skip);
                        skipped += skip;
                        if (n > 0) { okFiles++; okClips += n; }
                        else if (skip == 0) failed.Add(fbxPath + "（无动画）");
                    }
                    catch (Exception e)
                    {
                        failed.Add(fbxPath + " -> " + e.Message);
                        Debug.LogError($"[FBXAnimationExporter] 导出失败: {fbxPath}\n{e}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _status = $"完成：{okFiles} 个 FBX，共 {okClips} 个动画；跳过 {skipped} 个。" +
                      (failed.Count > 0 ? $" 失败 {failed.Count} 个。" : string.Empty);

            foreach (var f in failed) Debug.LogWarning("[FBXAnimationExporter] " + f);

            EditorUtility.DisplayDialog("提取完成",
                $"{_status}\n输出目录：{targetRoot}", "确定");
            Repaint();
        }

        /// <summary>
        /// 导出单个 FBX，返回成功写出的 clip 数。
        /// </summary>
        private int ExportSingle(string fbxPath, string targetRoot, out int skipped)
        {
            skipped = 0;
            var clips = GetClips(fbxPath);
            if (clips.Count == 0) return 0;

            // 动画名 = FBX 文件名
            string baseName = Path.GetFileNameWithoutExtension(fbxPath);

            // 输出子目录
            string outFolder = targetRoot;
            if (_settings.scanSubFolders)
            {
                string dir = Path.GetDirectoryName(fbxPath)?.Replace('\\', '/');
                string relative = GetRelativeToSources(dir);
                if (!string.IsNullOrEmpty(relative))
                    outFolder = $"{targetRoot}/{relative}";
            }
            EnsureFolder(outFolder);

            var exported = new List<AnimationClip>();

            // 单动画：文件名 = FBX 文件名；多动画：FBX 文件名_序号（避免同名互相覆盖）
            for (int i = 0; i < clips.Count; i++)
            {
                AnimationClip src = clips[i];
                string clipName = clips.Count > 1 ? $"{baseName}_{i:00}" : baseName;
                string animPath = $"{outFolder}/{clipName}.anim";

                if (File.Exists(Path.GetFullPath(animPath)))
                {
                    if (!_settings.overwrite)
                    {
                        skipped++;
                        continue;
                    }
                    AssetDatabase.DeleteAsset(animPath);
                }

                // CopySerialized 会连同原 clip 的 name 一起复制，必须先写到临时名再改名，
                // 否则磁盘上会出现「文件名与内部 name 不一致」的警告。
                var newClip = new AnimationClip { name = clipName };
                AssetDatabase.CreateAsset(newClip, animPath);
                EditorUtility.CopySerialized(src, newClip);
                newClip.name = clipName;
                EditorUtility.SetDirty(newClip);

                exported.Add(newClip);
            }

            if (_settings.createAnimController && exported.Count > 0)
            {
                string controllerPath = $"{outFolder}/{baseName}.controller";
                if (File.Exists(Path.GetFullPath(controllerPath)))
                {
                    if (!_settings.overwrite) return exported.Count;
                    AssetDatabase.DeleteAsset(controllerPath);
                }

                var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                foreach (var clip in exported)
                    controller.AddMotion(clip);
            }

            return exported.Count;
        }

        private string GetRelativeToSources(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return null;
            // 找到包含该目录的最长源文件夹，取相对路径
            string best = null;
            foreach (string folder in _settings.folders)
            {
                string f = folder.Replace('\\', '/').TrimEnd('/');
                if (dir.Equals(f, StringComparison.OrdinalIgnoreCase) ||
                    dir.StartsWith(f + "/", StringComparison.OrdinalIgnoreCase))
                {
                    if (best == null || f.Length > best.Length) best = f;
                }
            }
            if (best == null) return null;
            string rel = dir.Substring(best.Length).TrimStart('/');
            return rel;
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;

            string[] parts = assetFolder.Split('/');
            string cur = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        #endregion

        #region 配置持久化

        private void LoadSettings()
        {
            string json = EditorPrefs.GetString(PrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var s = JsonUtility.FromJson<Settings>(json);
                    if (s != null) _settings = s;
                }
                catch
                {
                    // 配置损坏时忽略，使用默认值
                }
            }
            _settings.folders ??= new List<string>();
            _settings.files ??= new List<string>();
            _dirty = true;
        }

        private void SaveSettings()
        {
            EditorPrefs.SetString(PrefsKey, JsonUtility.ToJson(_settings));
        }

        #endregion
    }
}
