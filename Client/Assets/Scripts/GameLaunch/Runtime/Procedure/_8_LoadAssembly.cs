#if ENABLE_HYBRID_CLR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using GameLaunch;
using HybridCLR;
using UnityEngine;
using YooAsset;

namespace GameLaunch
{
    public class _8_LoadAssembly : Procedure
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
            // 非Editor环境下，HotUpdate.dll.bytes已经被自动加载，不需要加载，重复加载反而会出问题。
#if !UNITY_EDITOR
            foreach (var aotDllName in App.PatchedAOTAssemblyList)
            {
                var handle = YooAssets.LoadAssetAsync<TextAsset>(aotDllName);
                await handle.Task;
                var aotDllAsset = handle.AssetObject as TextAsset;
                handle.Release();
                if (aotDllAsset != null)
                {
                    LoadImageErrorCode err = HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(aotDllAsset.bytes, HomologousImageMode.SuperSet);
                }
            }
            var hotAssemblies = new List<Assembly>();
            foreach (var hotDllName in App.HotAssemblyList)
            {
                var handle = YooAssets.LoadAssetAsync<TextAsset>(hotDllName);
                await handle.Task;
                var hotDllAsset = handle.AssetObject as TextAsset;
                handle.Release();
                if (hotDllAsset != null)
                {
                    hotAssemblies.Add(Assembly.Load(hotDllAsset.bytes));
                }
            }

#else
            //Editor下无需加载，直接查找获得HotUpdate程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var hotAssemblies = assemblies.Where(a => GameApp.HotAssemblyList.Contains(a.GetName().Name + ".dll")).ToList();
#endif
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
#endif