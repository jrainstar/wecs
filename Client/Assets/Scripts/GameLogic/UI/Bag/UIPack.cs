using GameEngine;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GameLogic
{
    public class UIPack : UIWindow
    {
        public override int Layer => (int)UILayer.Middle;

        public override bool FullScreen => true;

    }
}
