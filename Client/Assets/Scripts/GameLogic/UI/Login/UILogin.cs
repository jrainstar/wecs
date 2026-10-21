using GameEngine;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class UILogin : UIWindow
    {
        public override int Layer => (int)UILayer.Middle;

        public override bool FullScreen => true;

        [SerializeField]
        [LabelText("开始游戏")]
        private Button m_Login;
    }
}
