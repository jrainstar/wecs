using GameEngine;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameLogic
{
    public class UIHall : UIWindow
    {
        public override int Layer => (int)UILayer.Middle;

        public override bool FullScreen => true;

        [SerializeField]
        [LabelText("用户信息")]
        private UIHallUserInfo m_UserInfo;

        [SerializeField]
        [LabelText("用户资源")]
        private UIHallResource m_Resource;

        [SerializeField]
        [LabelText("主菜单")]
        private UIHallMainMenu m_MainMenu;

        [SerializeField]
        [LabelText("子菜单")]
        private UIHallSubMenu m_SubMenu;

        [SerializeField]
        [LabelText("战斗")]
        private UIHallBattle m_Battle;

        [SerializeField]
        [LabelText("当前角色")]
        private UIHallRole m_Role;
    }
}
