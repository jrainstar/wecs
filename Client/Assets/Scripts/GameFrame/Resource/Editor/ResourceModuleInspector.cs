#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;
using SceneHandle = YooAsset.SceneHandle;

namespace GameFrame
{
    /// <summary>
    /// ResourceModule 自定义 Inspector
    /// 运行时可视化：资源引用计数 / 实例 / 场景 / 一键操作
    /// 编辑模式下只显示"运行时才可用"的提示。
    /// </summary>
    [CustomEditor(typeof(ResourceModule))]
    public class ResourceModuleInspector : Editor
    {
        // 折叠状态
        private bool m_FoldAssets = true;
        private bool m_FoldInstances = true;
        private bool m_FoldScenes = true;

        // 过滤关键字
        private string m_Filter = string.Empty;

        // 每次绘制的最大展开行数（防止条目过多卡编辑器）
        private const int MaxRowsPerGroup = 200;

        // 样式缓存
        private GUIStyle m_HeaderStyle;
        private GUIStyle m_RowLabelStyle;
        private GUIStyle m_RowLabelStyleAlt;

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            EnsureStyles();

            var res = (ResourceModule)target;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("ResourceModule 调试面板仅在运行时可用。\n运行游戏后即可看到当前资源引用计数、实例与场景。", MessageType.Info);
                EditorGUILayout.Space(6);
                DrawDefaultInspector();
                return;
            }

            DrawHeader(res);
            EditorGUILayout.Space(4);
            DrawToolbar(res);
            EditorGUILayout.Space(6);
            DrawFilterBar();
            EditorGUILayout.Space(4);

