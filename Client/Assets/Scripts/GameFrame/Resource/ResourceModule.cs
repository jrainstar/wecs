using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;
using GameCore;
using Object = UnityEngine.Object;
using SceneHandle = YooAsset.SceneHandle;

namespace GameEngine
{
    /// <summary>
    /// 资源管理系统（基于 YooAsset）
    ///
    /// 分部文件：
    ///   - ResourceModule.cs           主文件：单例/字段/包管理/生命周期
    ///   - ResourceModule.Init.cs      YooAssets 初始化（Editor/Offline/Host/Custom）
    ///   - ResourceModule.Load.cs      资源加载（同步/回调/协程/Task/UniTask + 子资源）
    ///   - ResourceModule.UniTask.cs   UniTask 加载（带 CancellationToken/Progress）
    ///   - ResourceModule.Instantiate.cs 预制体实例化（泛型 GameObject/Component）
    ///   - ResourceModule.Scene.cs     场景加载/卸载
    ///   - ResourceModule.Preload.cs   批量预加载（含"预加载+切场景"一站式）
    ///   - ResourceModule.Release.cs   引用计数释放 + 未使用资源卸载
    ///   - ResourceModule.Debug.cs     Inspector 只读快照
    ///
    /// 引用计数语义（Q1=✅）：
    ///   每次 Load / Instantiate 会使对应 AssetObject.ReferenceCount +1；
    ///   Release / DestroyInstance 会 -1；
    ///   Update 定时扫描 ReferenceCount == 0 且长时间未使用的资源，自动 MemoryPool.Release。
    /// </summary>
    public partial class ResourceModule : GameModule<ResourceModule>
    {
        #region 包管理
        public string PackageName { get; private set; } = "DefaultPackage";

        /// <summary>
        /// 缓存当前使用的资源包实例，避免每次都走 YooAssets.GetPackage（会抛异常）。
        /// </summary>
        private ResourcePackage m_Package;

        /// <summary>资源包版本</summary>
        public string PackageVersion
        {
            get
            {
                if (!YooAssets.IsInitialized) return string.Empty;
                var pkg = GetPackageSafe();
                if (pkg == null) return string.Empty;
                try { return pkg.GetPackageVersion() ?? string.Empty; }
                catch { return string.Empty; }
            }
        }

        /// <summary>是否开启自动回收</summary>
        public bool OpenUnloadTimer { get; set; } = true;

        /// <summary>当新场景加载完毕</summary>
        public event Action<string> OnNewSceneLoadDone;
        #endregion

        #region 内部数据结构
        /// <summary>资源引用池：location -> AssetObject（引用计数容器）</summary>
        private readonly Dictionary<string, AssetObject> m_AssetObjectDict = new Dictionary<string, AssetObject>();

        /// <summary>预卸载资源缓存（供 Update 遍历时缓冲移除，避免 foreach 边遍历边修改）</summary>
        private readonly List<string> m_CacheUnloadAssetObjects = new List<string>();

        /// <summary>
        /// 实例化对象 -> 对应资源的 location（用于 DestroyInstance 时定位并减引用）
        /// key 使用 Object 基类，以支持 GameObject 及 Component（MonoBehaviour 等）泛型实例化
        /// </summary>
        private readonly Dictionary<Object, string> m_InstanceToLocation = new Dictionary<Object, string>();

        /// <summary>场景 location -> 场景句柄（用于新 API LoadScene/UnloadScene）</summary>
        private readonly Dictionary<string, SceneHandle> m_SceneHandles = new Dictionary<string, SceneHandle>();
        #endregion

        #region Update 相关
        /// <summary>资源在此时间内不使用则释放掉（单位：秒）</summary>
        private int m_NoUseUnloadTime = 15;

        /// <summary>资源自动检测释放时间（单位：秒）</summary>
        private int m_AutoCheckUnloadTimeInterval = 30;

        /// <summary>资源自动检测释放计时器</summary>
        private float m_CheckUnloadTimer;

        /// <summary>资源释放句柄</summary>
        private UnloadUnusedAssetsOperation m_UnusedAssetsOperation;
        #endregion

