using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 座位信息
    /// </summary>
    public class SeatInfo : BehaviorInfo
    {
        /// <summary>
        /// 座位字典, 键为座位 ID, 值为 EntityID
        /// </summary>
        public Dictionary<ulong, ulong> seatdict { get; set; }
        /// <summary>
        /// 座位字典, 键为 EntityID, 值为座位 ID
        /// </summary>
        public Dictionary<ulong, ulong> actordict { get; set; }

        protected override void OnReady()
        {
            seatdict = ObjectCache.Ensure<Dictionary<ulong, ulong>>();
            actordict = ObjectCache.Ensure<Dictionary<ulong, ulong>>();
        }

        protected override void OnReset()
        {
            seatdict.Clear();
            ObjectCache.Set(seatdict);

            actordict.Clear();
            ObjectCache.Set(actordict);
        }

        protected override BehaviorInfo OnClone()
        {
            var clone = ObjectCache.Ensure<SeatInfo>();
            clone.Ready(actor);
            foreach (var kv in seatdict) clone.seatdict.Add(kv.Key, kv.Value);
            foreach (var kv in actordict) clone.actordict.Add(kv.Key, kv.Value);

            return clone;
        }
    }
}
