using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 座位信息翻译器
    /// </summary>
    public class SeatTranslator : Translator<SeatInfo, RIL_SEAT>
    {
        public override ushort id => RIL_DEFINE.SEAT;

        protected override int OnCalcHashCode(SeatInfo info)
        {
            int hash = 17;
            hash = hash * 31 + info.actor.GetHashCode();
            foreach (var kv in info.sadict)
            {
                hash = hash * 31 + kv.Key.GetHashCode();
                hash = hash * 31 + kv.Value.GetHashCode();
            }
            return hash;
        }

        protected override void OnRIL(SeatInfo info, RIL_SEAT ril)
        {
            foreach (var kv in info.sadict) ril.seatdict.Add(kv.Key, kv.Value);
        }
    }
}