using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 运动行为
    /// </summary>
    public class Timeline : Behavior<TimelineInfo>
    {
        protected override void OnTick(FP tick)
        {
            base.OnTick(tick);
            if (false == stage.SeekBehaviorInfos(out List<TimelineInfo> timelines)) return;
            foreach (var timeline in timelines) Execute(timeline);
        }

        /// <summary>
        /// 执行时间轴
        /// </summary>
        /// <param name="timeline">时间轴信息</param>
        private void Execute(TimelineInfo timeline)
        {

        }
    }
}