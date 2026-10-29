using GameLogic.Gameplay.Common;
using System;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 时间轴行为
    /// </summary>
    public class Timeline : Behavior<TimelineInfo>
    {
        /// <summary>
        /// 指令执行器字典
        /// </summary>
        private Dictionary<Type, Executor> executordict { get; set; }

        protected override void OnAssemble()
        {
            base.OnAssemble();
            EnsureExecutors();
        }

        protected override void OnDisassemble()
        {
            base.OnDisassemble();
            RecycleExecutors();
        }

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

        private void EnsureExecutors()
        {
            executordict = ObjectCache.Ensure<Dictionary<Type, Executor>>();

            void Executor<T>() where T : Executor, new()
            {
                var executor = ObjectCache.Ensure<T>().Load(stage);
                executordict.Add(typeof(T), executor);
            }

            Executor<PrintExecutor>();
        }

        private void RecycleExecutors()
        {
            if (null == executordict) return;
            foreach (var executor in executordict.Values)
            {
                executor.Unload();
                ObjectCache.Set(executor);
            }
            executordict.Clear();
            ObjectCache.Set(executordict);
            executordict = null;
        }
    }
}