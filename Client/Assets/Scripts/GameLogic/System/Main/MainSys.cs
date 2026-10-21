using GameEngine;

namespace GameLogic
{
    public class MainSys : GameSystem
    {
        public override void OnEnter()
        {
            base.OnEnter();

            UIModule.Instance.ShowWindowSync<UIMain>();
        }

        public override void OnLeave()
        {
            UIModule.Instance.HideWindow<UIMain>();
            base.OnLeave();
        }
    }

}
