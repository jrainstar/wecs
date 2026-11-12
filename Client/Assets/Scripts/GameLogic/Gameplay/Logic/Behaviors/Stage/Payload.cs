using GameConf.Gameplay;
using GameLogic.Gameplay.Common;
using System;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 配置负载派发
    /// PayloadData 是一组 GameEvent, 派发给对应的 Executor 执行;
    /// 与 Timeline 的区别: 时间轴是"随时间推进"的流程, 这里是"某次命中/触发"产生的一次性效果,
    /// 由触发方 (AOE 重叠、子弹命中) 主动调用, 并带上明确的施法者与目标
    /// </summary>
    public class Payload : Behavior
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

        /// <summary>
        /// 按配置 ID 派发
        /// </summary>
        /// <param name="id">负载配置 ID</param>
        /// <param name="caster">施法者</param>
        /// <param name="target">目标, 0 表示没有目标</param>
        public void Play(int id, ulong caster, ulong target)
        {
            if (0 >= id) return;
            if (null == stage.cfg) return;
            if (false == stage.cfg.location.TbPayloadData.TryGetValue(id, out var data)) return;

            Play(data, caster, target);
        }

        /// <summary>
        /// 直接派发一份负载
        /// </summary>
        /// <param name="data">负载配置</param>
        /// <param name="caster">施法者</param>
        /// <param name="target">目标, 0 表示没有目标</param>
        public void Play(PayloadData data, ulong caster, ulong target)
        {
            if (null == data) return;
            if (null == data.Events) return;
            if (null == executordict) return;

            foreach (var e in data.Events)
            {
                if (null == e) continue;
                // 未注册的事件类型静默忽略
                if (false == executordict.TryGetValue(e.GetType(), out var executor)) continue;

                executor.HandleEvent(e, caster, target);
            }
        }

        private void EnsureExecutors()
        {
            executordict = ObjectCache.Ensure<Dictionary<Type, Executor>>();

            void Executor<E, T>() where E : GameEvent where T : Executor<E>, new()
            {
                var executor = ObjectCache.Ensure<T>().Load(stage);
                executordict.Add(typeof(E), executor);
            }

            Executor<DamageEvent, DamageExecutor>();
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
