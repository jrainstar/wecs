using GameLogic.Gameplay.Shared;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 事件订阅派发者信息
    /// </summary>
    public class EventorInfo : Component
    {
        /// <summary>
        /// 事件订阅派发者的增量计数器, 用于生成唯一的事件索引 (用作排序)
        /// </summary>
        public uint Increment { get; set; }

        /// <summary>
        /// 事件索引字典, 用于存储事件的索引 (用作排序)
        /// </summary>
        public Dictionary<(int, ulong entity), uint> Indexes { get; set; }

        protected override void OnReady()
        {
            Increment = 0;
            Indexes = ObjectCache.Ensure<Dictionary<(int, ulong), uint>>();
        }

        protected override void OnReset()
        {
            Increment = 0;
            Indexes.Clear();
            ObjectCache.Set(Indexes);
        }

        protected override Component OnClone()
        {
            var clone = ObjectCache.Ensure<EventorInfo>();
            clone.Ready(Entity);
            clone.Increment = Increment;
            clone.Indexes = ObjectCache.Ensure<Dictionary<(int type, ulong entity), uint>>();
            foreach (var kv in Indexes) clone.Indexes.Add(kv.Key, kv.Value);

            return clone;
        }
    }
}