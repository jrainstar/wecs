using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;
using UnityEditor;
using UnityEngine;
// 逻辑层也有一个用于窄相检测的 Transform, 这里统一指向 Unity 的组件
using Transform = UnityEngine.Transform;

namespace GameLogic.Editor
{
    /// <summary>
    /// 场景碰撞导出工具。
    /// 在指定节点下用 Unity 的物理碰撞盒 (Box/Sphere/Capsule/Mesh) 摆好场景碰撞,
    /// 逐个转换后导出 SceneData JSON, 供 StageData.scene 使用 (加载时一次性转成 FP)。
    ///
    /// 形状映射:
    /// BoxCollider     -> COLLIDER_BOX
    /// SphereCollider  -> COLLIDER_SPHERE
    /// CapsuleCollider -> COLLIDER_CAPSULE; 同节点网格名含 Cylinder 时导出为 COLLIDER_CYLINDER
    ///                    (Unity 圆柱图元自带的就是 CapsuleCollider)
    /// MeshCollider    -> COLLIDER_CYLINDER (网格名含 Cylinder 时, 按包围盒取半径/高度)
    /// </summary>
    public class SceneColliderExporter : EditorWindow
    {
        private const string PrefsKey = "GameLogic.Editor.SceneColliderExporter.Settings";
        private const string WindowMenu = "Tools/场景/场景碰撞导出 (SceneData JSON)";

        [Serializable]
        private class Settings
        {
            public string sceneName = "SceneData";
            public string outputFolder = "Assets/AssetRaws/SceneDatas";
            public int layer = COLLISION_DEFINE.LAYER_DEFAULT;
            public bool includeInactive = true;
            public bool includeDisabledColliders = false;
            public bool pretty = true;
        }

        private Settings _settings = new Settings();
        private Transform _root;
        private string _status = string.Empty;

        [MenuItem(WindowMenu, false, 100)]
        public static void ShowWindow()
        {
            var win = GetWindow<SceneColliderExporter>("场景碰撞导出");
            win.minSize = new Vector2(520, 340);
            win.Show();
        }

        private void OnEnable()
        {
            _settings = LoadSettings();
        }

        private void OnDisable()
        {
            SaveSettings(_settings);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("场景碰撞 → SceneData JSON", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "在指定节点下用 Unity 物理碰撞盒摆放场景碰撞，导出为 SceneData JSON。\n" +
                "Box / Sphere / Capsule 直接映射；CapsuleCollider 所在节点的网格名含 Cylinder 时导出为圆柱。\n" +
                "位置/尺寸为世界空间米制，旋转为欧拉角 (度)。",
                MessageType.Info);

            EditorGUILayout.Space(6);
            DrawRootSection();
            EditorGUILayout.Space(6);
            DrawOptionSection();
            EditorGUILayout.Space(6);
            DrawExportSection();
        }

        private void DrawRootSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("导出根节点", EditorStyles.boldLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    _root = (Transform)EditorGUILayout.ObjectField(_root, typeof(Transform), true);
                    if (GUILayout.Button("使用选中节点", GUILayout.Width(96)))
                    {
                        _root = Selection.activeTransform;
                        if (null != _root && string.IsNullOrEmpty(_settings.sceneName))
                            _settings.sceneName = _root.name;
                        SaveSettings(_settings);
                    }
                }

