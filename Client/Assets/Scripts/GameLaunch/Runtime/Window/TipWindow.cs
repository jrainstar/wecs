using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameLaunch
{
    public class TipWindow : MonoBehaviour
    {
        [SerializeField]
        private Text m_TextTitle;
        [SerializeField]
        private Text m_TextContent;
        [SerializeField]
        private Button m_ButtonCancel;
        [SerializeField]
        private Button m_ButtonConfirm;

        public void Show(string title, string content, UnityAction cancelAction = null, UnityAction confirmAction = null)
        {
            gameObject.SetActive(true);

            m_TextTitle.text = title;
            m_TextContent.text = content;

            m_ButtonCancel.onClick.RemoveAllListeners();
            m_ButtonConfirm.onClick.RemoveAllListeners();

            m_ButtonCancel.onClick.AddListener(cancelAction);
            m_ButtonConfirm.onClick.AddListener(confirmAction);
            m_ButtonCancel.onClick.AddListener(Hide);
            m_ButtonConfirm.onClick.AddListener(Hide);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

    }
}