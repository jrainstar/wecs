using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 运动信息
    /// </summary>
    public class MovementInfo : BehaviorInfo
    {
        /// <summary>
        /// 当前帧驱动了运动, 标记
        /// </summary>
        public bool turnmotion { get; set; }

        protected override void OnReady()
        {
            OnReset();
        }

        protected override void OnReset()
        {
            turnmotion = false;
        }

        protected override BehaviorInfo OnClone()
        {
            var clone = ObjectCache.Ensure<MovementInfo>();
            clone.Ready(actor);
            clone.turnmotion = turnmotion;

            return clone;
        }
    }
}