        #region 生命周期
        protected override void Awake()
        {
            base.Awake();
            // 只做一次探测；不强制要求包已存在，允许外部先初始化 ResourceModule 再初始化包
            m_Package = GetPackageSafe();
            if (m_Package == null)
            {
                Log.Warning($"[ResourceModule] YooAssets 包 '{PackageName}' 尚未准备好，请先完成 Init*Async 三步初始化。");
            }
        }

        internal void Update()
        {
            if (!OpenUnloadTimer) return;

            m_CheckUnloadTimer += Time.deltaTime;
            if (m_UnusedAssetsOperation == null && m_CheckUnloadTimer > m_AutoCheckUnloadTimeInterval)
            {
                m_CheckUnloadTimer = 0;
                var nowTicks = DateTime.Now.Ticks;
                // 秒 → Ticks（1 秒 = 10,000,000 ticks）
                var thresholdTicks = TimeSpan.FromSeconds(m_NoUseUnloadTime).Ticks;
                foreach (var assetObject in m_AssetObjectDict.Values)
                {
                    if (!assetObject.IsPreload
                        && nowTicks - assetObject.LastUseTime >= thresholdTicks
                        && !assetObject.IsLoading
                        && assetObject.ReferenceCount == 0)
                    {
                        m_CacheUnloadAssetObjects.Add(assetObject.Location);
                        MemoryPool.Release(assetObject);
                    }
                }

                foreach (var location in m_CacheUnloadAssetObjects)
                {
                    m_AssetObjectDict.Remove(location);
                }
                m_CacheUnloadAssetObjects.Clear();

                UnloadUnusedAssets();
            }
        }

        internal void Clear()
        {
            OnNewSceneLoadDone = null;
            ReleaseAll();
            ForceUnloadAllAssetsAsync().Forget();
        }
        #endregion

        #region 包访问 / 切换
        /// <summary>由外部（例如启动流程）在包初始化完成后切换当前使用的包。</summary>
        public void SetPackage(string packageName)
        {
            if (string.IsNullOrEmpty(packageName))
                throw new ArgumentException("[ResourceModule] packageName 不能为空", nameof(packageName));
            PackageName = packageName;
            m_Package = GetPackageSafe();
        }

        /// <summary>拿包实例；未初始化或未创建则返回 null，不抛异常。</summary>
        internal ResourcePackage GetPackageSafe()
        {
            if (!YooAssets.IsInitialized) return null;
            try
            {
                if (YooAssets.TryGetPackage(PackageName, out var pkg))
                    return pkg;
            }
            catch
            {
                // CheckInitialized 可能因为未初始化抛异常，兜底吞掉
            }
            return null;
        }

        /// <summary>拿包实例；未准备好抛异常（供加载路径调用）。</summary>
        internal ResourcePackage RequirePackage()
        {
            if (m_Package != null) return m_Package;
            m_Package = GetPackageSafe();
            if (m_Package == null)
                throw new InvalidOperationException($"[ResourceModule] 资源包 '{PackageName}' 未初始化，无法执行资源操作。");
            return m_Package;
        }

        /// <summary>
        /// 获取资源信息，如果资源不存在则返回的 assetInfo.Address 为 ""
        /// </summary>
        public AssetInfo GetAssetInfo(string location)
        {
            return RequirePackage().GetAssetInfo(location);
        }

        /// <summary>
        /// 将外部传入的 location 解析为字典 key（AssetPath）。
        /// - 若字典里已存在同名 key，直接返回（避免每次都走 YooAssets 查询）。
        /// - 否则通过 YooAssets 解析成 AssetPath；解析失败返回 null（表示资源不存在）。
        /// 供 Release / ForceRelease / RollbackAsset / GetRefCount / IsLoaded 等释放/查询路径统一使用。
        /// </summary>
        internal string ResolveAssetPath(string location)
        {
            if (string.IsNullOrEmpty(location)) return null;
            // 已经是标准 AssetPath，命中最快路径
            if (m_AssetObjectDict.ContainsKey(location)) return location;

            var package = GetPackageSafe();
            if (package == null) return null;
            try
            {
                var assetInfo = package.GetAssetInfo(location);
                return string.IsNullOrEmpty(assetInfo.AssetPath) ? null : assetInfo.AssetPath;
            }
            catch
            {
                return null;
            }
        }
        #endregion

