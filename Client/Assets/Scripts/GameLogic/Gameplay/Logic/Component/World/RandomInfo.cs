using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 随机信息
    /// </summary>
    public class RandomInfo : Component
    {
        /// <summary>
        /// 乘数
        /// </summary>
        public long A { get; private set; }
        /// <summary>
        /// 增量
        /// </summary>
        public long C { get; private set; }
        /// <summary>
        /// 模数
        /// </summary>
        public long M { get; private set; }
        /// <summary>
        /// 随机种子
        /// </summary>
        public long Seed { get; set; }
        /// <summary>
        /// 最新随机数
        /// </summary>
        public long Current { get; set; }

        protected override void OnReady()
        {
            OnReset();
        }

        protected override void OnReset()
        {
            A = 1664525;
            C = 1013904223;
            M = int.MaxValue;
            Seed = 0;
            Current = 0;
        }

        protected override Component OnClone()
        {
            var clone = ObjectCache.Ensure<RandomInfo>();
            clone.Ready(Entity);
            clone.Seed = Seed;
            clone.Current = Current;

            return clone;
        }
    }
}
