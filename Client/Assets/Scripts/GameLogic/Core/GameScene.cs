using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFrame;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameLogic
{
    /// <summary>
    /// 场景管理器（静态配置）：把状态映射到场景，并在状态跨越场景时异步加载 / 卸载场景。
    /// 对应旧版 SceneManager（EStateType → 场景名）。
    ///
    /// 关键原则：只有「新状态场景名」与「旧状态场景名」不同时才真正加载 / 卸载；
    /// 同一场景（如大厅内所有状态）只走系统进出，不动场景。
    /// </summary>
    public static class GameScene
    {
        /// <summary>场景配置：场景名 + 加载模式（null 表示常驻场景，不参与加载 / 卸载）</summary>
        private readonly struct SceneConfig
        {
            public readonly string SceneName;
            public readonly LoadSceneMode Mode;

            public SceneConfig(string sceneName, LoadSceneMode mode)
            {
                SceneName = sceneName;
                Mode = mode;
            }
        }

        /// <summary>状态 → 场景配置（null 表示常驻场景，不参与加载 / 卸载）</summary>
        private static readonly Dictionary<GameState, SceneConfig> s_SceneMap = new()
        {
            { GameState.Root,          new SceneConfig(null, LoadSceneMode.Additive) },
            { GameState.Main,          new SceneConfig(null, LoadSceneMode.Additive) },
            { GameState.Hall,          new SceneConfig(null, LoadSceneMode.Additive) },
            { GameState.Task,          new SceneConfig(null, LoadSceneMode.Additive) },
            { GameState.Mail,          new SceneConfig(null, LoadSceneMode.Additive) },
            { GameState.Mall,          new SceneConfig(null, LoadSceneMode.Additive) },
            { GameState.Pack,          new SceneConfig(null, LoadSceneMode.Additive) },
            { GameState.UIPack,        new SceneConfig(null, LoadSceneMode.Additive) },
            { GameState.UIHero,        new SceneConfig(null, LoadSceneMode.Additive) },
            { GameState.Gameplay,      new SceneConfig("Gameplay", LoadSceneMode.Single) },
            { GameState.GameplayLocal, new SceneConfig("Gameplay1", LoadSceneMode.Single) },
        };

        /// <summary>场景加载进度（0~1）</summary>
        public static event Action<float> OnProgress;

        /// <summary>场景开始加载</summary>
        public static event Action<string> OnLoadStart;

        /// <summary>场景加载完成</summary>
        public static event Action<string> OnLoadDone;

        private static bool s_Loading;

        /// <summary>是否正在加载场景</summary>
        public static bool IsLoading => s_Loading;

        /// <summary>状态 → 场景名（null 表示不加载场景）</summary>
        public static string GetSceneName(GameState state)
        {
            return s_SceneMap.TryGetValue(state, out var config) ? config.SceneName : null;
        }

        /// <summary>
        /// 状态切换时对应的场景切换（场景名不同才真正加载）。
        /// 放在「离开旧系统」之后、「进入新系统」之前调用。
        /// </summary>
        public static async UniTask ChangeSceneAsync(GameState newState, GameState oldState)
        {
            var newScene = GetSceneName(newState);
            var oldScene = GetSceneName(oldState);

            if (string.Equals(newScene, oldScene))
                return;

            // 先卸载旧场景（Single 模式由引擎自动卸载，无需手动处理）
            if (!string.IsNullOrEmpty(oldScene))
                UnloadScene(oldScene);

            // 再加载新场景
            if (!string.IsNullOrEmpty(newScene))
                await LoadSceneAsync(newScene, GetSceneMode(newState));
        }

        /// <summary>状态 → 加载模式（默认 Additive）</summary>
        public static LoadSceneMode GetSceneMode(GameState state)
        {
            return s_SceneMap.TryGetValue(state, out var config) ? config.Mode : LoadSceneMode.Additive;
        }

        /// <summary>按配置的加载模式异步加载场景，加载完设为活动场景</summary>
        private static async UniTask LoadSceneAsync(string sceneName, LoadSceneMode mode)
        {
            if (s_Loading)
            {
                Debug.LogWarning($"[GameScene] 正在加载场景，忽略重复请求: {sceneName}");
                return;
            }

            s_Loading = true;
            OnLoadStart?.Invoke(sceneName);

            try
            {
                var handle = ResourceModule.Instance.LoadScene(sceneName, mode);
                if (handle == null)
                {
                    Debug.LogWarning($"[GameScene] 场景加载失败: {sceneName}");
                    return;
                }

                // 逐帧更新进度
                while (!handle.IsDone)
                {
                    OnProgress?.Invoke(handle.Progress);
                    await UniTask.Yield();
                }

                OnProgress?.Invoke(1f);

                // 设为活动场景，便于后续 Instantiate 默认挂到该场景
                var scene = handle.SceneObject;
                if (scene.IsValid())
                    SceneManager.SetActiveScene(scene);
            }
            finally
            {
                s_Loading = false;
                OnLoadDone?.Invoke(sceneName);
            }
        }

        /// <summary>卸载场景（仅 Additive 加载的场景需要手动卸载）</summary>
        public static void UnloadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return;

            ResourceModule.Instance.UnloadScene(sceneName);
        }
    }
}
