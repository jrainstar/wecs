namespace GameLogic.Gameplay.Common
{
    /// <summary>
    /// 发射器定义
    /// </summary>
    public class EMITTER_DEFINE
    {
        /// <summary>
        /// 基础飞行速度 (米/秒)
        /// 速度配置 (EmitterSpeedData) 只描述缓动倍率 (进度|倍率), 实际速度 = 基础速度 * 倍率,
        /// 单位与逻辑层保持一致 (米)
        /// </summary>
        public static readonly FP SPEED = 10;
    }
}
