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
        public ulong Seat { get; set; }
        /// <summary>
        /// 英雄 ID
        /// </summary>
        public int Hero { get; set; }
    }
}