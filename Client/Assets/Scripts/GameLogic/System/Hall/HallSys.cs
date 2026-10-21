using Cysharp.Threading.Tasks;
using GameEngine;
using System.Threading.Tasks;

namespace GameLogic 
{
    public class HallSys : GameSystem
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
