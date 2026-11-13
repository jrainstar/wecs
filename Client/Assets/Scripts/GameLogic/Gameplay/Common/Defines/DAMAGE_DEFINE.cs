namespace GameLogic.Gameplay.Common
{
    /// <summary>
    /// 伤害定义
    /// 伤害公式里的可调参数集中在这里, 调整手感不用改代码
    /// </summary>
    public class DAMAGE_DEFINE
    {
        /// <summary>
        /// 防御曲线的基数
        /// 减伤比例 = 防御 / (防御 + 基数), 防御等于基数时减伤 50%,
        /// 防御再高也只是趋近 100%, 不会出现完全免伤
        /// </summary>
        public const float DEFENSE_BASE = 100f;
        /// <summary>
        /// 暴击伤害倍率
        /// </summary>
        public const float CRIT_DAMAGE = 2f;
        /// <summary>
        /// 单次命中的最低伤害, 防止高防御把伤害完全吃掉
        /// </summary>
        public const int MIN_DAMAGE = 1;
        /// <summary>
        /// 表格里百分比 (0~100) 换算成系数时的分母
        /// </summary>
        public const float RATE = 100f;
        /// <summary>
        /// 受击僵直时长 (秒)
        /// 伤害结算后目标进入受击状态, 僵直期间不能移动/释放技能,
        /// 期间再次受击会刷新该时长
        /// </summary>
        public const float HIT_STUN = 1f;
    }
}
