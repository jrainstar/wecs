using Cysharp.Threading.Tasks;
using GameFrame;
using Sirenix.OdinInspector;
using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class UIMain : UIWindow
    {
        public override int Layer => (int)UILayer.Middle;

        public override bool FullScreen => true;

        [SerializeField]
        [LabelText("开始游戏")]
        private Button m_Start;

        [SerializeField]
        [LabelText("网络状态")]
        private TextMeshProUGUI m_NetStatus;

        private void Awake()
        {
            m_Start.onClick.AddListener(OnStartButtonClick);
            EventModule.Instance.Subscribe(EventDefine.NetConnectComplete, OnNetConnectComplete);
            EventModule.Instance.Subscribe(EventDefine.NetConnectFail, OnNetConnectFail);
            EventModule.Instance.Subscribe(EventDefine.NetDisconnect, OnNetDisconnect);
        }

        private void OnDestroy()
        {
            m_Start.onClick.RemoveListener(OnStartButtonClick);
            EventModule.Instance?.Unsubscribe(EventDefine.NetConnectComplete, OnNetConnectComplete);
            EventModule.Instance?.Unsubscribe(EventDefine.NetConnectFail, OnNetConnectFail);
            EventModule.Instance?.Unsubscribe(EventDefine.NetDisconnect, OnNetDisconnect);
        }

        private void OnStartButtonClick()
        {
            GameEntry.Instance.ChangeState(GameState.Hall);
        }

        private void OnNetConnectComplete(object obj)
        {
            ShowNetInfo("网络连接成功").Forget();
        }

        private void OnNetConnectFail(object obj)
        {
            ShowNetInfo("网络连接失败").Forget();
        }

        private void OnNetDisconnect(object obj)
        {
            ShowNetInfo("网络连接断开").Forget();
        }

        private async UniTask ShowNetInfo(string info)
        {
            m_NetStatus.text = info;
            m_NetStatus.gameObject.SetActive(true);
            await UniTask.Delay(1000);
            m_NetStatus.gameObject.SetActive(false);
        }
    }
}
