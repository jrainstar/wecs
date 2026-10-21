using UnityEngine;
using UnityEngine.UI;

namespace GameLaunch
{
    public class LoadWindow : MonoBehaviour
    {
        private float m_Progress;
        [SerializeField]
        private Slider m_SliderProgress;
        [SerializeField]
        private Text m_TextContent;
        [SerializeField]
        private Text m_TextVersion;

        private void Update()
        {
            m_SliderProgress.value = Mathf.SmoothStep(m_SliderProgress.value, m_Progress, 0.4f);
        }

        public void SetContent(string str)
        {
            m_TextContent.text = str;
        }

        public void SetProgress(float value, bool force = false)
        {
            m_Progress = value;
            if (force)
            {
                m_SliderProgress.value = value;
            }
        }

        public void SetVersion(string appVersion, string assetVersion)
        {
            m_TextVersion.text = $"{appVersion}_{assetVersion}";
        }


    }
}