                EditorGUILayout.LabelField(
                    "导出该节点及其所有子节点上的碰撞盒（含自身）", EditorStyles.miniLabel);
            }
        }

        private void DrawOptionSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("输出设置", EditorStyles.boldLabel);

                _settings.sceneName = EditorGUILayout.TextField("场景名称", _settings.sceneName);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("输出文件夹", GUILayout.Width(70));
                    _settings.outputFolder = EditorGUILayout.TextField(_settings.outputFolder);
                    if (GUILayout.Button("…", GUILayout.Width(28)))
                    {
                        string abs = EditorUtility.OpenFolderPanel("选择输出文件夹", _settings.outputFolder, string.Empty);
                        if (!string.IsNullOrEmpty(abs))
                        {
                            string assetPath = ToAssetPath(abs);
                            if (!string.IsNullOrEmpty(assetPath)) _settings.outputFolder = assetPath;
                        }
                    }
                }

                _settings.layer = EditorGUILayout.IntPopup(
                    "碰撞层",
                    _settings.layer,
                    new[] { "默认 (1)", "地板 (2)", "玩家 (4)" },
                    new[] { COLLISION_DEFINE.LAYER_DEFAULT, COLLISION_DEFINE.LAYER_GROUND, COLLISION_DEFINE.LAYER_PLAYER });

                _settings.includeInactive = EditorGUILayout.ToggleLeft("包含未激活节点", _settings.includeInactive);
                _settings.includeDisabledColliders = EditorGUILayout.ToggleLeft("包含已禁用的碰撞盒", _settings.includeDisabledColliders);
                _settings.pretty = EditorGUILayout.ToggleLeft("JSON 缩进格式化", _settings.pretty);
            }
        }

        private void DrawExportSection()
        {
            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, MessageType.None);

            using (new EditorGUI.DisabledScope(null == _root))
            {
                if (GUILayout.Button("导出场景碰撞", GUILayout.Height(36)))
                    Export();
            }
        }

        private void Export()
        {
            if (null == _root)
            {
                EditorUtility.DisplayDialog("提示", "请先指定导出根节点。", "确定");
                return;
            }

            var list = new List<CollisionData>();
            Collider[] colliders = _root.GetComponentsInChildren<Collider>(_settings.includeInactive);
            int skipped = 0;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider source = colliders[i];
                if (null == source) continue;

                if (!_settings.includeDisabledColliders && !source.enabled)
                {
                    skipped++;
                    continue;
                }

                if (!TryConvert(source, _settings.layer, out CollisionData data, out string error))
                {
                    skipped++;
                    Debug.LogWarning($"[SceneColliderExporter] {GetPath(source.transform)} -> {error}");
                    continue;
                }

                list.Add(data);
            }

            if (list.Count == 0)
            {
                _status = "没有可导出的碰撞盒。";
                EditorUtility.DisplayDialog("提示", _status, "确定");
                Repaint();
                return;
            }

            var scene = new SceneData
            {
                name = string.IsNullOrEmpty(_settings.sceneName) ? _root.name : _settings.sceneName,
                colliders = list.ToArray(),
            };

            string folder = (_settings.outputFolder ?? string.Empty).Replace('\\', '/').TrimEnd('/');
            if (string.IsNullOrEmpty(folder))
            {
                EditorUtility.DisplayDialog("错误", "请先设置输出文件夹。", "确定");
                return;
            }

            try
            {
                EnsureAssetFolder(folder);
                string assetPath = folder + "/" + SanitizeFileName(scene.name) + ".json";
                string absPath = Path.GetFullPath(assetPath);
                File.WriteAllText(absPath, JsonUtility.ToJson(scene, _settings.pretty) + Environment.NewLine, new UTF8Encoding(false));

                if (IsAssetsPath(assetPath))
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                AssetDatabase.Refresh();

                _status = $"导出完成：{list.Count} 个碰撞盒，跳过 {skipped} 个。\n{assetPath}";
                EditorUtility.DisplayDialog("导出完成", _status, "确定");
            }
            catch (Exception e)
            {
                _status = "导出失败：" + e.Message;
                Debug.LogException(e);
                EditorUtility.DisplayDialog("导出失败", e.Message, "确定");
            }

            SaveSettings(_settings);
            Repaint();
        }

        /// <summary>
        /// 把 Unity 碰撞盒转换为碰撞数据。
        /// 位置取世界空间中心, 尺寸/半径按 lossyScale 烘焙, 缩放固定导出为 1。
        /// 数据保持浮点 (米/度), 由 CollisionData.Apply 在加载时一次性转成 FP。
        /// </summary>
        private static bool TryConvert(Collider source, int layer, out CollisionData data, out string error)
        {
            data = default;
            error = null;

            Transform transform = source.transform;
            Vector3 scale = transform.lossyScale;
            Vector3 abs = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

            data.layer = layer;
            data.euler = transform.eulerAngles;

            if (source is BoxCollider box)
            {
                data.shape = COLLISION_DEFINE.COLLIDER_BOX;
                data.position = transform.TransformPoint(box.center);
                data.size = Vector3.Scale(box.size, abs);
                return true;
            }

            if (source is SphereCollider sphere)
            {
                data.shape = COLLISION_DEFINE.COLLIDER_SPHERE;
                data.position = transform.TransformPoint(sphere.center);
                data.radius = sphere.radius * Max(abs);
                return true;
            }

            if (source is CapsuleCollider capsule)
            {
                // Unity 圆柱图元挂的是 CapsuleCollider, 用网格名区分圆柱与胶囊
                data.shape = IsCylinderMesh(source)
                    ? COLLISION_DEFINE.COLLIDER_CYLINDER
                    : COLLISION_DEFINE.COLLIDER_CAPSULE;
                data.position = transform.TransformPoint(capsule.center);

                float radiusScale;
                float heightScale;
                Quaternion adjust;

                switch (capsule.direction)
                {
                    case 0: // X 轴
                        radiusScale = Mathf.Max(abs.y, abs.z);
                        heightScale = abs.x;
                        // 逻辑层胶囊长轴固定为 Y, 用 -90° 绕 Z 把 Y 转到 X
                        adjust = Quaternion.Euler(0f, 0f, -90f);
                        break;
                    case 2: // Z 轴
                        radiusScale = Mathf.Max(abs.x, abs.y);
                        heightScale = abs.z;
                        // 用 +90° 绕 X 把 Y 转到 Z
                        adjust = Quaternion.Euler(90f, 0f, 0f);
                        break;
                    default: // Y 轴
                        radiusScale = Mathf.Max(abs.x, abs.z);
                        heightScale = abs.y;
                        adjust = Quaternion.identity;
                        break;
                }

                data.euler = (transform.rotation * adjust).eulerAngles;
                data.radius = capsule.radius * radiusScale;
                data.height = capsule.height * heightScale;
                return true;
            }

            if (source is MeshCollider meshcollider)
            {
                Mesh mesh = meshcollider.sharedMesh;
                if (null != mesh && mesh.name.IndexOf("cylinder", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Bounds bounds = mesh.bounds;
                    data.shape = COLLISION_DEFINE.COLLIDER_CYLINDER;
                    data.position = transform.TransformPoint(bounds.center);
                    data.radius = Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f * Mathf.Max(abs.x, abs.z);
                    data.height = bounds.size.y * abs.y;
                    return true;
                }

                error = "MeshCollider 仅支持名称含 Cylinder 的圆柱网格，已跳过";
                return false;
            }

            error = $"不支持的碰撞类型 {source.GetType().Name}，已跳过";
            return false;
        }

        /// <summary>
        /// 判断碰撞盒所在节点使用的网格是否为圆柱。
        /// Unity 圆柱图元的胶囊碰撞盒与胶囊一模一样, 只能靠网格名 (Cylinder) 区分。
        /// 网格数据在 MeshFilter 上, MeshRenderer 只负责渲染。
        /// </summary>
        private static bool IsCylinderMesh(Collider source)
        {
            MeshFilter filter = source.GetComponent<MeshFilter>();
            if (null == filter) filter = source.GetComponentInChildren<MeshFilter>(true);
            if (null == filter) return false;

            Mesh mesh = filter.sharedMesh;
            return null != mesh && mesh.name.IndexOf("cylinder", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static float Max(Vector3 value)
        {
            return Mathf.Max(value.x, Mathf.Max(value.y, value.z));
        }

        private static string GetPath(Transform transform)
        {
            if (null == transform) return "<null>";

            string path = transform.name;
            Transform parent = transform.parent;
            while (null != parent)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "SceneData";

            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
                sb.Append(Array.IndexOf(invalid, name[i]) >= 0 ? '_' : name[i]);

            string result = sb.ToString().Trim();
            return string.IsNullOrEmpty(result) ? "SceneData" : result;
        }

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
            return IsAssetsPath(rel) ? rel : null;
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;

            string[] parts = assetFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

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
            if (string.IsNullOrEmpty(settings.outputFolder))
                settings.outputFolder = "Assets/AssetRaws/SceneDatas";
            if (settings.layer <= 0)
                settings.layer = COLLISION_DEFINE.LAYER_DEFAULT;

            return settings;
        }

        private static void SaveSettings(Settings settings)
        {
            if (null == settings) return;
            EditorPrefs.SetString(PrefsKey, JsonUtility.ToJson(settings));
        }
    }
}
