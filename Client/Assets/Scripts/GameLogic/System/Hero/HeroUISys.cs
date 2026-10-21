using GameEngine;

namespace GameLogic
{
    public class HeroUISys : GameSystem
    {
        public override void OnEnter()
        {
            base.OnEnter();
            UIModule.Instance.ShowWindowSync<UIRole>();
        }

        public override void OnLeave()
        {
            UIModule.Instance.HideWindow<UIRole>();
            base.OnLeave();
        }
    }

}
