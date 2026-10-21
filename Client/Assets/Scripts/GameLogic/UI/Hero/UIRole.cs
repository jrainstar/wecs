using GameEngine;
using Sirenix.OdinInspector;
using SuperScrollView;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class UIRole : UIWindow
    {
        public override int Layer => (int)UILayer.Middle;

        public override bool FullScreen => true;

        [SerializeField]
        [LabelText("关闭")]
        private Button m_Close;

        [SerializeField]
        [LabelText("选择")]
        private Button m_Select;

        [SerializeField]
        [LabelText("英雄列表")]
        private UIRoleItems m_HeroItems;

        [SerializeField]
        [LabelText("英雄信息")]
        private UIRoleInfo m_HeroInfo;

        private void Awake()
        {
            m_Close.onClick.AddListener(OnClose);
            m_Select.onClick.AddListener(OnSelect);

            m_HeroItems.OnSelectedChanged += OnHeroSelectedChanged;
        }

        private void Start()
        {
            // 初始化时默认选中第一个，刷新 UIHeroInfo 数据
            m_HeroItems.NotifyCurrentSelected();
        }

        private void OnEnable()
        {
            // 窗口被 UIModule.ShowWindowSync 复用（非首次打开）时，
            // Start 不再执行，OnEnable 中重新刷一次当前选中
            m_HeroItems?.NotifyCurrentSelected();
        }

        private void OnHeroSelectedChanged(GameConf.Gameplay.Role.RoleData heroData)
        {
            if (heroData == null)
            {
                return;
            }
            m_HeroInfo.SetData(heroData);
        }

        private void OnSelect()
        {
            var heroData = m_HeroItems.CurrentHeroData;
            if (heroData == null)
            {
                return;
            }

            // 通过事件派发设置当前角色，HeroSys 会处理持久化存储
            EventModule.Instance.Dispatch(EventDefine.SetHeroData, heroData.Id);

            // 设置完成后返回上一级
            // GameEntry.Instance.GoBack();
        }

        private void OnClose()
        {
            GameEntry.Instance.GoBack();
            // UISys.Instance.HideWindow<UIHero>();
        }

        private void OnDestroy()
        {
            m_Close.onClick.RemoveListener(OnClose);
            m_Select.onClick.RemoveListener(OnSelect);

            m_HeroItems.OnSelectedChanged -= OnHeroSelectedChanged;
        }
    }
}