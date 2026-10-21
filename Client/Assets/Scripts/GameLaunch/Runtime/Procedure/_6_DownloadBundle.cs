using Cysharp.Threading.Tasks;
using GameLaunch;
using System;
using UnityEngine;
using YooAsset;

namespace GameLaunch
{
    public class _6_DownloadBundle : Procedure
    {
        public override void OnEnter(object data)
        {
            //App.Instance.SetContent("DownloadBundle...");
            GameApp.Instance.SetContent("资源下载中...");
            BeginDownloader((ResourceDownloaderOperation)data).Forget();
        }

        private async UniTaskVoid BeginDownloader(ResourceDownloaderOperation operation)
        {
            operation.DownloadError += OnDownloadError;
            operation.DownloadProgressChanged += OnDownloadProgressChanged;

            operation.StartDownload();

            await operation.ToUniTask();

            if (operation.Status != EOperationStatus.Succeeded)
            {
                return;
            }

            ChangeProcedure<_7_ClearUnusedBundle>();

        }

        private void OnDownloadError(DownloadErrorEventArgs args)
        {
            Debug.LogError(args.ErrorInfo);
            GameApp.Instance.ShowTip("提示", args.ErrorInfo, confirmAction: Application.Quit);
        }

        private void OnDownloadProgressChanged(DownloadProgressChangedEventArgs args)
        {
            GameApp.Instance.SetProgress(args.Progress, true);
            string currentSizeMB = (args.CurrentDownloadBytes / 104876f).ToString("F1");
            string totalSizeMB = (args.TotalDownloadBytes / 104876f).ToString("F1");
            GameApp.Instance.SetContent($"{args.CurrentDownloadCount}/{args.TotalDownloadCount} {currentSizeMB}MB/{totalSizeMB}MB");
        }


    }
}