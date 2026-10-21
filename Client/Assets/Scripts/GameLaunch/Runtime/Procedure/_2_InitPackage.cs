using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace GameLaunch
{
    public class _2_InitPackage : Procedure
    {
        public override void OnEnter(object data)
        {
            GameApp.Instance.SetContent("初始化游戏...");
            GameApp.Instance.SetProgress(0, true);
            YooAssets.Initialize();
            InitPackage().Forget();
        }

        private async UniTask InitPackage()
        {
            await UniTask.Delay(100);

            var playMode = GameApp.Instance.PlayMode;
            if (!YooAssets.TryGetPackage(GameApp.PackageName, out var package))
            {
                package = YooAssets.CreatePackage(GameApp.PackageName);
            }

            InitializePackageOperation operation = null;

            // 编辑器模拟模式
            if (playMode == EPlayMode.EditorSimulateMode)
            {
#if UNITY_EDITOR
                var buildResult = EditorSimulateBuildInvoker.Build(GameApp.PackageName, (int)EBundleType.VirtualAssetBundle);
                var packageRoot = buildResult.PackageRootDirectory;
                var createParameters = new EditorSimulateModeOptions
                {
                    EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot)
                };
                operation = package.InitializePackageAsync(createParameters);
#endif
            }
            // 单机运行模式
            else if (playMode == EPlayMode.OfflinePlayMode)
            {
                var createParameters = new OfflinePlayModeOptions
                {
                    BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters()
                };
                operation = package.InitializePackageAsync(createParameters);
            }
            // 联机运行模式
            else if (playMode == EPlayMode.HostPlayMode)
            {
                IRemoteService remoteService = new RemoteService();
                var createParameters = new HostPlayModeOptions
                {
                    BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(),
                    CacheFileSystemParameters = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(remoteService)
                };
                createParameters.BuiltinFileSystemParameters.AddParameter(EFileSystemParameter.CopyBuiltinPackageManifest, true);
                operation = package.InitializePackageAsync(createParameters);
            }
            // WebGL运行模式
            else if (playMode == EPlayMode.WebPlayMode)
            {
                var createParameters = new WebPlayModeOptions();
#if UNITY_WEBGL && WEIXINMINIGAME && !UNITY_EDITOR
                string packageRoot = $"{WeChatWASM.WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE"; //注意：如果有子目录，请修改此处！
                IRemoteService remoteService = new RemoteService();
                createParameters.WebNetworkFileSystemParameters = WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteService);
#else
                createParameters.WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
#endif
                operation = package.InitializePackageAsync(createParameters);
            }

            if (operation == null)
            {
                var error = $"未支持的运行模式: {playMode}";
                Debug.LogError(error);
                GameApp.Instance.ShowTip("提示", error, confirmAction: Application.Quit);
                return;
            }

            await operation.ToUniTask();

            if (operation.Status == EOperationStatus.Failed)
            {
                Debug.LogError(operation.Error);
                GameApp.Instance.ShowTip("提示", operation.Error, confirmAction: Application.Quit);
                return;
            }

            ChangeProcedure<_3_RequestPackageVersion>();
        }

        /// <summary>
        /// 远端资源地址查询服务类
        /// </summary>
        private class RemoteService : IRemoteService
        {
            public IReadOnlyList<string> GetRemoteUrls(string fileName)
            {
                return new List<string> { $"{GameApp.AssetServerURL}/{fileName}" };
            }
        }

    }
}