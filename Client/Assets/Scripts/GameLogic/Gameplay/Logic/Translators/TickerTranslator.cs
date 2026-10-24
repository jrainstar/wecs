namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 驱动翻译器
    /// </summary>
    public class TickerTranslator : Translator<TickerInfo, RIL_TICKER>
    {
        public override ushort id => RIL_DEFINE.TICKER;

        protected override int OnCalcHashCode(TickerInfo info)
        {
            var hash = info.actor.GetHashCode() ^ info.timescale.GetHashCode();
            return hash * 31 + stage.timescale.GetHashCode();
        }

        protected override void OnRIL(TickerInfo info, RIL_TICKER ril)
        {
            ril.timescale = (stage.timescale * info.timescale * stage.cfg.fp2int).AsUInt();
        }
    }
}