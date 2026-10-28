using Cysharp.Threading.Tasks;
using System;
using UnityEngine.SceneManagement;
using YooAsset;
using SceneHandle = YooAsset.SceneHandle;

namespace GameFrame
{
    /// <summary>
    /// ResourceModule - 场景加载分部
    /// 提供：
    ///   - 旧 API：LoadSceneAsync（Single）/ LoadAddSceneAsync（Additive），自带资源卸载与事件回调
    ///   - 新 API：LoadScene / UnloadScene（句柄式，配合 m_SceneHandles 登记，便于 Inspector 查看）
    /// </summary>
    public partial class ResourceModule
    {
        #region 旧 API（UniTask 单向流程）
        /// <summary>
        /// 异步切换场景（Single）：加载完毕会自动卸载之前的资源。
        /// 注意！加载资源逻辑写在 await 之后，不然会因为 UnloadAllAssets 造成资源丢失。
        /// </summary>
        public async UniTask<Scene> LoadSceneAsync(string location)
        {
            var package = RequirePackage();
            var handle = package.LoadSceneAsync(location, LoadSceneMode.Single);

            // Single 会卸载其他场景，清理已缓存句柄
            m_SceneHandles.Clear();
            m_SceneHandles[location] = handle;

            await handle.ToUniTask();
            await UniTask.Yield();
            UnloadAllAssets(false);
            await UnloadUnusedAssets();
            await UniTask.Yield();
            OnNewSceneLoadDone?.Invoke(location);
            return handle.SceneObject;
        }

        /// <summary>
        /// 异步切换场景（Single）：加载完毕会自动卸载之前的资源，并在加载过程中回调进度。
        /// </summary>
        /// <param name="location">场景地址</param>
        /// <param name="onProgress">加载进度回调（0~1），可为 null</param>
        public async UniTask<Scene> LoadSceneAsync(string location, Action<float> onProgress)
        {
            var package = RequirePackage();
            var handle = package.LoadSceneAsync(location, LoadSceneMode.Single);

            // Single 会卸载其他场景，清理已缓存句柄
            m_SceneHandles.Clear();
            m_SceneHandles[location] = handle;

            await WaitSceneHandleWithProgress(handle, onProgress);

            await UniTask.Yield();
            UnloadAllAssets(false);
            await UnloadUnusedAssets();
            await UniTask.Yield();
            OnNewSceneLoadDone?.Invoke(location);
            return handle.SceneObject;
        }

        /// <summary>异步加载附加场景（Additive）</summary>
        public async UniTask<Scene> LoadAddSceneAsync(string location, bool active)
        {
            var package = RequirePackage();
            var handle = package.LoadSceneAsync(location, LoadSceneMode.Additive);
            m_SceneHandles[location] = handle;

            await handle.ToUniTask();
            if (active)
                handle.ActivateScene();
            await UniTask.Yield();
            return handle.SceneObject;
        }

        /// <summary>等待场景句柄完成，并在等待过程中回报进度（0~1）</summary>
        private async UniTask WaitSceneHandleWithProgress(SceneHandle handle, Action<float> onProgress)
        {
            if (onProgress == null)
            {
                await handle.ToUniTask();
                return;
            }

            onProgress(0f);
            while (!handle.IsDone)
            {
                onProgress(handle.Progress);
                await UniTask.Yield();
            }
            onProgress(1f);
        }
        #endregion

        #region 新 API（句柄式）
        /// <summary>
        /// 异步加载场景（返回 SceneHandle 句柄，不做资源卸载）
        /// </summary>
        public SceneHandle LoadScene(string location, LoadSceneMode mode = LoadSceneMode.Single, bool activateOnLoad = true)
        {
            var package = GetPackageSafe();
            if (package == null) return null;

            var handle = package.LoadSceneAsync(location, mode, allowSceneActivation: activateOnLoad);

            // Single 模式会卸载其他场景，清理已缓存句柄
            if (mode == LoadSceneMode.Single)
                m_SceneHandles.Clear();

            m_SceneHandles[location] = handle;
            return handle;
        }

        /// <summary>卸载场景（仅 Additive 加载的场景需要手动卸载）</summary>
        public void UnloadScene(string location)
        {
            if (m_SceneHandles.TryGetValue(location, out var handle))
            {
                m_SceneHandles.Remove(location);
                handle.UnloadSceneAsync();
            }
        }
        #endregion
    }
}