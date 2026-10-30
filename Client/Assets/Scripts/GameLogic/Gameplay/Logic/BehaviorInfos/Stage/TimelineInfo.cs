using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 驱动信息, 用于单个 Entity 的独立时间缩放
    /// </summary>
    public class TimelineInfo : BehaviorInfo
    {
        /// <summary>
        /// 时间轴
        /// </summary>
        public int id { get; set; }

        /// <summary>
        /// 释放者
        /// </summary>
        public ulong caster { get; set; }

        /// <summary>
        /// 时间缩放
        /// </summary>
        public FP timescale { get; set; }

        /// <summary>
        /// 运行时间
        /// </summary>
        public FP timeelapsed { get; set; }

        protected override void OnReady()
        {
            id = 0;
            caster = 0;
            timescale = FP.One;
            timeelapsed = FP.Zero;
        }

        protected override void OnReset()
        {
            id = 0;
            caster = 0;
            timescale = FP.One;
            timeelapsed = FP.Zero;
        }

        protected override BehaviorInfo OnClone()
        {
            var clone = ObjectCache.Ensure<TimelineInfo>();
            clone.Ready(actor);

            clone.id = id;
            clone.caster = caster;
            clone.timescale = timescale;
            clone.timeelapsed = timeelapsed;

            return clone;
        }
    }
}
