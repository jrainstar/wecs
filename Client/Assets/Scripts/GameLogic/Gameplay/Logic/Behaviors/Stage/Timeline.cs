using GameConf.Gameplay;
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
            var begin = timelineinfo.timeelapsed;
            var end = begin + tick * timelineinfo.timescale;
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
                HandleEvent(node.Event);
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
        private void HandleEvent(TimelineEvent timelineevent)
        {
            if (null == timelineevent) return;
            if (null == executordict) return;
            if (false == executordict.TryGetValue(timelineevent.GetType(), out var executor)) return;
            executor.HandleEvent(timelineevent);
        }

        private void EnsureExecutors()
        {
            executordict = ObjectCache.Ensure<Dictionary<Type, Executor>>();

            void Executor<E, T>() where E : TimelineEvent where T : Executor<E>, new()
            {
                var executor = ObjectCache.Ensure<T>().Load(stage);
                executordict.Add(typeof(E), executor);
            }

            Executor<PrintEvent, PrintExecutor>();
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