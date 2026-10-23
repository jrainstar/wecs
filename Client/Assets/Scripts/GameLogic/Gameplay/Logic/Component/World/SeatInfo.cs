using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 座位信息
    /// </summary>
    public class SeatInfo : Component
    {
        /// <summary>
        /// 座位字典, 键为座位 ID, 值为 EntityID
        /// </summary>
        public Dictionary<ulong, ulong> SeatDict { get; set; }
        /// <summary>
        /// 座位字典, 键为 EntityID, 值为座位 ID
        /// </summary>
        public Dictionary<ulong, ulong> EntityDict { get; set; }

        protected override void OnReady()
        {
            SeatDict = ObjectCache.Ensure<Dictionary<ulong, ulong>>();
            EntityDict = ObjectCache.Ensure<Dictionary<ulong, ulong>>();
        }

        protected override void OnReset()
        {
            SeatDict.Clear();
            ObjectCache.Set(SeatDict);

            EntityDict.Clear();
            ObjectCache.Set(EntityDict);
        }

        protected override Component OnClone()
        {
            var clone = ObjectCache.Ensure<SeatInfo>();
            clone.Ready(Entity);
            foreach (var kv in SeatDict) clone.SeatDict.Add(kv.Key, kv.Value);
            foreach (var kv in EntityDict) clone.EntityDict.Add(kv.Key, kv.Value);

            return clone;
        }
    }
}
