using Cysharp.Threading.Tasks;
using GameLaunch;
using UnityEngine;
using YooAsset;

namespace GameLaunch
{
    public class _7_ClearUnusedBundle : Procedure
    {
        public override void OnEnter(object data)
        {
            GameApp.Instance.SetContent("清理资源中...");
            ClearUnusedBundle().Forget();
        }

        private async UniTaskVoid ClearUnusedBundle()
        {
            var package = YooAssets.GetPackage(GameApp.PackageName);
            var operation = package.ClearCacheAsync(new ClearCacheOptions(ClearCacheMethods.ClearUnusedBundleFiles));

            await operation.ToUniTask();

            if (operation.Status == EOperationStatus.Failed)
            {
                Debug.LogError(operation.Error);
                GameApp.Instance.ShowTip("提示", operation.Error, confirmAction: Application.Quit);
                return;
            }

#if ENABLE_HYBRID_CLR
            ChangeProcedure<_8_LoadAssembly>();
#else
            ChangeProcedure<_8_NoLoadAssembly>();
#endif
        }
    }
}