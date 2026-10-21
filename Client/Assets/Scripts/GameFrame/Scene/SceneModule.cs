using System;
using Cysharp.Threading.Tasks;

namespace GameEngine
{
    public class SceneModule : GameModule<SceneModule>
    {
        public event Action<float> OnProgress;
        public event Action<string> OnLoadStart;
        public event Action<string> OnLoadDone;

        private bool m_IsLoading;

        public bool IsLoading => m_IsLoading;

        protected override void DynamicAwake()
        {
            base.DynamicAwake();
            transform.parent = GameModules.Instance.transform;
        }

        public async UniTask LoadSceneAsync(string sceneName)
        {
            if (m_IsLoading) return;
            m_IsLoading = true;

            OnLoadStart?.Invoke(sceneName);
            await ResourceModule.Instance.LoadSceneAsync(sceneName, OnSceneProgress);
            OnLoadDone?.Invoke(sceneName);
            m_IsLoading = false;
        }

        private void OnSceneProgress(float progress)
        {
            OnProgress?.Invoke(progress);
        }
    }
}
