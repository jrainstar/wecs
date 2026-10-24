using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 空间渲染指令
    /// </summary>
    public class RIL_SPATIAL : IRIL
    {
        public override ushort id => RIL_DEFINE.SPATIAL;
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

        protected override void OnReady()
        {
            position = FPVector3.Zero;
            euler = FPVector3.Zero;
            scale = FP.Zero;
        }

        protected override void OnReset()
        {
            position = FPVector3.Zero;
            euler = FPVector3.Zero;
            scale = FP.Zero;
        }

        public override string ToString()
        {
            return $"RIL_SPATIAL: position={position}, euler={euler}, scale={scale}";
        }
    }
}
