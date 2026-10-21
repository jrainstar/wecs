using Cysharp.Threading.Tasks;
using GameLaunch;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;

namespace GameLogic
{
    public static class Main
    {
        public static void Entrance(object[] args)
        {
            EntryBuild();
        }

        private static async void EntryBuild()
        {
            var package = YooAssets.GetPackage(GameApp.PackageName);

            // 先叠加加载场景，最后再卸载旧场景
            var engineHandle = package.LoadSceneSync("GameEngine", LoadSceneMode.Additive);
            await UniTask.DelayFrame(0);

            var entryHandle = package.LoadSceneSync("GameEntry", LoadSceneMode.Additive);
            await UniTask.DelayFrame(0);

            // 切换到Hall状态，新UI已创建
            GameEntry.Instance.ChangeState(GameState.Main);
            await UniTask.DelayFrame(2); // 确保UI显示

            // 卸载旧的场景
            await SceneManager.UnloadSceneAsync("GameLaunch");
            await SceneManager.UnloadSceneAsync("GameEngine");
        }

        //private static async void EntryBuild()
        //{
        //    // 先叠加加载场景，最后再卸载旧场景
        //    var engineHandle = YooAssets.LoadSceneSync("GameEngine", LoadSceneMode.Additive);
        //    await UniTask.DelayFrame(0);

        //    var entryHandle = YooAssets.LoadSceneSync("GameEntry", LoadSceneMode.Additive);
        //    await UniTask.DelayFrame(0);

        //    // 切换到Hall状态，新UI已创建
        //    GameEntry.Instance.ChangeState(GameState.Main);
        //    await UniTask.DelayFrame(2); // 确保UI显示

        //    // 卸载旧的场景
        //    await SceneManager.UnloadSceneAsync("GameLaunch");
        //    await SceneManager.UnloadSceneAsync("GameEngine");
        //}

    }
}