        #region AssetObject 获取（内部核心）
        /// <summary>
        /// 获取资源句柄（内部核心方法）
        /// </summary>
        /// <param name="location">资源地址</param>
        /// <param name="isPreload">是否为预加载条目（预加载条目不会被 Update 自动回收）</param>
        /// <param name="refCountDelta">
        /// 引用计数变化量（默认 +1）
        /// - 1（默认）：Load 语义，本次调用 +1
        /// - 0：仅探查，不改变引用计数（供 UnloadAssetReference/PreLoadAssetAsyncByTag 等特殊场景用）
        /// </param>
        internal AssetObject GetAssetObject(string location, bool isPreload = false, int refCountDelta = 1)
        {
            if (string.IsNullOrEmpty(location))
                throw new ArgumentException($"[ResourceModule] location [{location}] is invalid.");

            var package = RequirePackage();
            var assetInfo = package.GetAssetInfo(location);

            // 统一以 AssetPath 作为唯一键，不使用可寻址(Address)
            if (string.IsNullOrEmpty(assetInfo.AssetPath))
                throw new Exception($"[ResourceModule] can not find asset location: {location} !");

            var assetPath = assetInfo.AssetPath;
            if (!m_AssetObjectDict.TryGetValue(assetPath, out var assetObject))
            {
                var assetHandle = package.LoadAssetAsync(assetPath);
                if (!assetHandle.IsValid || assetHandle.Status == EOperationStatus.Failed)
                    throw new Exception($"{assetHandle.Error}");

                assetObject = MemoryPool.Acquire<AssetObject>();
                assetObject.Init(assetPath, assetHandle, isPreload);
                m_AssetObjectDict.Add(assetPath, assetObject);
            }

            assetObject.LastUseTime = DateTime.Now.Ticks;
            if (refCountDelta != 0)
                assetObject.ReferenceCount += refCountDelta;
            return assetObject;
        }

        /// <summary>
        /// 获取子资源句柄（内部核心方法）
        /// </summary>
        internal AssetObject GetSubAssetsObject(string location, bool isPreload = false, int refCountDelta = 1)
        {
            if (string.IsNullOrEmpty(location))
                throw new ArgumentException($"[ResourceModule] location [{location}] is invalid.");

            var package = RequirePackage();
            var assetInfo = package.GetAssetInfo(location);

            // 统一以 AssetPath 作为唯一键，不使用可寻址(Address)
            if (string.IsNullOrEmpty(assetInfo.AssetPath))
                throw new Exception($"[ResourceModule] can not find asset location: {location} !");

            var assetPath = assetInfo.AssetPath;
            if (!m_AssetObjectDict.TryGetValue(assetPath, out var assetObject))
            {
                var subHandle = package.LoadSubAssetsAsync(assetPath);
                if (!subHandle.IsValid || subHandle.Status == EOperationStatus.Failed)
                    throw new Exception($"{subHandle.Error}");

                assetObject = MemoryPool.Acquire<AssetObject>();
                assetObject.Init(assetPath, subHandle, isPreload);
                m_AssetObjectDict.Add(assetPath, assetObject);
            }

            assetObject.LastUseTime = DateTime.Now.Ticks;
            if (refCountDelta != 0)
                assetObject.ReferenceCount += refCountDelta;
            return assetObject;
        }

        /// <summary>
        /// 回滚一次引用计数：-1；若归零并且不在预加载状态，会立即从字典移除并回收到 MemoryPool。
        /// 供加载失败、UniTask 被取消、DestroyInstance 等场景调用。
        /// 允许传入原始 location（可能不是 AssetPath），内部会解析成字典 key 再操作。
        /// </summary>
        internal void RollbackAsset(string location)
        {
            var assetPath = ResolveAssetPath(location);
            if (assetPath == null) return;
            if (!m_AssetObjectDict.TryGetValue(assetPath, out var assetObject)) return;

            assetObject.ReferenceCount--;
            if (assetObject.ReferenceCount <= 0 && !assetObject.IsPreload)
            {
                m_AssetObjectDict.Remove(assetPath);
                MemoryPool.Release(assetObject);
            }
        }
        #endregion
    }
}