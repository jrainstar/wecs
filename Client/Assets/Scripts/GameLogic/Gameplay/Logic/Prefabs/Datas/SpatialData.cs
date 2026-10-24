using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 空间数据结构
    /// </summary>
    public struct SpatialData
    {
        /// <summary>
        /// 位置
        /// </summary>
        public FPVector3 position { get; set; }
        /// <summary>
        /// 旋转
        /// </summary>
        public FPVector3 euler { get; set; }
        /// <summary>
        /// 缩放
        /// </summary>
        public FP scale { get; set; }
    }
}
