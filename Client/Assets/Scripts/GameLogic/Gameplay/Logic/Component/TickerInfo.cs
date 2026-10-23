using GameLogic.Gameplay.Shared;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 驱动信息, 用于单个 Entity 的独立时间缩放
    /// </summary>
    public class TickerInfo : Component
    {
        private FP mTimeScale = FP.One;
        /// <summary>
        /// 时间缩放
        /// </summary>
        public FP TimeScale
        {
            get => mTimeScale;
            set => mTimeScale = FPMath.Clamp(value, 0, FP.MaxValue);
        }

        protected override void OnReady()
        {
            OnReset();
        }

        protected override void OnReset()
        {
            TimeScale = FP.One;
        }

        protected override Component OnClone()
        {
            var clone = ObjectCache.Ensure<TickerInfo>();
            clone.Ready(Entity);
            clone.TimeScale = TimeScale;

            return clone;
        }
    }
}
