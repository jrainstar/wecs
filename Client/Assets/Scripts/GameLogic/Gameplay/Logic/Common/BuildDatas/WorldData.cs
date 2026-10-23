namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// World 数据
    /// </summary>
    public struct WorldData
    {
        /// <summary>
        /// 游戏的种子, 用于随机数生成等等
        /// </summary>
        public int Seed { get; set; }
        /// <summary>
        /// 玩家数据数组, 包含了所有参与游戏的玩家信息
        /// </summary>
        public PlayerData[] Players { get; set; }
    }

}
