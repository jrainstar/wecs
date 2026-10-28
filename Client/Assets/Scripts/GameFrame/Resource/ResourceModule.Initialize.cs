using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;
using GameCore;

namespace GameFrame
{
    /// <summary>
    /// ResourceModule - 初始化/销毁分部
    ///
    /// 封装 YooAssets 的完整启动流程（三步不可缺）：
    ///   1) YooAssets.Initialize()（全局幂等）
    ///   2) YooAssets.CreatePackage(packageName)（若已存在则复用）
    ///   3) package.InitializePackageAsync(options)
    ///      + RequestPackageVersionAsync
    ///      + LoadPackageManifestAsync
    ///
    /// 使用示例：
    ///   await ResourceModule.Instance.InitEditorSimulateAsync("DefaultPackage");
    ///   await ResourceModule.Instance.InitOfflineAsync("DefaultPackage");
    ///   await ResourceModule.Instance.InitHostAsync("DefaultPackage", "https://cdn.your.com/game");
    ///   await ResourceModule.Instance.InitCustomAsync("DefaultPackage", myOptions);
    /// </summary>
    public partial class ResourceModule
    {
        #region 初始化状态
        /// <summary>YooAssets 是否已完成全局初始化</summary>
        public static bool IsGlobalInitialized => YooAssets.IsInitialized;

        /// <summary>
        /// 当前包是否已完全就绪（文件系统 + 清单都加载成功，可以安全调用 Load/Instantiate）。
        /// </summary>
        public bool IsPackageReady
        {
            get
            {
                if (!YooAssets.IsInitialized) return false;
                if (!YooAssets.TryGetPackage(PackageName, out var pkg)) return false;
                if (pkg.InitializeStatus != EOperationStatus.Succeeded) return false;
                try { return !string.IsNullOrEmpty(pkg.GetPackageVersion()); }
                catch { return false; }
            }
        }
        #endregion

        #region 全局初始化 & 包获取/创建（内部）
        /// <summary>确保 YooAssets 全局已初始化。幂等：重复调用无副作用。</summary>
        private static void EnsureYooAssetsInitialized()
        {
            if (!YooAssets.IsInitialized)
                YooAssets.Initialize();
        }

        /// <summary>获取或创建资源包（不做 InitializePackageAsync）。</summary>
        private static ResourcePackage GetOrCreatePackage(string packageName)
        {
            if (YooAssets.TryGetPackage(packageName, out var pkg))
                return pkg;
            return YooAssets.CreatePackage(packageName);
        }

        /// <summary>校验并设置当前使用的包名（内部）。</summary>
        private void SetActivePackageName(string packageName)
        {
            if (string.IsNullOrEmpty(packageName))
                throw new ArgumentException("[ResourceModule] packageName 不能为空", nameof(packageName));
            PackageName = packageName;
        }

        /// <summary>
        /// 执行 InitializePackageAsync 之后必需的两步：
        ///   1) RequestPackageVersionAsync
        ///   2) LoadPackageManifestAsync
        /// </summary>
        private static async UniTask<bool> RequestVersionAndLoadManifestAsync(ResourcePackage package, int manifestTimeout = 60)
        {
            var versionOp = package.RequestPackageVersionAsync();
            await versionOp.ToUniTask();
            if (versionOp.Status != EOperationStatus.Succeeded)
            {
                Log.Error($"[ResourceModule] RequestPackageVersion 失败: {versionOp.Error}");
                return false;
            }

            var manifestOp = package.LoadPackageManifestAsync(new LoadPackageManifestOptions(versionOp.PackageVersion, manifestTimeout));
            await manifestOp.ToUniTask();
            if (manifestOp.Status != EOperationStatus.Succeeded)
            {
                Log.Error($"[ResourceModule] LoadPackageManifest 失败: {manifestOp.Error}");
                return false;
            }

            Log.Info($"[ResourceModule] 清单加载完成，版本号: {versionOp.PackageVersion}");
            return true;
        }

        /// <summary>
        /// 协程版：RequestPackageVersion + LoadPackageManifest。通过 result[0] 回填成功/失败。
        /// </summary>
        private static IEnumerator RequestVersionAndLoadManifestCoroutine(ResourcePackage package, bool[] result, int manifestTimeout = 60)
        {
            var versionOp = package.RequestPackageVersionAsync();
            yield return versionOp;
            if (versionOp.Status != EOperationStatus.Succeeded)
            {
                Log.Error($"[ResourceModule] RequestPackageVersion 失败: {versionOp.Error}");
                result[0] = false;
                yield break;
            }

            var manifestOp = package.LoadPackageManifestAsync(new LoadPackageManifestOptions(versionOp.PackageVersion, manifestTimeout));
            yield return manifestOp;
            if (manifestOp.Status != EOperationStatus.Succeeded)
            {
                Log.Error($"[ResourceModule] LoadPackageManifest 失败: {manifestOp.Error}");
                result[0] = false;
                yield break;
            }

            Log.Info($"[ResourceModule] 清单加载完成，版本号: {versionOp.PackageVersion}");
            result[0] = true;
        }

