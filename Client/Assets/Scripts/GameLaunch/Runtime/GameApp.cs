using UnityEngine;
using YooAsset;
using UnityEngine.Events;
using System.Collections.Generic;
using System.IO;

namespace GameLaunch
{
    public class GameApp : MonoBehaviour
    {

        public static readonly string ProjectDir = Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/') + "/";

        #region >>>>>>>>>>>热更DLL相关<<<<<<<<<<<<

        public static readonly string AotDllOutPutDir = "Assets/AssetRaws/Dlls/Aot/";
        public static readonly string HotDllOutPutDir = "Assets/AssetRaws/Dlls/Hot/";
        public static readonly IReadOnlyList<string> HotAssemblyList = new List<string>
        {
            "GameLogic.dll",        //第一个热更程序集作为主逻辑入口
        };
        public static readonly IReadOnlyList<string> PatchedAOTAssemblyList = new List<string>
        {
            "DOTween.dll",
            "GameFramework.dll",
            "Luban.Runtime.dll",
            "MemoryPack.Core.dll",
            "System.Core.dll",
            "System.Runtime.CompilerServices.Unsafe.dll",
            "System.dll",
            "UniTask.dll",
            "Unity.InputSystem.dll",
            "UnityEngine.CoreModule.dll",
            "mscorlib.dll",
        };

        #endregion

        #region >>>>>>>>>>>热更资源相关<<<<<<<<<<<<

        public static readonly string URL = "http://192.168.1.75:7011";
        public static readonly string PackageName = "DefaultPackage";
        public static readonly string Version = "V1.0";

        public static string AssetServerURL
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorUserBuildSettings.activeBuildTarget switch
                {
                    UnityEditor.BuildTarget.Android => $"{URL}/Android/{PackageName}/{Version}",
                    UnityEditor.BuildTarget.iOS => $"{URL}/IPhone/{PackageName}/{Version}",
                    UnityEditor.BuildTarget.WebGL => $"{URL}/WebGL/{PackageName}/{Version}",
                    _ => $"{URL}/PC/{PackageName}/{Version}"
                };
#else
                return Application.platform switch
                {
                    RuntimePlatform.Android =>         $"{URL}/Android/{PackageName}/{Version}",
                    RuntimePlatform.IPhonePlayer =>    $"{URL}/IPhone/{PackageName}/{Version}",
                    RuntimePlatform.WebGLPlayer =>     $"{URL}/WebGL/{PackageName}/{Version}",
                    _ =>                               $"{URL}/PC/{PackageName}/{Version}"
                };
#endif
            }
        }

        #endregion

        [SerializeField] private LoadWindow m_LoadWindow;
        [SerializeField] private TipWindow m_TipWindow;

        [SerializeField] private EPlayMode m_PlayMode = EPlayMode.EditorSimulateMode;

        public EPlayMode PlayMode
        {
            get
            {
#if UNITY_EDITOR
                return m_PlayMode;
#elif UNITY_WEBGL
            return EPlayMode.WebPlayMode;
#else
            return m_PlayMode == EPlayMode.OfflinePlayMode ? EPlayMode.OfflinePlayMode : EPlayMode.HostPlayMode;
#endif
            }
        }

        public static GameApp Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            new _0_Splash().OnEnter(null);
        }

        private void OnDestroy()
        {
            Instance = null;
        }

        public void SetContent(string content)
        {
            m_LoadWindow.SetContent(content);
        }

        public void SetVersion(string appVersion, string assetVersion)
        {
            m_LoadWindow.SetVersion(appVersion, assetVersion);
        }

        public void SetProgress(float progress, bool force = false)
        {
            m_LoadWindow.SetProgress(progress, force);
        }

        public void ShowTip(string title, string content, UnityAction cancelAction = null, UnityAction confirmAction = null)
        {
            m_TipWindow.Show(title, content, cancelAction, confirmAction);
        }

    }
}