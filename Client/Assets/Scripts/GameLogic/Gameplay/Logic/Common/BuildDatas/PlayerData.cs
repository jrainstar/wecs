using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 玩家数据
    /// </summary>
    public struct PlayerData
    {
        /// <summary>
        /// 座位 ID
        /// </summary>
        public ulong seat { get; set; }
        /// <summary>
        /// 英雄 ID
        /// </summary>
        public int hero { get; set; }

        /// <summary>
        /// 位置
        /// </summary>
        public IntVector3 position { get; set; }
        /// <summary>
        /// 旋转
        /// </summary>
        public IntVector3 euler { get; set; }
        /// <summary>
        /// 缩放
        /// </summary>
        public int scale { get; set; }
    }
}