using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 外观动画翻译器
    /// </summary>
    public class FacadeAnimationTranslator : Translator<FacadeInfo, RIL_FACADE_ANIMATION>
    {
        public override ushort id => RIL_DEFINE.FACADE_ANIMATION;

        protected override int OnCalcHashCode(FacadeInfo info)
        {
            int hash = 17;
            hash = hash * 31 + info.actor.GetHashCode();
            hash = hash * 31 + info.animstate.GetHashCode();
            hash = hash * 31 + info.animspeed.GetHashCode();
            hash = hash * 31 + (info.animname != null ? info.animname.GetHashCode() : 0);
            hash = hash * 31 + info.animloop.GetHashCode();
            hash = hash * 31 + info.animelapsed.GetHashCode();

            return hash;
        }

        protected override void OnRIL(FacadeInfo info, RIL_FACADE_ANIMATION ril)
        {
            ril.animstate = info.animstate;
            ril.animspeed = (info.animspeed * stage.cfg.fp2int).AsUInt();
            ril.animname = info.animname;
            ril.animloop = info.animloop;
            ril.animelapsed = (info.animelapsed * stage.cfg.fp2int).AsUInt();
        }
    }
}