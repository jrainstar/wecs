using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// Actors 渲染指令
    /// </summary>
    public class RIL_ACTOR : IRIL
    {
        public override ushort id => RIL_DEFINE.ACTOR;
        
        /// <summary>
        /// Actor 列表
        /// </summary>
        public List<ulong> actors { get; set; }
        
        protected override void OnReady()
        {
            actors = RILCache.Ensure<List<ulong>>();
        }

        protected override void OnReset()
        {
            actors.Clear();
            RILCache.Set(actors);
        }
    }
}