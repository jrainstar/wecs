using System;
using System.Collections;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;
using GameCore;
using Object = UnityEngine.Object;

namespace GameFrame
{
    /// <summary>
    /// ResourceModule - 资源加载分部
    /// 包含：同步 / UniTask / 回调 / 协程 / Task 五种加载方式 + 子资源加载
    /// 引用计数：每次 Load 会使对应 AssetObject.ReferenceCount +1；失败或取消会回滚。
    /// </summary>
    public partial class ResourceModule
    {
        #region 同步加载
        /// <summary>
        /// 同步加载资源（引用计数 +1）
        /// </summary>
        public T LoadAssetSync<T>(string location) where T : Object
        {
            if (string.IsNullOrEmpty(location))
            {
                Log.Error("[ResourceModule] LoadAssetSync location 为空");
                return null;
            }

            AssetObject assetObject;
            try
            {
                assetObject = GetAssetObject(location);
            }
            catch (Exception e)
            {
                Log.Error($"[ResourceModule] LoadAssetSync 失败: {location}, {e.Message}");
                return null;
            }
            if (assetObject == null) return null;

            assetObject.AssetHandle.WaitForAsyncComplete();

            if (assetObject.Status == EOperationStatus.Failed)
            {
                Log.Error($"[ResourceModule] {assetObject.AssetHandle.Error}");
                RollbackAsset(location);
                return null;
            }
            if (assetObject.AssetHandle.AssetObject == null)
            {
                Log.Error($"[ResourceModule] Load asset failed, asset is null. {location}");
                RollbackAsset(location);
                return null;
            }
            return assetObject.AssetHandle.AssetObject as T;
        }

        /// <summary>同步加载资源（引用计数 +1）—— 与 LoadAssetSync 等价的短别名</summary>
        public T LoadAsset<T>(string location) where T : Object => LoadAssetSync<T>(location);
        #endregion

        #region UniTask 加载（不带 CancellationToken 的简化版）
        /// <summary>异步加载资源（引用计数 +1）—— UniTask</summary>
        public async UniTask<T> LoadAssetAsync<T>(string location) where T : Object
        {
            if (string.IsNullOrEmpty(location))
            {
                Log.Error("[ResourceModule] LoadAssetAsync location 为空");
                return null;
            }

            AssetObject assetObject;
            try
            {
                assetObject = GetAssetObject(location);
            }
            catch (Exception e)
            {
                Log.Error($"[ResourceModule] LoadAssetAsync 失败: {location}, {e.Message}");
                return null;
            }
            if (assetObject == null) return null;

            if (assetObject.AssetHandle.AssetObject != null)
                return assetObject.AssetHandle.AssetObject as T;

            await assetObject.AssetHandle.ToUniTask();

            if (assetObject.Status == EOperationStatus.Failed)
            {
                Log.Error($"[ResourceModule] {assetObject.AssetHandle.Error}");
                RollbackAsset(location);
                return null;
            }
            if (assetObject.AssetHandle.AssetObject == null)
            {
                Log.Error($"[ResourceModule] Load asset failed, asset is null! location:{location}");
                RollbackAsset(location);
                return null;
            }
            return assetObject.AssetHandle.AssetObject as T;
        }

        /// <summary>异步加载资源 + 回调（引用计数 +1）</summary>
        public async UniTaskVoid LoadAssetAsync<T>(string location, Action<T> callback) where T : Object
        {
            var asset = await LoadAssetAsync<T>(location);
            callback?.Invoke(asset);
        }
        #endregion

