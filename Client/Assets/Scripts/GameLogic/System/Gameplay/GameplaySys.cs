using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    public class GameplaySys : GameSystem
    {
        override public void OnEnter()
        {
            base.OnEnter();
            var cam = Camera.main;
            cam.enabled = false;
            cam.enabled = true;
        }

        public override UniTask OnEnterAsync()
        {
            var cam = Camera.main;
            cam.enabled = false;
            cam.enabled = true;
            return base.OnEnterAsync();
        }
    }

}
