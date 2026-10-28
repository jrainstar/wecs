using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using GameCore;
using SceneHandle = YooAsset.SceneHandle;

namespace GameFrame
{
    /// <summary>
    /// ResourceModule - 预加载分部
    ///
    /// 提供：
    ///   1. PreLoadAssetAsyncByTag / PreLoadAssetAsyncByLocation：按标签或单地址预加载（旧 API）
    ///   2. Preload / PreloadUniTask：批量预加载 + 进度回调
    ///   3. PreloadAndLoadScene / PreloadAndLoadSceneUniTask：预加载+切场景一站式
    ///
    /// 预加载条目 isPreload=true，不会被 Update 自动回收；引用计数按 Load 语义 +1，需要显式 Release。
    /// </summary>
    public partial class ResourceModule
    {
        #region 单项预加载（旧 API）
        /// <summary>根据标签异步预加载资源（引用计数 +1）</summary>
        public async UniTask PreLoadAssetAsyncByTag(string tag)
        {
            var package = RequirePackage();
            var assetInfos = package.GetAssetInfos(tag);
            if (assetInfos == null) return;
            foreach (var assetInfo in assetInfos)
            {
                // 统一使用 AssetPath，不走可寻址
                var assetObject = GetAssetObject(assetInfo.AssetPath, isPreload: true);
                if (assetObject == null) continue;
                await assetObject.AssetHandle.ToUniTask();
            }
        }

        /// <summary>根据地址异步预加载资源（引用计数 +1）</summary>
        public async UniTask PreLoadAssetAsyncByLocation(string location)
        {
            var assetObject = GetAssetObject(location, isPreload: true);
            if (assetObject == null) return;
            await assetObject.AssetHandle.ToUniTask();
        }
        #endregion

        #region 预加载条目
        /// <summary>单个预加载条目</summary>
        public struct PreloadItem
        {
            /// <summary>资源 location</summary>
            public string Location;
            /// <summary>资源类型（如 typeof(GameObject) / typeof(AudioClip) / typeof(Texture) 等）</summary>
            public Type AssetType;

            public PreloadItem(string location, Type assetType)
            {
                Location = location;
                AssetType = assetType;
            }
        }
        #endregion

        #region 批量预加载 - 协程
        /// <summary>批量预加载资源（进度回调 + 全部完成回调）</summary>
        public Coroutine Preload(List<PreloadItem> items, Action<float> onProgress = null, Action onComplete = null)
        {
            return StartCoroutine(PreloadCoroutine(items, onProgress, onComplete));
        }

        private IEnumerator PreloadCoroutine(List<PreloadItem> items, Action<float> onProgress, Action onComplete)
        {
            if (items == null || items.Count == 0)
            {
                onProgress?.Invoke(1f);
                onComplete?.Invoke();
                yield break;
            }

            var package = GetPackageSafe();
            if (package == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var handles = new List<AssetHandle>(items.Count);
            var locations = new List<string>(items.Count);

            // 发起加载
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.Location) || item.AssetType == null)
                {
                    handles.Add(null);
                    locations.Add(null);
                    continue;
                }

                AssetObject assetObject = null;
                try { assetObject = GetAssetObject(item.Location, isPreload: true); }
                catch (Exception e) { Log.Error($"[ResourceModule] Preload 发起失败: {item.Location}, {e.Message}"); }

                handles.Add(assetObject?.AssetHandle);
                locations.Add(assetObject?.Location);
            }

            // 等待
            while (true)
            {
                float totalProgress = 0f;
                int doneCount = 0;
                for (int i = 0; i < handles.Count; i++)
                {
                    var h = handles[i];
                    if (h == null) { doneCount++; totalProgress += 1f; continue; }
                    totalProgress += h.Progress;
                    if (h.IsDone) doneCount++;
                }

                onProgress?.Invoke(totalProgress / handles.Count);

                if (doneCount >= handles.Count) break;
                yield return null;
            }

            // 失败回滚
            for (int i = 0; i < handles.Count; i++)
            {
                var h = handles[i];
                if (h == null) continue;
                if (h.Status != EOperationStatus.Succeeded)
                {
                    Log.Error($"[ResourceModule] 预加载失败: {locations[i]}, {h.Error}");
                    RollbackAsset(locations[i]);
                }
            }

