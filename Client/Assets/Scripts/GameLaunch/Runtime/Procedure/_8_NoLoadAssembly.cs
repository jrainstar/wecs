using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using GameLaunch;
using UnityEngine;
using YooAsset;

namespace GameLaunch
{
    public class _8_NoLoadAssembly : Procedure
    {
        public override void OnEnter(object data)
        {
            // App.Instance.SetContent("LoadAssembly...");
            GameApp.Instance.SetContent("加载程序集...");
            LoadAssembly().Forget();
        }

#pragma warning disable CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行
        private async UniTaskVoid LoadAssembly()
#pragma warning restore CS1998 // 异步方法缺少 "await" 运算符，将以同步方式运行
        {
            await UniTask.Delay(100);

            //Editor下无需加载，直接查找获得HotUpdate程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var hotAssemblies = assemblies.Where(a => GameApp.HotAssemblyList.Contains(a.GetName().Name + ".dll")).ToList();

            if (hotAssemblies.Count == 0)
            {
                Debug.LogError("HotUpdateAssembly has not been found.");
                GameApp.Instance.ShowTip("提示", "HotUpdateAssembly has not been found.", confirmAction: Application.Quit);
                return;
            }

            ChangeProcedure<_9_EnterGame>(hotAssemblies);
        }
    }
}
