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
        public FPVector3 Position { get; set; }
        /// <summary>
        /// 旋转
        /// </summary>
        public FPVector3 Euler { get; set; }
        /// <summary>
        /// 缩放
        /// </summary>
        public FP Scale { get; set; }
    }
}
