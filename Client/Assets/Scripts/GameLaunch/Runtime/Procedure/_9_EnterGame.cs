using Cysharp.Threading.Tasks;
using GameLaunch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using YooAsset;

namespace GameLaunch
{
    public class _9_EnterGame : Procedure
    {
        public override void OnEnter(object data)
        {
            GameApp.Instance.SetProgress(100, true);
            GameApp.Instance.SetContent("进入游戏中...");

            var hotAssemblies = (List<Assembly>)data;
            EnterGame(hotAssemblies).Forget();

        }

        private async UniTaskVoid EnterGame(List<Assembly> hotAssemblies)
        {
            //执行热更代码

            await UniTask.Delay(100);

            Type mainType = null;

            foreach (var assembly in hotAssemblies)
            {
                if (assembly.GetName().Name == "GameLogic")
                {
                    mainType = assembly.GetType("GameLogic.Main");
                }

            }

            if (mainType == null)
            {
                UnityEngine.Debug.LogError("Class GameLogic.Main is null.");
                GameApp.Instance.ShowTip("提示", "Class GameLogic.Main is null.");
                return;
            }

            var enterFunction = mainType.GetMethod("Entrance");
            if (enterFunction == null)
            {
                UnityEngine.Debug.LogError("Main's function Entrance is null.");
                GameApp.Instance.ShowTip("提示", "Main's function Entrance is null.");
                return;
            }

            enterFunction.Invoke(mainType, new object[] { new object[] { hotAssemblies } });
        }
    }
}