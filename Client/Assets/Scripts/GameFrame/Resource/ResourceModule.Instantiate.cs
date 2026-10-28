using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;
using GameCore;
using Object = UnityEngine.Object;

namespace GameFrame
{
    /// <summary>
    /// ResourceModule - 预制体实例化分部
    /// 泛型 API：Instantiate&lt;T&gt; / InstantiateAsync&lt;T&gt; / DestroyInstance&lt;T&gt;
    ///   - T 支持 GameObject 或挂在预制体根节点上的任意 Component（如 MonoBehaviour 子类）
    ///   - 引用计数：Instantiate +1；DestroyInstance -1
    ///
    /// 兼容 API：InstantiateGameObject / InstantiateGameObjectAsync（保留原 AssetReference 路径）
    /// </summary>
    public partial class ResourceModule
    {
        #region 泛型实例化
        /// <summary>
        /// 同步加载并实例化预制体（引用计数 +1）
        /// </summary>
        public T Instantiate<T>(string location, Transform parent = null) where T : Object
        {
            var prefab = LoadAssetSync<GameObject>(location);
            if (prefab == null) return null;

            // 统一以字典里的 AssetPath 作为实例登记 key，保证 DestroyInstance -> RollbackAsset 命中
            var assetPath = ResolveAssetPath(location) ?? location;

            var instanceGo = Object.Instantiate(prefab, parent);
            if (instanceGo == null)
            {
                RollbackAsset(assetPath);
                return null;
            }

            if (typeof(T) == typeof(GameObject))
            {
                m_InstanceToLocation[instanceGo] = assetPath;
                return instanceGo as T;
            }

            var comp = instanceGo.GetComponent<T>();
            if (comp == null)
            {
                Log.Error($"[ResourceModule] Instantiate<{typeof(T).Name}> 失败：预制体 {location} 上未找到组件 {typeof(T).Name}");
                Object.Destroy(instanceGo);
                RollbackAsset(assetPath);
                return null;
            }
            m_InstanceToLocation[comp] = assetPath;
            return comp;
        }

        /// <summary>
        /// 异步加载并实例化预制体（引用计数 +1）—— UniTask 版本
        /// </summary>
        public async UniTask<T> InstantiateAsync<T>(string location, Transform parent = null) where T : Object
        {
            var prefab = await LoadAssetAsync<GameObject>(location);
            if (prefab == null) return null;

            var assetPath = ResolveAssetPath(location) ?? location;

            var instanceGo = Object.Instantiate(prefab, parent);
            if (instanceGo == null)
            {
                RollbackAsset(assetPath);
                return null;
            }

            if (typeof(T) == typeof(GameObject))
            {
                m_InstanceToLocation[instanceGo] = assetPath;
                return instanceGo as T;
            }

            var comp = instanceGo.GetComponent<T>();
            if (comp == null)
            {
                Log.Error($"[ResourceModule] InstantiateAsync<{typeof(T).Name}> 失败：预制体 {location} 上未找到组件 {typeof(T).Name}");
                Object.Destroy(instanceGo);
                RollbackAsset(assetPath);
                return null;
            }
            m_InstanceToLocation[comp] = assetPath;
            return comp;
        }

        /// <summary>
        /// 异步加载并实例化预制体（引用计数 +1）—— 回调版本
        /// </summary>
        public void InstantiateAsync<T>(string location, Action<T> onComplete, Transform parent = null) where T : Object
        {
            InstantiateAsyncInternal(location, onComplete, parent).Forget();
        }

        private async UniTaskVoid InstantiateAsyncInternal<T>(string location, Action<T> onComplete, Transform parent) where T : Object
        {
            var result = await InstantiateAsync<T>(location, parent);
            onComplete?.Invoke(result);
        }

