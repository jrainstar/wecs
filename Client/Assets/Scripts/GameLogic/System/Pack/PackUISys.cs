using GameFrame;

namespace GameLogic
{
    public class PackUISys : GameSystem
    {
        public override void OnEnter()
        {
            base.OnEnter();
            UIModule.Instance.ShowWindowSync<UIPack>();
        }
    }

}
