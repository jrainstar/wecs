using Cysharp.Threading.Tasks;
using GameLaunch;
using UnityEngine;
using YooAsset;

namespace GameLaunch
{
    public class _4_UpdatePackageManifest : Procedure
    {
        public override void OnEnter(object data)
        {
            GameApp.Instance.SetContent("更新包清单...");
            // App.Instance.SetContent("UpdatePackageManifest...");
            UpdatePackageManifest((string)data).Forget();
        }

        private async UniTaskVoid UpdatePackageManifest(string packageVersion)
        {
            await UniTask.Delay(100);

            var package = YooAssets.GetPackage(GameApp.PackageName);
            var operation = package.LoadPackageManifestAsync(new LoadPackageManifestOptions(packageVersion, 10));

            await operation.ToUniTask();

            if (operation.Status == EOperationStatus.Failed)
            {
                Debug.LogError(operation.Error);
                GameApp.Instance.ShowTip("提示", operation.Error, confirmAction: Application.Quit);
            }

            GameApp.Instance.SetVersion(GameApp.Version, packageVersion);

            if (GameApp.Instance.PlayMode == EPlayMode.EditorSimulateMode || GameApp.Instance.PlayMode == EPlayMode.OfflinePlayMode)
            {
#if ENABLE_HYBRID_CLR
                ChangeProcedure<_8_LoadAssembly>();
#else
                ChangeProcedure<_8_NoLoadAssembly>();
#endif
            }
            else
            {
                ChangeProcedure<_5_CreateDownloader>();
            }
        }
    }
}