        /// <summary>
        /// 销毁实例化对象，并释放对应引用计数（-1）
        /// 支持传入 GameObject 或 Component（Component 会自动销毁其所在 GameObject）
        /// </summary>
        public void DestroyInstance<T>(T instance) where T : Object
        {
            if (instance == null) return;

            GameObject go = instance as GameObject;
            if (go == null && instance is Component comp)
                go = comp.gameObject;

            if (m_InstanceToLocation.TryGetValue(instance, out var location))
            {
                m_InstanceToLocation.Remove(instance);
                if (go != null) Object.Destroy(go);
                RollbackAsset(location);
            }
            else
            {
                // 不是通过 ResourceModule 实例化的（或以别的引用形式登记），直接销毁 GameObject
                if (go != null) Object.Destroy(go);
            }
        }
        #endregion

        #region 兼容：AssetReference 路径（旧 API）
        // 注意：这条路径的引用计数由 AssetReference（Init +1 / OnDestroy -1）负责，
        //       所以内部加载走 refCountDelta=0 的探查形式，不与 AssetReference 累计。

        /// <summary>
        /// 同步加载预制体并实例化（挂 AssetReference 自动 +/-1 计数）—— 旧 API，保留兼容
        /// </summary>
        public GameObject InstantiateGameObject(string location, Transform parent = null)
        {
            AssetObject assetObject;
            try
            {
                // refCountDelta=0：仅加载，不改变引用计数，把计数职责交给 AssetReference
                assetObject = GetAssetObject(location, refCountDelta: 0);
                assetObject.AssetHandle.WaitForAsyncComplete();
            }
            catch (Exception e)
            {
                Log.Error($"[ResourceModule] InstantiateGameObject 失败: {location}, {e.Message}");
                return null;
            }

            if (assetObject.Status != EOperationStatus.Succeeded || assetObject.AssetHandle.AssetObject == null)
            {
                Log.Error($"[ResourceModule] InstantiateGameObject 失败: {location}, {assetObject.AssetHandle.Error}");
                return null;
            }

            var prefab = assetObject.AssetHandle.AssetObject as GameObject;
            if (prefab == null) return null;

            var instantiate = Object.Instantiate(prefab, parent);
            var assetReference = instantiate.AddComponent<AssetReference>();
            assetReference.Init(assetObject.Location); // +1（用 AssetPath 作为 key，保证 OnDestroy 时能命中字典）
            return instantiate;
        }

        /// <summary>同步加载预制体（保留原拼写别名以兼容旧调用）</summary>
        [Obsolete("请使用 InstantiateGameObject（原方法名拼写错误）", false)]
        public GameObject InstantiateGameObjec(string location, Transform parent = null)
            => InstantiateGameObject(location, parent);

        /// <summary>
        /// 异步加载预制体并实例化（挂 AssetReference 自动 +/-1 计数）—— 旧 API，保留兼容
        /// </summary>
        public async UniTask<GameObject> InstantiateGameObjectAsync(string location, Transform parent = null)
        {
            AssetObject assetObject;
            try
            {
                assetObject = GetAssetObject(location, refCountDelta: 0);
            }
            catch (Exception e)
            {
                Log.Error($"[ResourceModule] InstantiateGameObjectAsync 失败: {location}, {e.Message}");
                return null;
            }

            if (!assetObject.AssetHandle.IsDone)
                await assetObject.AssetHandle.ToUniTask();

            if (assetObject.Status != EOperationStatus.Succeeded || assetObject.AssetHandle.AssetObject == null)
            {
                Log.Error($"[ResourceModule] InstantiateGameObjectAsync 失败: {location}, {assetObject.AssetHandle.Error}");
                return null;
            }

            var prefab = assetObject.AssetHandle.AssetObject as GameObject;
            if (prefab == null) return null;

            var instantiate = Object.Instantiate(prefab, parent);
            var assetReference = instantiate.AddComponent<AssetReference>();
            assetReference.Init(assetObject.Location); // +1（用 AssetPath 作为 key，保证 OnDestroy 时能命中字典）
            return instantiate;
        }
        #endregion
    }
}