        #region 协程加载
        /// <summary>
        /// 协程方式加载资源（引用计数 +1）
        /// 用法：yield return ResourceModule.Instance.LoadAssetCoroutine&lt;GameObject&gt;("xx", asset => { ... });
        /// </summary>
        public IEnumerator LoadAssetCoroutine<T>(string location, Action<T> onComplete) where T : Object
        {
            if (string.IsNullOrEmpty(location))
            {
                onComplete?.Invoke(null);
                yield break;
            }

            AssetObject assetObject = null;
            bool getFailed = false;
            try
            {
                assetObject = GetAssetObject(location);
            }
            catch (Exception e)
            {
                Log.Error($"[ResourceModule] LoadAssetCoroutine 失败: {location}, {e.Message}");
                getFailed = true;
            }
            if (getFailed || assetObject == null)
            {
                onComplete?.Invoke(null);
                yield break;
            }

            if (!assetObject.AssetHandle.IsDone)
                yield return assetObject.AssetHandle;

            if (assetObject.Status != EOperationStatus.Succeeded || assetObject.AssetHandle.AssetObject == null)
            {
                Log.Error($"[ResourceModule] 协程加载失败: {location}, {assetObject.AssetHandle.Error}");
                RollbackAsset(location);
                onComplete?.Invoke(null);
                yield break;
            }
            onComplete?.Invoke(assetObject.AssetHandle.AssetObject as T);
        }
        #endregion

        #region Task 加载
        /// <summary>Task 方式加载资源（引用计数 +1）</summary>
        public async Task<T> LoadAssetTask<T>(string location) where T : Object
        {
            if (string.IsNullOrEmpty(location)) return null;

            AssetObject assetObject;
            try
            {
                assetObject = GetAssetObject(location);
            }
            catch (Exception e)
            {
                Log.Error($"[ResourceModule] LoadAssetTask 失败: {location}, {e.Message}");
                return null;
            }
            if (assetObject == null) return null;

            if (!assetObject.AssetHandle.IsDone)
                await assetObject.AssetHandle;

            if (assetObject.Status != EOperationStatus.Succeeded || assetObject.AssetHandle.AssetObject == null)
            {
                Log.Error($"[ResourceModule] Task加载失败: {location}, {assetObject.AssetHandle.Error}");
                RollbackAsset(location);
                return null;
            }
            return assetObject.AssetHandle.AssetObject as T;
        }
        #endregion

        #region 子资源加载
        /// <summary>同步加载子资源（引用计数 +1）</summary>
        public T LoadSubAssetsSync<T>(string location, string name) where T : Object
        {
            AssetObject assetObject;
            try
            {
                assetObject = GetSubAssetsObject(location);
            }
            catch (Exception e)
            {
                Log.Error($"[ResourceModule] LoadSubAssetsSync 失败: {location}, {e.Message}");
                return null;
            }
            if (assetObject == null) return null;

            assetObject.SubAssetsHandle.WaitForAsyncComplete();

            if (assetObject.Status == EOperationStatus.Failed)
            {
                Log.Error($"[ResourceModule] {assetObject.SubAssetsHandle.Error}");
                RollbackAsset(location);
                return null;
            }
            return assetObject.SubAssetsHandle.GetSubAssetObject<T>(name);
        }

        /// <summary>异步加载子资源（引用计数 +1）</summary>
        public async UniTask<T> LoadSubAssetsAsync<T>(string location, string name) where T : Object
        {
            AssetObject assetObject;
            try
            {
                assetObject = GetSubAssetsObject(location);
            }
            catch (Exception e)
            {
                Log.Error($"[ResourceModule] LoadSubAssetsAsync 失败: {location}, {e.Message}");
                return null;
            }
            if (assetObject == null) return null;

            await assetObject.SubAssetsHandle.ToUniTask();

            if (assetObject.Status == EOperationStatus.Failed)
            {
                Log.Error($"[ResourceModule] {assetObject.SubAssetsHandle.Error}");
                RollbackAsset(location);
                return null;
            }
            return assetObject.SubAssetsHandle.GetSubAssetObject<T>(name);
        }

        /// <summary>异步加载子资源 + 回调（引用计数 +1）</summary>
        public async UniTaskVoid LoadSubAssetsAsync<T>(string location, string name, Action<T> callback) where T : Object
        {
            var asset = await LoadSubAssetsAsync<T>(location, name);
            callback?.Invoke(asset);
        }
        #endregion
    }
}