            onProgress?.Invoke(1f);
            onComplete?.Invoke();
        }
        #endregion

        #region 批量预加载 - UniTask
        /// <summary>UniTask 方式批量预加载资源</summary>
        public async UniTask PreloadUniTask(
            List<PreloadItem> items,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (items == null || items.Count == 0)
            {
                progress?.Report(1f);
                return;
            }

            var package = GetPackageSafe();
            if (package == null) return;

            var handles = new List<AssetHandle>(items.Count);
            var locations = new List<string>(items.Count);

            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.Location) || item.AssetType == null)
                {
                    handles.Add(null);
                    locations.Add(null);
                    continue;
                }

                AssetObject assetObject = null;
                try { assetObject = GetAssetObject(item.Location, isPreload: true); }
                catch (Exception e) { Log.Error($"[ResourceModule] PreloadUniTask 发起失败: {item.Location}, {e.Message}"); }

                handles.Add(assetObject?.AssetHandle);
                locations.Add(assetObject?.Location);
            }

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    float totalProgress = 0f;
                    int doneCount = 0;
                    for (int i = 0; i < handles.Count; i++)
                    {
                        var h = handles[i];
                        if (h == null) { doneCount++; totalProgress += 1f; continue; }
                        totalProgress += h.Progress;
                        if (h.IsDone) doneCount++;
                    }

                    progress?.Report(totalProgress / handles.Count);

                    if (doneCount >= handles.Count) break;
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // 取消：回滚所有本次 +1（包括尚未完成的与已完成的）
                for (int i = 0; i < handles.Count; i++)
                {
                    if (handles[i] == null) continue;
                    RollbackAsset(locations[i]);
                }
                throw;
            }

            for (int i = 0; i < handles.Count; i++)
            {
                var h = handles[i];
                if (h == null) continue;
                if (h.Status != EOperationStatus.Succeeded)
                {
                    Log.Error($"[ResourceModule] UniTask预加载失败: {locations[i]}, {h.Error}");
                    RollbackAsset(locations[i]);
                }
            }

            progress?.Report(1f);
        }
        #endregion

        #region 预加载 + 切场景（协程）
        /// <summary>预加载资源并切换场景（推荐入口）</summary>
        public Coroutine PreloadAndLoadScene(
            string sceneLocation,
            List<PreloadItem> items,
            Action<float> onProgress = null,
            Action<SceneHandle> onComplete = null,
            LoadSceneMode sceneMode = LoadSceneMode.Single)
        {
            return StartCoroutine(PreloadAndLoadSceneCoroutine(sceneLocation, items, onProgress, onComplete, sceneMode));
        }

        private IEnumerator PreloadAndLoadSceneCoroutine(
            string sceneLocation,
            List<PreloadItem> items,
            Action<float> onProgress,
            Action<SceneHandle> onComplete,
            LoadSceneMode sceneMode)
        {
            // 阶段1: 资源预加载（占总进度 0~0.5）
            bool resDone = false;
            yield return PreloadCoroutine(items,
                p => onProgress?.Invoke(p * 0.5f),
                () => resDone = true);
            while (!resDone) yield return null;

            // 阶段2: 加载场景（占总进度 0.5~1）
            var sceneHandle = LoadScene(sceneLocation, sceneMode);
            if (sceneHandle == null)
            {
                onComplete?.Invoke(null);
                yield break;
            }

            while (!sceneHandle.IsDone)
            {
                onProgress?.Invoke(0.5f + sceneHandle.Progress * 0.5f);
                yield return null;
            }

            onProgress?.Invoke(1f);
            onComplete?.Invoke(sceneHandle);
        }
        #endregion

        #region 预加载 + 切场景（UniTask）
        /// <summary>UniTask 方式：预加载资源并切换场景</summary>
        public async UniTask<SceneHandle> PreloadAndLoadSceneUniTask(
            string sceneLocation,
            List<PreloadItem> items,
            IProgress<float> progress = null,
            LoadSceneMode sceneMode = LoadSceneMode.Single,
            CancellationToken cancellationToken = default)
        {
            IProgress<float> phase1 = progress == null ? null : new Progress<float>(p => progress.Report(p * 0.5f));
            await PreloadUniTask(items, phase1, cancellationToken);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sceneHandle = LoadScene(sceneLocation, sceneMode);
                if (sceneHandle == null)
                {
                    RollbackPreloadItems(items);
                    progress?.Report(1f);
                    return null;
                }

                while (!sceneHandle.IsDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    progress?.Report(0.5f + sceneHandle.Progress * 0.5f);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                progress?.Report(1f);
                return sceneHandle;
            }
            catch (OperationCanceledException)
            {
                // 场景阶段被取消：回滚阶段1的所有资源引用
                RollbackPreloadItems(items);
                throw;
            }
        }

        /// <summary>批量回滚一批预加载项的引用计数（每项 -1）</summary>
        private void RollbackPreloadItems(List<PreloadItem> items)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.Location))
                    RollbackAsset(item.Location);
            }
        }
        #endregion
    }
}