            DrawAssetsGroup(res);
            DrawInstancesGroup(res);
            DrawScenesGroup(res);
        }

        #region 头部 & 工具条

        private void DrawHeader(ResourceModule res)
        {
            const float LabelW = 90f;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("ResourceModule · Runtime Debug", m_HeaderStyle);
            EditorGUILayout.Space(2);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Package", EditorStyles.label, GUILayout.Width(LabelW));
                var pkg = string.IsNullOrEmpty(res.PackageName) ? "<empty>" : res.PackageName;
                EditorGUILayout.SelectableLabel(pkg, EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight),
                    GUILayout.ExpandWidth(true));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Version", EditorStyles.label, GUILayout.Width(LabelW));
                var ver = string.IsNullOrEmpty(res.PackageVersion) ? "<empty>" : res.PackageVersion;
                EditorGUILayout.SelectableLabel(ver, EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight),
                    GUILayout.ExpandWidth(true));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawStatCell("Assets", res.AssetCount.ToString(), LabelW);
                DrawStatCell("TotalRef", res.TotalRefCount.ToString(), LabelW);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawStatCell("Instances", res.InstanceCount.ToString(), LabelW);
                DrawStatCell("Scenes", res.SceneCount.ToString(), LabelW);
            }

            EditorGUILayout.Space(2);
            DrawAutoUnloadInfo(res, LabelW);

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 绘制自动回收状态区：开关 / 下次扫描倒计时（进度条） / 闲置阈值
        /// </summary>
        private void DrawAutoUnloadInfo(ResourceModule res, float labelWidth)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("AutoUnload", EditorStyles.label, GUILayout.Width(labelWidth));
                var openBefore = res.OpenUnloadTimer;
                var openAfter = EditorGUILayout.ToggleLeft(
                    openBefore ? "开启" : "关闭",
                    openBefore,
                    GUILayout.Width(60));
                if (openAfter != openBefore)
                    res.OpenUnloadTimer = openAfter;

                GUILayout.Label($"闲置阈值 {res.NoUseUnloadTime}s   扫描间隔 {res.AutoCheckUnloadInterval}s",
                    EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Next Scan", EditorStyles.label, GUILayout.Width(labelWidth));

                var interval = Mathf.Max(1, res.AutoCheckUnloadInterval);
                var countdown = res.NextAutoUnloadCountdown;

                // 进度条 rect
                var barRect = GUILayoutUtility.GetRect(
                    GUIContent.none, EditorStyles.helpBox,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight),
                    GUILayout.ExpandWidth(true));

                if (countdown < 0f)
                {
                    // 未开启
                    EditorGUI.ProgressBar(barRect, 0f, "自动回收已关闭");
                }
                else
                {
                    var elapsed = res.AutoCheckUnloadTimer;
                    var progress = Mathf.Clamp01(elapsed / interval);
                    EditorGUI.ProgressBar(barRect, progress, $"距下次扫描 {countdown:F1}s / {interval}s");
                }
            }
        }

        private static void DrawStatCell(string label, string value, float labelWidth)
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true)))
            {
                GUILayout.Label(label, EditorStyles.label, GUILayout.Width(labelWidth));
                GUILayout.Label(value, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
            }
        }

        private void DrawToolbar(ResourceModule res)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh", EditorStyles.miniButton))
                {
                    Repaint();
                }
                if (GUILayout.Button("UnloadUnusedAssets", EditorStyles.miniButton))
                {
                    res.UnloadUnusedAssets();
                }
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("ReleaseAll (危险)", EditorStyles.miniButton))
                {
                    if (EditorUtility.DisplayDialog("ReleaseAll",
                        "将销毁所有已实例化对象并释放所有资源引用，确定继续吗？",
                        "确定", "取消"))
                    {
                        res.ReleaseAll();
                        GUI.backgroundColor = Color.white;
                        GUIUtility.ExitGUI();
                    }
                }
                GUI.backgroundColor = Color.white;
            }
        }

        private void DrawFilterBar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Filter", GUILayout.Width(40));
                m_Filter = EditorGUILayout.TextField(m_Filter);
                if (GUILayout.Button("×", GUILayout.Width(22)))
                {
                    m_Filter = string.Empty;
                    GUI.FocusControl(null);
                }
            }
        }

        #endregion

        #region Assets 分组

        private void DrawAssetsGroup(ResourceModule res)
        {
            m_FoldAssets = EditorGUILayout.BeginFoldoutHeaderGroup(m_FoldAssets, $"Assets ({res.AssetCount})");
            if (m_FoldAssets)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("Location", EditorStyles.miniBoldLabel, GUILayout.MinWidth(200), GUILayout.ExpandWidth(true));
                    GUILayout.Label("RefCount", EditorStyles.miniBoldLabel, GUILayout.Width(70));
                    GUILayout.Label("Preload", EditorStyles.miniBoldLabel, GUILayout.Width(60));
                    GUILayout.Label("Status", EditorStyles.miniBoldLabel, GUILayout.Width(90));
                    GUILayout.Label("Progress", EditorStyles.miniBoldLabel, GUILayout.Width(70));
                    GUILayout.Label("Op", EditorStyles.miniBoldLabel, GUILayout.Width(120));
                }

                int idx = 0;
                int shown = 0;
                foreach (var assetObject in res.EnumerateAssetObjects())
                {
                    if (assetObject == null) continue;
                    if (!Match(assetObject.Location)) continue;
                    if (shown >= MaxRowsPerGroup) { DrawOverflowTip(shown); break; }

                    DrawAssetRow(res, assetObject, idx++);
                    shown++;
                }

                if (shown == 0)
                    EditorGUILayout.LabelField("(无匹配的资源)", EditorStyles.centeredGreyMiniLabel);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(4);
        }

        private void DrawAssetRow(ResourceModule res, AssetObject assetObject, int rowIndex)
        {
            var handle = assetObject.CurrentHandle;
            var status = handle != null ? handle.Status : EOperationStatus.None;
            var progress = handle != null ? handle.Progress : 0f;

            var style = (rowIndex % 2 == 0) ? m_RowLabelStyle : m_RowLabelStyleAlt;

            using (new EditorGUILayout.HorizontalScope(style))
            {
                Object pingTarget = assetObject.AssetHandle != null ? assetObject.AssetHandle.AssetObject : null;
                if (GUILayout.Button(new GUIContent(assetObject.Location, assetObject.Location),
                    EditorStyles.linkLabel,
                    GUILayout.MinWidth(200), GUILayout.ExpandWidth(true)))
                {
                    if (pingTarget != null)
                    {
                        EditorGUIUtility.PingObject(pingTarget);
                        Selection.activeObject = pingTarget;
                    }
                }

                var rcColor = GUI.color;
                if (assetObject.ReferenceCount <= 0) GUI.color = new Color(1f, 0.5f, 0.5f);
                else if (assetObject.ReferenceCount >= 5) GUI.color = new Color(1f, 0.85f, 0.4f);
                GUILayout.Label(assetObject.ReferenceCount.ToString(), GUILayout.Width(70));
                GUI.color = rcColor;

                GUILayout.Label(assetObject.IsPreload ? "√" : "-", GUILayout.Width(60));
                GUILayout.Label(status.ToString(), GUILayout.Width(90));
                GUILayout.Label(progress >= 1f ? "1.00" : progress.ToString("F2"), GUILayout.Width(70));

                if (GUILayout.Button("Release -1", EditorStyles.miniButton, GUILayout.Width(70)))
                {
                    res.Release(assetObject.Location);
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("Force", EditorStyles.miniButton, GUILayout.Width(46)))
                {
                    if (EditorUtility.DisplayDialog("ForceRelease",
                        $"强制释放 {assetObject.Location}（当前 RefCount={assetObject.ReferenceCount}）？\n\n所有依赖此资源的对象都会失效。",
                        "确定", "取消"))
                    {
                        res.ForceRelease(assetObject.Location);
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        #endregion

        #region Instances 分组

        private void DrawInstancesGroup(ResourceModule res)
        {
            m_FoldInstances = EditorGUILayout.BeginFoldoutHeaderGroup(m_FoldInstances, $"Instances ({res.InstanceCount})");
            if (m_FoldInstances)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("Instance", EditorStyles.miniBoldLabel, GUILayout.MinWidth(200), GUILayout.ExpandWidth(true));
                    GUILayout.Label("Type", EditorStyles.miniBoldLabel, GUILayout.Width(120));
                    GUILayout.Label("Location", EditorStyles.miniBoldLabel, GUILayout.Width(200));
                    GUILayout.Label("Op", EditorStyles.miniBoldLabel, GUILayout.Width(80));
                }

                int idx = 0;
                int shown = 0;
                foreach (var kv in res.EnumerateInstances())
                {
                    var instance = kv.Key;
                    var location = kv.Value;
                    if (!Match(location) && !Match(instance != null ? instance.name : string.Empty)) continue;
                    if (shown >= MaxRowsPerGroup) { DrawOverflowTip(shown); break; }

                    DrawInstanceRow(res, instance, location, idx++);
                    shown++;
                }

                if (shown == 0)
                    EditorGUILayout.LabelField("(无匹配的实例)", EditorStyles.centeredGreyMiniLabel);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(4);
        }

        private void DrawInstanceRow(ResourceModule res, Object instance, string location, int rowIndex)
        {
            var style = (rowIndex % 2 == 0) ? m_RowLabelStyle : m_RowLabelStyleAlt;
            using (new EditorGUILayout.HorizontalScope(style))
            {
                string display = instance != null ? instance.name : "<null>";
                if (GUILayout.Button(new GUIContent(display, display),
                    EditorStyles.linkLabel,
                    GUILayout.MinWidth(200), GUILayout.ExpandWidth(true)))
                {
                    if (instance != null)
                    {
                        EditorGUIUtility.PingObject(instance);
                        Selection.activeObject = instance;
                    }
                }

                GUILayout.Label(instance != null ? instance.GetType().Name : "-", GUILayout.Width(120));
                GUILayout.Label(new GUIContent(location, location), GUILayout.Width(200));

                if (GUILayout.Button("Destroy", EditorStyles.miniButton, GUILayout.Width(75)))
                {
                    if (instance is GameObject go) res.DestroyInstance(go);
                    else if (instance is Component comp) res.DestroyInstance(comp);
                    else res.DestroyInstance(instance);
                    GUIUtility.ExitGUI();
                }
            }
        }

        #endregion

        #region Scenes 分组

        private void DrawScenesGroup(ResourceModule res)
        {
            m_FoldScenes = EditorGUILayout.BeginFoldoutHeaderGroup(m_FoldScenes, $"Scenes ({res.SceneCount})");
            if (m_FoldScenes)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("Location", EditorStyles.miniBoldLabel, GUILayout.MinWidth(200), GUILayout.ExpandWidth(true));
                    GUILayout.Label("IsDone", EditorStyles.miniBoldLabel, GUILayout.Width(60));
                    GUILayout.Label("Progress", EditorStyles.miniBoldLabel, GUILayout.Width(70));
                    GUILayout.Label("Op", EditorStyles.miniBoldLabel, GUILayout.Width(80));
                }

                int idx = 0;
                int shown = 0;
                foreach (var kv in res.EnumerateScenes())
                {
                    var location = kv.Key;
                    var handle = kv.Value;
                    if (!Match(location)) continue;
                    if (shown >= MaxRowsPerGroup) { DrawOverflowTip(shown); break; }

                    DrawSceneRow(res, location, handle, idx++);
                    shown++;
                }

                if (shown == 0)
                    EditorGUILayout.LabelField("(无匹配的场景)", EditorStyles.centeredGreyMiniLabel);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawSceneRow(ResourceModule res, string location, SceneHandle handle, int rowIndex)
        {
            var style = (rowIndex % 2 == 0) ? m_RowLabelStyle : m_RowLabelStyleAlt;
            using (new EditorGUILayout.HorizontalScope(style))
            {
                GUILayout.Label(new GUIContent(location, location), GUILayout.MinWidth(200), GUILayout.ExpandWidth(true));
                GUILayout.Label(handle != null && handle.IsDone ? "Yes" : "No", GUILayout.Width(60));
                GUILayout.Label(handle != null ? handle.Progress.ToString("F2") : "-", GUILayout.Width(70));

                if (GUILayout.Button("Unload", EditorStyles.miniButton, GUILayout.Width(75)))
                {
                    res.UnloadScene(location);
                    GUIUtility.ExitGUI();
                }
            }
        }

        #endregion

        #region 工具

        private bool Match(string s)
        {
            if (string.IsNullOrEmpty(m_Filter)) return true;
            if (string.IsNullOrEmpty(s)) return false;
            return s.IndexOf(m_Filter, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void DrawOverflowTip(int shown)
        {
            EditorGUILayout.HelpBox($"仅展示前 {shown} 条，请使用 Filter 过滤查看更多。", MessageType.Info);
        }

        private void EnsureStyles()
        {
            if (m_HeaderStyle == null)
            {
                m_HeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleLeft,
                };
            }
            if (m_RowLabelStyle == null)
            {
                m_RowLabelStyle = new GUIStyle();
                m_RowLabelStyle.padding = new RectOffset(4, 4, 2, 2);
            }
            if (m_RowLabelStyleAlt == null)
            {
                m_RowLabelStyleAlt = new GUIStyle();
                m_RowLabelStyleAlt.padding = new RectOffset(4, 4, 2, 2);
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.04f));
                tex.Apply();
                m_RowLabelStyleAlt.normal.background = tex;
            }
        }

        #endregion
    }
}
#endif