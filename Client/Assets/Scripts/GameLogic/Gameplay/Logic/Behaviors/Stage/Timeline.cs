using GameConf.Gameplay;
using GameLogic.Gameplay.Common;
using System;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 时间轴行为
    /// </summary>
    public class Timeline : Behavior
    {
        /// <summary>
        /// 指令执行器字典
        /// </summary>
        private Dictionary<Type, Executor> executordict { get; set; }

        /// <summary>
        /// 当前正在播放的时间轴宿主
        /// 时间轴是 Stage 级单例, 事件派发时通过它得知事件属于哪个 Actor
        /// </summary>
        public TimelineInfo current { get; private set; }

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
            foreach (var timeline in timelines) Execute(timeline, tick);
        }

        /// <summary>
        /// 播放时间轴
        /// 同一个 Actor 同一时间只允许播放一条时间轴, 旧的播完(帧末回收)后才可以播下一条
        /// </summary>
        /// <param name="actor">承载时间轴的 Actor</param>
        /// <param name="id">时间轴配置 ID</param>
        /// <param name="timescale">时间缩放</param>
        /// <param name="caster">释放者</param>
        /// <returns>时间轴信息, Actor 已移除或时间轴仍在播放时返回 null</returns>
        public TimelineInfo Play(ulong actor, int id, FP timescale, ulong caster = 0)
        {
            if (false == stage.cache.Valid(actor)) return null;
            // 不允许打断/替换, 已存在 (含同帧内刚播完尚未回收的) 即失败
            if (null != stage.GetBehaviorInfo<TimelineInfo>(actor)) return null;

            var timelineinfo = stage.AddBehaviorInfo<TimelineInfo>(actor);

            timelineinfo.id = id;
            timelineinfo.caster = caster;
            timelineinfo.timescale = timescale;
            timelineinfo.timeelapsed = FP.Zero;
            timelineinfo.chargeelapsed = FP.Zero;
            timelineinfo.charging = false;

            return timelineinfo;
        }

        /// <summary>
        /// 执行时间轴
        /// </summary>
        /// <param name="timelineinfo">时间轴信息</param>
        /// <param name="tick">步长</param>
        private void Execute(TimelineInfo timelineinfo, FP tick)
        {
            if (false == stage.cfg.location.TbTimelineData.TryGetValue(timelineinfo.id, out var timelinedata)) return;
            // 时间缩放为 0 视为暂停, 不推进时间
            if (timelineinfo.timescale <= FP.Zero) return;
            if (null == timelinedata.Nodes) return;

            var maxtime = (FP)timelinedata.MaxTime;
            var delta = tick * timelineinfo.timescale;
            var begin = timelineinfo.timeelapsed;
            var end = begin + delta;
            // 蓄力计时: 只在蓄力中推进, 只加不减, GoTo 回跳不清零
            if (timelineinfo.charging)
            {
                timelineinfo.chargeelapsed += delta;
            }
            // 本帧为结束帧, 需要把 MaxTime 上的节点一并触发, 因此窗口右端闭合
            var finish = end >= maxtime;
            if (finish) end = maxtime;

            // 触发时间窗口 [Begin, End) 内的节点, 结束帧为 [Begin, End]
            foreach (var node in timelinedata.Nodes)
            {
                if (null == node) continue;
                var time = (FP)node.Time;
                if (time < begin) continue;
                if (finish ? time > end : time >= end) continue;
                // 执行器无状态, 事件作用于哪条时间轴通过 current 传递
                current = timelineinfo;
                HandleEvent(node.Event, timelineinfo.caster, 0);
                current = null;
            }

            // 循环, 本帧跨越 GoTo.From 时回跳到 GoTo.To
            if (null != timelinedata.GoTo
                && begin < (FP)timelinedata.GoTo.From
                && end >= (FP)timelinedata.GoTo.From)
            {
                timelineinfo.timeelapsed = (FP)timelinedata.GoTo.To;
                return;
            }

            // 到达 MaxTime, 时间轴结束
            if (finish)
            {
                // 时间轴只负责切换动画, 结束时不还原动画状态
                // 角色保持节点指定的动画, 由 Movement/StateMachine 等行为在后续帧自然切回待机/移动
                stage.RmvBehaviorInfo(timelineinfo);
                return;
            }

            // 期间被外部 Play 重播过 (timeelapsed 已被重置), 保留新值不覆盖
            if (timelineinfo.timeelapsed == begin)
            {
                timelineinfo.timeelapsed = end;
            }
        }

        /// <summary>
        /// 派发时间轴事件
        /// </summary>
        /// <param name="timelineevent">时间轴事件</param>
        /// <param name="caster">施法者</param>
        /// <param name="target">目标, 0 表示没有目标</param>
        private void HandleEvent(GameEvent timelineevent, ulong caster, ulong target)
        {
            if (null == timelineevent) return;
            if (null == executordict) return;
            if (false == executordict.TryGetValue(timelineevent.GetType(), out var executor)) return;
            executor.HandleEvent(timelineevent, caster, target);
        }

        private void EnsureExecutors()
        {
            executordict = ObjectCache.Ensure<Dictionary<Type, Executor>>();

            void Executor<E, T>() where E : GameEvent where T : Executor<E>, new()
            {
                var executor = ObjectCache.Ensure<T>().Load(stage);
                executordict.Add(typeof(E), executor);
            }

            Executor<PrintEvent, PrintExecutor>();
            Executor<AnimationEvent, AnimationExecutor>();
            Executor<EmitterEvent, EmitterExecutor>();
            Executor<EffectEvent, VisualExecutor>();
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