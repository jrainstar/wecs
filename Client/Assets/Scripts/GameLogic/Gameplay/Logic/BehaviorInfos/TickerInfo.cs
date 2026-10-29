using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 驱动信息, 用于单个 Entity 的独立时间缩放
    /// </summary>
    public class TickerInfo : BehaviorInfo
    {
        /// <summary>
        /// 时间缩放
        /// </summary>
        public FP timescale { get; set; }

        protected override void OnReady()
        {
            OnReset();
        }

        protected override void OnReset()
        {
            timescale = FP.One;
        }

        protected override BehaviorInfo OnClone()
        {
            var clone = ObjectCache.Ensure<TickerInfo>();
            clone.Ready(actor);
            clone.timescale = timescale;

            return clone;
        }
    }
}
