#if ENABLE_HYBRID_CLR
using HybridCLR.Editor.Settings;
#endif
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using YooAsset.Editor;

namespace GameLaunch
{
    public class GameLaunchWindow : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset m_VisualTreeAsset = default;

        [MenuItem("Tools/发布工具窗口", priority = 9999)]
        public static void ShowExample()
        {
            GameLaunchWindow wnd = GetWindow<GameLaunchWindow>();
            wnd.titleContent = new GUIContent("GameLaunchWindow");
        }

        public void CreateGUI()
        {
            SetPatchAOTAssemblies();

            VisualElement root = rootVisualElement;

            VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
            root.Add(labelFromUXML);

            var textField = root.Q<TextField>("BuildBundleOutputDir");
            textField.value = BundleBuilderHelper.GetDefaultBuildOutputRoot() + "/";
            textField.SetEnabled(false);

            textField = root.Q<TextField>("BuildBundleInOutDir");
            textField.value = BundleBuilderHelper.GetStreamingAssetsRoot() + "/";
            textField.SetEnabled(false);

            textField = root.Q<TextField>("AotDllOutputDir");
            textField.value = GameApp.AotDllOutPutDir;
            textField.SetEnabled(false);

            textField = root.Q<TextField>("HotDllOutputDir");
            textField.value = GameApp.HotDllOutPutDir;
            textField.SetEnabled(false);

            textField = root.Q<TextField>("BuildOutputDir");
            textField.value = GameApp.ProjectDir + "Builds/";
            textField.SetEnabled(false);

            root.Q<TextField>("BuildVersion").value = GameApp.Version;

            var enumField = root.Q<EnumField>("BuildTarget");
            enumField.value = EditorUserBuildSettings.activeBuildTarget;

            root.Q<Button>("CompileDll").clicked += CompileDll;
            root.Q<Button>("BuildOutPutBundle").clicked += BuildOutPutBundle;
            root.Q<Button>("BuildInBundle").clicked += BuildInBundle;
            root.Q<Button>("BuildWindow").clicked += BuildWindow;
            root.Q<Button>("BuildAndroid").clicked += BuildAndroid;
        }

        public void SetPatchAOTAssemblies()
        {
#if ENABLE_HYBRID_CLR
            var patchAOTAssemblies = GameApp.PatchedAOTAssemblyList;
            var count = GameApp.PatchedAOTAssemblyList.Count;
            HybridCLRSettings.Instance.patchAOTAssemblies = new string[count];
            for (int i = 0; i < patchAOTAssemblies.Count; i++)
            {
                HybridCLRSettings.Instance.patchAOTAssemblies[i] = patchAOTAssemblies[i];
            }
#endif
        }

        /// <summary>
        /// 编译dll
        /// </summary>
        public void CompileDll()
        {
#if ENABLE_HYBRID_CLR
            DllBuilder.CompileDll();
#endif
        }

        /// <summary>
        /// 打热更包资源
        /// </summary>
        public void BuildOutPutBundle()
        {
#if ENABLE_HYBRID_CLR
            DllBuilder.CompileAndCopyHotDll();
#endif
            var buildTarget = (BuildTarget)rootVisualElement.Q<EnumField>("BuildTarget").value;
            var buildVersion = rootVisualElement.Q<TextField>("BuildVersion").value;
            AssetBuilder.BuildInternal(false, buildTarget, buildVersion);
        }

        /// <summary>
        /// 打整包资源
        /// </summary>
        public void BuildInBundle()
        {
#if ENABLE_HYBRID_CLR
            DllBuilder.CompileAndCopyHotAndAotDll();
#endif
            var buildTarget = (BuildTarget)rootVisualElement.Q<EnumField>("BuildTarget").value;
            var buildVersion = rootVisualElement.Q<TextField>("BuildVersion").value;
            AssetBuilder.BuildInternal(true, buildTarget, buildVersion);
        }

        /// <summary>
        /// 打包APK
        /// </summary>
        public void BuildAndroid()
        {
            BuildInBundle();
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            var buildVersion = rootVisualElement.Q<TextField>("BuildVersion").value;
            var outPutDir = rootVisualElement.Q<TextField>("BuildOutputDir").value;
            CheckAndCreateDirectory(outPutDir);
            var apkPath = $"{outPutDir}/Android/{Application.version}/{Application.productName}.apk";
            buildPlayerOptions.targetGroup = BuildTargetGroup.Android;
            buildPlayerOptions.locationPathName = apkPath;
            buildPlayerOptions.target = BuildTarget.Android;
            buildPlayerOptions.options = BuildOptions.None;
            BuildPipeline.BuildPlayer(buildPlayerOptions);
        }

        /// <summary>
        /// 打包APK
        /// </summary>
        public void BuildWindow()
        {
            BuildInBundle();
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            var buildVersion = rootVisualElement.Q<TextField>("BuildVersion").value;
            var outPutDir = rootVisualElement.Q<TextField>("BuildOutputDir").value;
            CheckAndCreateDirectory(outPutDir);
            var exePath = $"{outPutDir}/Window/{Application.version}/{Application.productName}.exe";
            buildPlayerOptions.targetGroup = BuildTargetGroup.Standalone;
            buildPlayerOptions.locationPathName = exePath;
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            buildPlayerOptions.options = BuildOptions.None;
            BuildPipeline.BuildPlayer(buildPlayerOptions);
        }

        public static void CheckAndCreateDirectory(string filePath)
        {
            string folderPath = Path.GetDirectoryName(filePath);
            if (folderPath != null && !Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }
    }
}
