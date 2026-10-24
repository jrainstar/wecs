using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 标签信息, Entity 上的标签信息, 用于标记 Entity 各种颗粒度细的信息
    /// </summary>
    public class TagInfo : BehaviorInfo
    {
        /// <summary>
        /// 标签的数据集合, 键为 TAG_DEFINE, 值为 Int64
        /// </summary>
        public Dictionary<ushort, long> tags { get; set; }

        protected override void OnReady()
        {
            tags = ObjectCache.Ensure<Dictionary<ushort, long>>();
        }

        protected override void OnReset()
        {
            tags.Clear();
            ObjectCache.Set(tags);
        }

        protected override BehaviorInfo OnClone()
        {
            var clone = ObjectCache.Ensure<TagInfo>();
            clone.Ready(actor);
            foreach (var kv in tags) clone.tags.Add(kv.Key, kv.Value);

            return clone;
        }
    }
}
