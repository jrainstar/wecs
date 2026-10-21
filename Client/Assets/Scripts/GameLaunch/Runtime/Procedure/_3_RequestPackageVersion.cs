using System;
using Cysharp.Threading.Tasks;
using GameLaunch;
using UnityEngine;
using YooAsset;

namespace GameLaunch
{
    public class _3_RequestPackageVersion : Procedure
    {
        public override void OnEnter(object data)
        {
            //App.Instance.SetContent("RequestPackageVersion...");
            GameApp.Instance.SetContent("请求包体版本...");
            RequestPackageVersion().Forget();
        }

        private async UniTaskVoid RequestPackageVersion()
        {
            await UniTask.Delay(100);
            //if (Application.internetReachability == NetworkReachability.NotReachable)
            //{
            //    Debug.LogError("network reachability is not reachable.!");
            //    GameApp.Instance.ShowTip("network reachability is not reachable.!", confirmAction: Application.Quit);
            //    return;
            //}

            var package = YooAssets.GetPackage(GameApp.PackageName);
            var operation = package.RequestPackageVersionAsync();
            await operation.ToUniTask();
            if (operation.Status == EOperationStatus.Failed)
            {
                Debug.LogError(operation.Error);
                GameApp.Instance.ShowTip("提示", operation.Error, confirmAction: Application.Quit);
                return;
            }

            ChangeProcedure<_4_UpdatePackageManifest>(operation.PackageVersion);
        }
    }
}