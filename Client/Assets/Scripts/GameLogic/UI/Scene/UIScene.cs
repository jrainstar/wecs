using GameFrame;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class UIScene : UIWindow
    {
        public override int Layer => (int)UILayer.Mask;

        public override bool FullScreen => true;

        [SerializeField]
        [LabelText("进度条")]
        private Slider m_Progress;

        [SerializeField]
        [LabelText("进度文本")]
        private TextMeshProUGUI m_ProgressText;

        protected override void OnShow()
        {
            base.OnShow();
            SetProgress(0f);
            SceneModule.Instance.OnProgress += OnSceneProgress;
            SceneModule.Instance.OnLoadDone += OnSceneLoadDone;
        }

        protected override void OnHide()
        {
            SceneModule.Instance.OnProgress -= OnSceneProgress;
            SceneModule.Instance.OnLoadDone -= OnSceneLoadDone;
            base.OnHide();
        }

        private void OnSceneProgress(float progress)
        {
            SetProgress(progress);
        }

        private void OnSceneLoadDone(string sceneName)
        {
            SetProgress(1f);
            UIModule.Instance.HideWindow<UIScene>();
        }

        private void SetProgress(float progress)
        {
            if (m_Progress != null)
                m_Progress.value = progress;

            if (m_ProgressText != null)
                m_ProgressText.text = $"{(progress * 100f):F0}%";
        }

        // ── 静态入口 ─────────────────────────────────────────────

        /// <summary>显示加载界面并开始加载场景</summary>
        public static async void LoadScene(string sceneName, string tip = null)
        {
            UIModule.Instance.ShowWindowSync<UIScene>();
            await SceneModule.Instance.LoadSceneAsync(sceneName);
        }
    }
}
