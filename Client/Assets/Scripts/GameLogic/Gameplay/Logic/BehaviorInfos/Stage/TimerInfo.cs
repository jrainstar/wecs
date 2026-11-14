using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 定时任务
    /// 纯数据结构 (无引用类型), 参与快照; 到点后做什么由 payload 配置
    /// 或 TimerEvent 的监听方决定, 不持有委托, 所以可以被克隆与回放
    /// </summary>
    public struct TimerTask
    {
        /// <summary>
        /// 任务 ID
        /// </summary>
        public uint id;
        /// <summary>
        /// 触发间隔
        /// </summary>
        public FP interval;
        /// <summary>
        /// 已经流逝的时间
        /// </summary>
        public FP elapsed;
        /// <summary>
        /// 剩余次数, 小于 0 表示无限循环
        /// </summary>
        public int loop;
        /// <summary>
        /// 负载配置 ID, 0 表示不派发负载
        /// </summary>
        public int payload;
        /// <summary>
        /// 施法者
        /// </summary>
        public ulong caster;
        /// <summary>
        /// 目标, 0 表示没有目标
        /// </summary>
        public ulong target;
        /// <summary>
        /// 是否激活, false 表示已停止, 帧末回收
        /// </summary>
        public bool active;
    }

    /// <summary>
    /// 定时器信息 (Stage 级)
    /// 挂载在 Stage.sa 上, 随 StageInfo 一起被快照克隆, 回滚后定时器进度不丢失
    /// </summary>
    public class TimerInfo : BehaviorInfo
    {
        /// <summary>
        /// 自增的任务 ID
        /// 随快照一起恢复, 保证回滚后新任务的 ID 与回滚前一致
        /// </summary>
        public uint increment { get; set; }

        /// <summary>
        /// 定时任务列表, 按创建顺序排列
        /// </summary>
        public List<TimerTask> tasks { get; set; }

        protected override void OnReady()
        {
            increment = 0;
            tasks = ObjectCache.Ensure<List<TimerTask>>();
        }

        protected override void OnReset()
        {
            increment = 0;
            if (null != tasks)
            {
                tasks.Clear();
                ObjectCache.Set(tasks);
                tasks = null;
            }
        }

        protected override BehaviorInfo OnClone()
        {
            var clone = ObjectCache.Ensure<TimerInfo>();
            clone.Ready(actor);
            clone.increment = increment;
            // TimerTask 是结构体, AddRange 即深拷贝
            clone.tasks.AddRange(tasks);

            return clone;
        }
    }
}
