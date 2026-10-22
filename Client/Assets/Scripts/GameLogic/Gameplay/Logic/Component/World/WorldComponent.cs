using GameLogic.Gameplay.Shared;
using System;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 场景状态
    /// </summary>
    public enum StageState
    {
        /// <summary>
        /// 无
        /// </summary>
        None,
        /// <summary>
        /// 初始化
        /// </summary>
        Initialized,
        /// <summary>
        /// 销毁了
        /// </summary>
        Disposed,
        /// <summary>
        /// 暂停中
        /// </summary>
        Paused,
        /// <summary>
        /// 驱动中
        /// </summary>
        Ticking,
        /// <summary>
        /// 停止了
        /// </summary>
        Stopped,
    }

    /// <summary>
    /// 场景信息
    /// </summary>
    public class WorldComponent : Component
    {
        /// <summary>
        /// 当前 Stage 状态
        /// </summary>
        public StageState State { get; set; }
        /// <summary>
        /// 帧号
        /// </summary>
        public uint Frame { get; set; }
        /// <summary>
        /// 流逝时间
        /// </summary>
        public FP Elapsed { get; set; }
        /// <summary>
        /// 时间缩放
        /// </summary>
        public FP TimeScale { get; set; }
        /// <summary>
        /// Entity 自增 ID
        /// </summary>
        public ulong Increment { get; set; }
        /// <summary>
        /// Entity 列表
        /// </summary>
        public List<ulong> Entities { get; set; }
        /// <summary>
        /// 行为类型列表, 键为 EntityID, 值为该 Entity 上的所有行为类型
        /// </summary>
        public Dictionary<ulong, List<Type>> SystemTypes { get; set; }
        /// <summary>
        /// 行为信息列表, 键为行为类型, 值为该行为类型的所有 组件 列表
        /// </summary>
        public Dictionary<Type, List<Component>> ComponentDict { get; set; }

        protected override void OnReady()
        {
            State = StageState.None;
            Frame = 0;
            Elapsed = 0;
            TimeScale = FP.One;
            Increment = 0;

            Entities = ObjectCache.Ensure<List<ulong>>();
            SystemTypes = ObjectCache.Ensure<Dictionary<ulong, List<Type>>>();
            ComponentDict = ObjectCache.Ensure<Dictionary<Type, List<Component>>>();
        }

        protected override void OnReset()
        {
            State = StageState.None;
            Frame = 0;
            Elapsed = 0;
            TimeScale = FP.One;
            Increment = 0;

            Entities.Clear();
            ObjectCache.Set(Entities);

            SystemTypes.Clear();
            ObjectCache.Set(SystemTypes);

            ComponentDict.Clear();
            ObjectCache.Set(ComponentDict);
        }

        protected override Component OnClone()
        {
            var clone = ObjectCache.Ensure<WorldComponent>();
            clone.Ready(Entity);
            clone.State = State;
            clone.Frame = Frame;
            clone.Elapsed = Elapsed;
            clone.TimeScale = TimeScale;
            clone.Increment = Increment;

            clone.Entities.AddRange(Entities);

            foreach (var kv in SystemTypes)
            {
                var list = ObjectCache.Ensure<List<Type>>();
                foreach (var type in kv.Value)
                {
                    list.Add(type);
                }
                clone.SystemTypes.Add(kv.Key, list);
            }

            foreach (var kv in ComponentDict)
            {
                var list = ObjectCache.Ensure<List<Component>>();
                foreach (var info in kv.Value)
                {
                    list.Add(info.Clone());
                }
                clone.ComponentDict.Add(kv.Key, list);
            }

            return clone;
        }
    }
}