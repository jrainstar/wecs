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

        /// <summary>
        /// 蓄力时间
        /// </summary>
        public FP chargeelapsed { get; set; }

        /// <summary>
        /// 蓄力中
        /// </summary>
        public bool charging { get; set; }

        /// <summary>
        /// 是否锁定移动
        /// 由时间轴动画节点的 Move 配置决定 (false = 施法期间禁止移动),
        /// 时间轴播完随 BehaviorInfo 一起移除, 解锁无需额外代码
        /// </summary>
        public bool lockmove { get; set; }

        protected override void OnReady()
        {
            OnReset();
        }

        protected override void OnReset()
        {
            id = 0;
            caster = 0;
            timescale = FP.One;
            timeelapsed = FP.Zero;
            chargeelapsed = FP.Zero;
            charging = false;
            lockmove = false;
        }

        protected override BehaviorInfo OnClone()
        {
            var clone = ObjectCache.Ensure<TimelineInfo>();
            clone.Ready(actor);

            clone.id = id;
            clone.caster = caster;
            clone.timescale = timescale;
            clone.timeelapsed = timeelapsed;
            clone.chargeelapsed = chargeelapsed;
            clone.charging = charging;
            clone.lockmove = lockmove;

            return clone;
        }
    }
}
