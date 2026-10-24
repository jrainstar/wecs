namespace GameLogic.Gameplay.Logic
{
    public struct BuildData
    {
        /// <summary>
        /// GameID
        /// </summary>
        public ulong id { get; set; }
        /// <summary>
        /// 我的座位
        /// </summary>
        public ulong seat { get; set; }
        /// <summary>
        /// Stage 数据
        /// </summary>
        public StageData sdata { get; set; }
    }
}