        /// <summary>初始化成功后刷新内部 m_Package 缓存。</summary>
        private void RefreshPackageCache()
        {
            m_Package = GetPackageSafe();
        }
        #endregion

        #region 编辑器模拟模式 (EditorSimulateMode)
        /// <summary>
        /// 使用编辑器模拟模式初始化资源包（仅 Editor 可用）。
        /// 会自动执行一次 EditorSimulateBuild 拿到 PackageRoot。
        /// </summary>
        public async UniTask<bool> InitEditorSimulateAsync(string packageName)
        {
            SetActivePackageName(packageName);
            EnsureYooAssetsInitialized();
            var package = GetOrCreatePackage(packageName);

            if (package.InitializeStatus == EOperationStatus.Succeeded)
            {
                Log.Info($"[ResourceModule] 资源包 {packageName} 已初始化，跳过。");
                RefreshPackageCache();
                return true;
            }

#if UNITY_EDITOR
            var buildResult = EditorSimulateBuildInvoker.Build(packageName, (int)EBundleType.VirtualAssetBundle);
            var packageRoot = buildResult.PackageRootDirectory;

            var options = new EditorSimulateModeOptions
            {
                EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot)
            };

            var op = package.InitializePackageAsync(options);
            await op.ToUniTask();

            if (op.Status != EOperationStatus.Succeeded)
            {
                Log.Error($"[ResourceModule] EditorSimulate 初始化失败: {op.Error}");
                return false;
            }

            if (!await RequestVersionAndLoadManifestAsync(package))
                return false;

            RefreshPackageCache();
            Log.Info($"[ResourceModule] EditorSimulate 初始化成功: {packageName}");
            return true;
#else
            Log.Error("[ResourceModule] EditorSimulateMode 只能在 Unity Editor 下使用，请改用 InitOfflineAsync / InitHostAsync。");
            return false;
#endif
        }
        #endregion

        #region 单机离线模式 (OfflinePlayMode)
        /// <summary>使用单机离线模式初始化资源包（内置资源，无热更）。</summary>
        public async UniTask<bool> InitOfflineAsync(string packageName)
        {
            SetActivePackageName(packageName);
            EnsureYooAssetsInitialized();
            var package = GetOrCreatePackage(packageName);

            if (package.InitializeStatus == EOperationStatus.Succeeded)
            {
                Log.Info($"[ResourceModule] 资源包 {packageName} 已初始化，跳过。");
                RefreshPackageCache();
                return true;
            }

            var options = new OfflinePlayModeOptions
            {
                BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters()
            };

            var op = package.InitializePackageAsync(options);
            await op.ToUniTask();

            if (op.Status != EOperationStatus.Succeeded)
            {
                Log.Error($"[ResourceModule] Offline 初始化失败: {op.Error}");
                return false;
            }

            if (!await RequestVersionAndLoadManifestAsync(package))
                return false;

            RefreshPackageCache();
            Log.Info($"[ResourceModule] Offline 初始化成功: {packageName}");
            return true;
        }
        #endregion

        #region 联机热更模式 (HostPlayMode)
        /// <summary>使用联机热更模式初始化资源包。</summary>
        /// <param name="packageName">包名</param>
        /// <param name="hostUrl">主 CDN 地址</param>
        /// <param name="fallbackUrl">备用 CDN 地址（可传null，则复用 hostUrl）</param>
        public async UniTask<bool> InitHostAsync(string packageName, string hostUrl, string fallbackUrl = null)
        {
            if (string.IsNullOrEmpty(hostUrl))
                throw new ArgumentException("[ResourceModule] hostUrl 不能为空", nameof(hostUrl));

            SetActivePackageName(packageName);
            EnsureYooAssetsInitialized();
            var package = GetOrCreatePackage(packageName);

            if (package.InitializeStatus == EOperationStatus.Succeeded)
            {
                Log.Info($"[ResourceModule] 资源包 {packageName} 已初始化，跳过。");
                RefreshPackageCache();
                return true;
            }

            IRemoteService remoteService = new DefaultRemoteService(hostUrl, string.IsNullOrEmpty(fallbackUrl) ? hostUrl : fallbackUrl);

            var options = new HostPlayModeOptions
            {
                BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(),
                CacheFileSystemParameters = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(remoteService)
            };
            options.BuiltinFileSystemParameters.AddParameter(EFileSystemParameter.CopyBuiltinPackageManifest, true);

            var op = package.InitializePackageAsync(options);
            await op.ToUniTask();

            if (op.Status != EOperationStatus.Succeeded)
            {
                Log.Error($"[ResourceModule] Host 初始化失败: {op.Error}");
                return false;
            }

            if (!await RequestVersionAndLoadManifestAsync(package))
                return false;

            RefreshPackageCache();
            Log.Info($"[ResourceModule] Host 初始化成功: {packageName} <- {hostUrl}");
            return true;
        }
        #endregion

