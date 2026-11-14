using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 定时器事件
    /// 定时任务到点时派发, 需要写逻辑的方监听这个事件即可 (OnAssemble 里 Listen, OnDisassemble 里 UnListen)
    /// 因为不带任何委托, 定时器状态可以被快照/回滚/回放
    /// </summary>
    public struct TimerEvent : IEvent
    {
        /// <summary>
        /// 任务 ID
        /// </summary>
        public uint id { get; set; }
        /// <summary>
        /// 负载配置 ID, 0 表示没有负载
        /// </summary>
        public int payload { get; set; }
        /// <summary>
        /// 施法者
        /// </summary>
        public ulong caster { get; set; }
        /// <summary>
        /// 目标, 0 表示没有目标
        /// </summary>
        public ulong target { get; set; }
    }

    /// <summary>
    /// 定时器行为 (Stage 级)
    /// 由逻辑帧驱动, 使用 FP 计时, 跟随 Stage 的时间缩放; Stage 暂停/停止时不推进
    /// 与 TickerKit.Timing 的区别: 后者跑在渲染帧上, 与逻辑帧无关, 不参与回滚
    ///
    /// 快照/回放:
    ///   定时任务以纯数据 (TimerInfo.tasks) 的形式挂在 Stage 上, 随 StageInfo 一起 Clone,
    ///   所以回滚后间隔、已流逝时间、剩余次数、任务 ID 都能完整恢复
    ///   因此这里不接受 Action 回调 (委托无法克隆, 且回滚后捕获的行为实例已被重建),
    ///   到点后的效果只有两条出口:
    ///     1. payload > 0 时派发配置负载 (stage.payload.Play), 走 Executor 体系
    ///     2. 派发 TimerEvent, 由订阅方写逻辑
    /// </summary>
    public class Timer : Behavior<TimerInfo>
    {
        /// <summary>
        /// 本帧到点的任务 ID 缓冲
        /// </summary>
        private List<uint> firings { get; set; }

        protected override void OnAssemble()
        {
            base.OnAssemble();
            firings = ObjectCache.Ensure<List<uint>>();
        }

        protected override void OnDisassemble()
        {
            base.OnDisassemble();
            // 任务数据挂在 TimerInfo 上, 由 BehaviorInfo 自己回收, 这里只回收临时缓冲
            if (null != firings)
            {
                firings.Clear();
                ObjectCache.Set(firings);
                firings = null;
            }
        }

        protected override void OnTick(FP tick)
        {
            base.OnTick(tick);

            var tasks = info.tasks;
            if (tick > FP.Zero && tasks.Count > 0)
            {
                firings.Clear();

                // 只推进本帧开始前已存在的任务, 本帧新增的下一帧才开始计时
                var count = tasks.Count;
                for (int i = 0; i < count; i++)
                {
                    var task = tasks[i];
                    if (false == task.active) continue;

                    task.elapsed += tick;
                    if (task.elapsed < task.interval)
                    {
                        tasks[i] = task;
                        continue;
                    }

                    // 一帧最多触发一次, 多出来的时间留在 elapsed 里, 长时间运行不漂移
                    task.elapsed -= task.interval;
                    if (task.loop > 0) task.loop--;
                    tasks[i] = task;

                    firings.Add(task.id);
                }

                for (int i = 0; i < firings.Count; i++)
                {
                    var index = IndexOf(firings[i]);
                    if (index < 0) continue;

                    var task = tasks[index];
                    if (false == task.active) continue;

                    // 派发期间回调可能 Stop 掉别的任务, 这里只关心自己
                    Fire(task);

                    // 回调里可能 Stop 了自己, 重新定位
                    index = IndexOf(task.id);
                    if (index < 0) continue;

                    // 次数耗尽, 帧末回收
                    if (0 == tasks[index].loop)
                    {
                        var done = tasks[index];
                        done.active = false;
                        tasks[index] = done;
                    }
                }
            }

            Recycle(tasks);
        }

        /// <summary>
        /// 延迟一段时间后执行一次
        /// </summary>
        /// <param name="delay">延迟时间</param>
        /// <param name="payload">负载配置 ID, 0 表示只派发 TimerEvent</param>
        /// <param name="caster">施法者</param>
        /// <param name="target">目标, 0 表示没有目标</param>
        /// <returns>任务 ID</returns>
        public uint Delay(FP delay, int payload = 0, ulong caster = 0, ulong target = 0)
        {
            return Add(delay, 1, payload, caster, target);
        }

        /// <summary>
        /// 间隔一段时间循环执行
        /// </summary>
        /// <param name="interval">触发间隔</param>
        /// <param name="payload">负载配置 ID, 0 表示只派发 TimerEvent</param>
        /// <param name="caster">施法者</param>
        /// <param name="target">目标, 0 表示没有目标</param>
        /// <param name="loop">循环次数, 小于 0 表示无限循环</param>
        /// <returns>任务 ID</returns>
        public uint Repeat(FP interval, int payload = 0, ulong caster = 0, ulong target = 0, int loop = -1)
        {
            return Add(interval, loop, payload, caster, target);
        }

        /// <summary>
        /// 添加一个定时任务
        /// </summary>
        /// <param name="interval">触发间隔, 小于等于 0 时每帧触发</param>
        /// <param name="loop">循环次数, 小于 0 表示无限循环</param>
        /// <param name="payload">负载配置 ID, 0 表示只派发 TimerEvent</param>
        /// <param name="caster">施法者</param>
        /// <param name="target">目标, 0 表示没有目标</param>
        /// <returns>任务 ID</returns>
        public uint Add(FP interval, int loop, int payload = 0, ulong caster = 0, ulong target = 0)
        {
            var timerinfo = info;
            if (null == timerinfo) return 0;

            timerinfo.increment++;

            var task = new TimerTask
            {
                id = timerinfo.increment,
                interval = interval > FP.Zero ? interval : FP.Zero,
                elapsed = FP.Zero,
                loop = loop,
                payload = payload,
                caster = caster,
                target = target,
                active = true,
            };

            timerinfo.tasks.Add(task);

            return task.id;
        }

        /// <summary>
        /// 停止定时器
        /// </summary>
        /// <param name="id">任务 ID</param>
        /// <returns>YES/NO</returns>
        public bool Stop(uint id)
        {
            var timerinfo = info;
            if (null == timerinfo) return false;

            var index = IndexOf(id);
            if (index < 0) return false;
            if (false == timerinfo.tasks[index].active) return false;

            var task = timerinfo.tasks[index];
            task.active = false;
            timerinfo.tasks[index] = task;

            return true;
        }

        /// <summary>
        /// 停止全部定时器
        /// </summary>
        public void StopAll()
        {
            var timerinfo = info;
            if (null == timerinfo) return;

            var tasks = timerinfo.tasks;
            for (int i = 0; i < tasks.Count; i++)
            {
                if (false == tasks[i].active) continue;

                var task = tasks[i];
                task.active = false;
                tasks[i] = task;
            }
        }

        /// <summary>
        /// 定时器是否仍在运行
        /// </summary>
        /// <param name="id">任务 ID</param>
        /// <returns>YES/NO</returns>
        public bool Running(uint id)
        {
            var index = IndexOf(id);

            return index >= 0 && info.tasks[index].active;
        }

        /// <summary>
        /// 运行中的定时器数量
        /// </summary>
        public int count
        {
            get
            {
                var timerinfo = info;
                if (null == timerinfo) return 0;

                int result = 0;
                foreach (var task in timerinfo.tasks) if (task.active) result++;

                return result;
            }
        }

        /// <summary>
        /// 派发到点的任务
        /// </summary>
        /// <param name="task">定时任务</param>
        private void Fire(TimerTask task)
        {
            // 逻辑出口 1: 配置负载, 走 Executor 体系
            if (task.payload > 0)
            {
                var payload = stage.payload;
                if (null != payload) payload.Play(task.payload, task.caster, task.target);
            }

            // 逻辑出口 2: 事件, 由订阅方写逻辑
            var eventor = stage.eventor;
            if (null != eventor)
            {
                eventor.Tell(new TimerEvent
                {
                    id = task.id,
                    payload = task.payload,
                    caster = task.caster,
                    target = task.target,
                });
            }
        }

        private int IndexOf(uint id)
        {
            if (0 == id) return -1;

            var timerinfo = info;
            if (null == timerinfo) return -1;

            var tasks = timerinfo.tasks;
            for (int i = 0; i < tasks.Count; i++) if (tasks[i].id == id) return i;

            return -1;
        }

        /// <summary>
        /// 回收已停止的任务
        /// </summary>
        /// <param name="tasks">任务列表</param>
        private void Recycle(List<TimerTask> tasks)
        {
            for (int i = tasks.Count - 1; i >= 0; i--)
            {
                if (tasks[i].active) continue;
                tasks.RemoveAt(i);
            }
        }
    }
}
