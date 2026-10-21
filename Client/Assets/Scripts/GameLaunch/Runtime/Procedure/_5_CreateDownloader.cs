using Cysharp.Threading.Tasks;
using GameLaunch;
using System.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace GameLaunch
{
    public class _5_CreateDownloader : Procedure
    {
        public override void OnEnter(object data)
        {
            //App.Instance.SetContent("CreateDownloader...");
            GameApp.Instance.SetContent("创建下载器...");
            CreateDownloader().Forget();
        }

        private async UniTask CreateDownloader()
        {
            await UniTask.Delay(100);

            var package = YooAssets.GetPackage(GameApp.PackageName);
            var operation = package.CreateResourceDownloader(new ResourceDownloaderOptions(10, 3));

            if (operation.Status == EOperationStatus.Failed)
            {
                Debug.LogError(operation.Error);
                GameApp.Instance.ShowTip("提示", operation.Error, confirmAction: Application.Quit);
                return;
            }

            if (operation.TotalDownloadCount > 0)
            {
                ChangeProcedure<_6_DownloadBundle>(operation);
            }
            else
            {
#if ENABLE_HYBRID_CLR
                ChangeProcedure<_8_LoadAssembly>();
#else
                ChangeProcedure<_8_NoLoadAssembly>();
#endif
            }
        }
    }
}