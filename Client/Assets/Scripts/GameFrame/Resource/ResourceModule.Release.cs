using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;
using GameCore;
using Object = UnityEngine.Object;

namespace GameEngine
{
    /// <summary>
    /// ResourceModule - 资源释放/查询分部
    ///
    /// 提供：
    ///   - Release / ForceRelease / ReleaseAll：按 location 显式释放
    ///   - UnloadUnusedAssets（YooAsset 内部未使用回收）
    ///   - UnloadAllAssets（含/不含预加载）
    ///   - UnloadUnusedAssetsAsync / ForceUnloadAllAssetsAsync：与旧 API 兼容
    ///   - LoadAssetReference / UnloadAssetReference：AssetReference 组件用的引用计数 +/-
    ///   - GetRefCount / IsLoaded：查询
    /// </summary>
    public partial class ResourceModule
    {
        #region 显式释放
        /// <summary>
        /// 按 location 释放（引用计数 -1，为 0 且不是预加载条目时立即回收）
        /// </summary>
        public void Release(string location)
        {
            if (string.IsNullOrEmpty(location)) return;
            RollbackAsset(location);
        }

        /// <summary>强制释放某个 location（无论引用计数为多少）</summary>
        public void ForceRelease(string location)
        {
            var assetPath = ResolveAssetPath(location);
            if (assetPath == null) return;
            if (m_AssetObjectDict.TryGetValue(assetPath, out var assetObject))
            {
                m_AssetObjectDict.Remove(assetPath);
                MemoryPool.Release(assetObject);
            }
        }

        /// <summary>释放所有资源（含所有实例、场景）</summary>
        public void ReleaseAll()
        {
            // 1) 销毁所有已实例化对象（key 可能是 GameObject，也可能是 Component）
            foreach (var kv in m_InstanceToLocation)
            {
                if (kv.Key == null) continue;
                if (kv.Key is GameObject go)
                    Object.Destroy(go);
                else if (kv.Key is Component comp && comp.gameObject != null)
                    Object.Destroy(comp.gameObject);
                else
                    Object.Destroy(kv.Key);
            }
            m_InstanceToLocation.Clear();

            // 2) 释放所有 AssetObject（走 MemoryPool 归还，其 Clear 会释放 Handle）
            var keys = m_AssetObjectDict.Keys.ToList();
            foreach (var location in keys)
            {
                MemoryPool.Release(m_AssetObjectDict[location]);
                m_AssetObjectDict.Remove(location);
            }

            // 3) 场景句柄清空（不主动 UnloadScene，避免误伤当前 active 场景；Single 场景由引擎接管）
            m_SceneHandles.Clear();
        }
        #endregion

        #region YooAsset 卸载
        /// <summary>
        /// 卸载所有资源
        /// </summary>
        /// <param name="containsPreload">是否包含预载资源</param>
        public void UnloadAllAssets(bool containsPreload = false)
        {
            var keys = m_AssetObjectDict.Keys.ToList();
            foreach (var location in keys)
            {
                if (!containsPreload && m_AssetObjectDict[location].IsPreload)
                    continue;
                MemoryPool.Release(m_AssetObjectDict[location]);
                m_AssetObjectDict.Remove(location);
            }
        }

        /// <summary>卸载引擎中未使用的资源（YooAsset 的 UnloadUnusedAssets）</summary>
        public UnloadUnusedAssetsOperation UnloadUnusedAssets()
        {
            var package = GetPackageSafe();
            return package?.UnloadUnusedAssetsAsync();
        }

        /// <summary>释放所有未使用的资源（UniTask 版）</summary>
        public async UniTask wUnloadUnusedAssetsAsync()
        {
            if (m_UnusedAssetsOperation != null && m_UnusedAssetsOperation.Status == EOperationStatus.Processing)
            {
                Log.Warning("Unloading...,dont unload again!.");
                return;
            }
            var package = GetPackageSafe();
            if (package == null) return;
            m_UnusedAssetsOperation = package.UnloadUnusedAssetsAsync();
            await m_UnusedAssetsOperation.ToUniTask();
            m_UnusedAssetsOperation = null;
        }

        /// <summary>
        /// 卸载资源包所有资源（返回 Operation，供外部 await）
        /// 会先执行 ReleaseAll 保证内部登记清空，再走 YooAsset 卸载。
        /// </summary>
        public UnloadAllAssetsOperation UnloadAllAssetsWithOperation()
        {
            ReleaseAll();
            var package = GetPackageSafe();
            return package?.UnloadAllAssetsAsync();
        }

        /// <summary>强制释放所有资源。</summary>
        public async UniTaskVoid ForceUnloadAllAssetsAsync()
        {
            var package = GetPackageSafe();
            if (package == null) return;
            var operation = package.UnloadAllAssetsAsync();
            await operation.ToUniTask();
        }
        #endregion

        #region AssetReference 组件专用引用计数 +/-
        /// <summary>减少引用计数（AssetReference.OnDestroy 调用）</summary>
        public void UnloadAssetReference(string location)
        {
            if (string.IsNullOrEmpty(location)) return;
            RollbackAsset(location);
        }

        /// <summary>
        /// 增加引用计数（AssetReference.Init 调用）
        /// 注意：此路径不会加载资源，只对已存在于字典中的 AssetObject 做 +1。
        /// 因为 InstantiateGameObject 已经通过 LoadAssetSync 使计数 +1，
        /// 这里再 +1 是为了让 AssetReference 生命周期与实例强绑定；DestroyInstance 走 RollbackAsset -1 保持平衡。
        /// </summary>
        public void LoadAssetReference(string location)
        {
            var assetPath = ResolveAssetPath(location);
            if (assetPath == null) return;
            if (m_AssetObjectDict.TryGetValue(assetPath, out var assetObject))
                assetObject.ReferenceCount++;
        }
        #endregion

        #region 查询
        /// <summary>获取当前 location 的引用计数（不存在返回0）</summary>
        public int GetRefCount(string location)
        {
            var assetPath = ResolveAssetPath(location);
            if (assetPath == null) return 0;
            if (m_AssetObjectDict.TryGetValue(assetPath, out var assetObject))
                return assetObject.ReferenceCount;
            return 0;
        }

        /// <summary>是否已加载</summary>
        public bool IsLoaded(string location)
        {
            var assetPath = ResolveAssetPath(location);
            if (assetPath == null) return false;
            return m_AssetObjectDict.ContainsKey(assetPath);
        }
        #endregion
    }
}