        #region 自定义 Options
        /// <summary>使用自定义 InitializePackageOptions 初始化资源包（如 WebPlayMode）。</summary>
        public async UniTask<bool> InitCustomAsync(string packageName, InitializePackageOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            SetActivePackageName(packageName);
            EnsureYooAssetsInitialized();
            var package = GetOrCreatePackage(packageName);

            if (package.InitializeStatus == EOperationStatus.Succeeded)
            {
                Log.Info($"[ResourceModule] 资源包 {packageName} 已初始化，跳过。");
                RefreshPackageCache();
                return true;
            }

            var op = package.InitializePackageAsync(options);
            await op.ToUniTask();

            if (op.Status != EOperationStatus.Succeeded)
            {
                Log.Error($"[ResourceModule] Custom 初始化失败: {op.Error}");
                return false;
            }

            if (!await RequestVersionAndLoadManifestAsync(package))
                return false;

            RefreshPackageCache();
            Log.Info($"[ResourceModule] Custom 初始化成功: {packageName}");
            return true;
        }
        #endregion

        #region 协程兜底（无 UniTask 环境用）
        /// <summary>协程版初始化（Editor 模拟）。onComplete: 成功=true，失败=false。</summary>
        public IEnumerator InitEditorSimulateCoroutine(string packageName, Action<bool> onComplete = null)
        {
            SetActivePackageName(packageName);
            EnsureYooAssetsInitialized();
            var package = GetOrCreatePackage(packageName);

            if (package.InitializeStatus == EOperationStatus.Succeeded)
            {
                RefreshPackageCache();
                onComplete?.Invoke(true);
                yield break;
            }

#if UNITY_EDITOR
            var buildResult = EditorSimulateBuildInvoker.Build(packageName, (int)EBundleType.VirtualAssetBundle);
            var options = new EditorSimulateModeOptions
            {
                EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(buildResult.PackageRootDirectory)
            };
            var op = package.InitializePackageAsync(options);
            yield return op;
            if (op.Status != EOperationStatus.Succeeded)
            {
                Log.Error($"[ResourceModule] EditorSimulate 初始化失败: {op.Error}");
                onComplete?.Invoke(false);
                yield break;
            }

            var result = new bool[1];
            yield return RequestVersionAndLoadManifestCoroutine(package, result);
            if (result[0]) RefreshPackageCache();
            onComplete?.Invoke(result[0]);
#else
            Log.Error("[ResourceModule] EditorSimulateMode 只能在 Editor 下使用。");
            onComplete?.Invoke(false);
            yield break;
#endif
        }
        #endregion

        #region 销毁
        /// <summary>销毁当前包并从 YooAssets 中移除。销毁前会释放所有引用。</summary>
        public async UniTask DestroyPackageAsync()
        {
            ReleaseAll();

            if (!YooAssets.IsInitialized) return;
            if (!YooAssets.TryGetPackage(PackageName, out var package)) return;

            var op = package.DestroyPackageAsync();
            await op.ToUniTask();
            YooAssets.RemovePackage(PackageName);
            m_Package = null;
            Log.Info($"[ResourceModule] 资源包已销毁并移除: {PackageName}");
        }

        /// <summary>销毁 YooAssets 全局（会销毁并移除所有包）。慎用，一般在退出游戏时调用。</summary>
        public void DestroyGlobal()
        {
            ReleaseAll();
            if (YooAssets.IsInitialized)
            {
                YooAssets.Destroy();
                m_Package = null;
                Log.Info("[ResourceModule] YooAssets 全局已销毁。");
            }
        }
        #endregion

        #region 内置 RemoteService 默认实现
        /// <summary>
        /// 简易 RemoteService：把 bundle 请求拼接为 {host}/{fileName}。
        /// 如需签名/CDN分发策略，请自行实现 IRemoteService 并通过 InitCustomAsync 传入。
        /// </summary>
        private class DefaultRemoteService : IRemoteService
        {
            private readonly string m_MainHost;
            private readonly string m_FallbackHost;

            public DefaultRemoteService(string mainHost, string fallbackHost)
            {
                m_MainHost = mainHost;
                m_FallbackHost = fallbackHost;
            }

            public string GetRemoteMainURL(string fileName) => $"{m_MainHost}/{fileName}";
            public string GetRemoteFallbackURL(string fileName) => $"{m_FallbackHost}/{fileName}";

            public IReadOnlyList<string> GetRemoteUrls(string fileName)
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
}
