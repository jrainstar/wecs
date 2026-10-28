using GameFrame;

namespace GameLogic
{
    public class LoginSys : GameSystem
    {
        public override void OnEnter()
        {
            base.OnEnter();
            UIModule.Instance.ShowWindowSync<UIHall>();
        }

        public override void OnLeave()
        {
            UIModule.Instance.HideWindow<UIHall>();
            base.OnLeave();
        }
    }
}
