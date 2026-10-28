using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 状态机信息翻译器
    /// </summary>
    public class StateMachineTranslator : Translator<StateMachineInfo, RIL_STATE_MACHINE>
    {
        public override ushort id => RIL_DEFINE.STATE_MACHINE;

        protected override int OnCalcHashCode(StateMachineInfo info)
        {
            int hash = 17;
            hash = hash * 31 + info.actor.GetHashCode();
            hash = hash * 31 + info.current.GetHashCode();

            return hash;
        }

        protected override void OnRIL(StateMachineInfo info, RIL_STATE_MACHINE ril)
        {
            ril.current = info.current;
            ril.last = info.last;
        }
    }
}