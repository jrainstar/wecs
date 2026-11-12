namespace GameLogic.Gameplay.Common
{
    /// <summary>
    /// 属性定义
    /// </summary>
    public class ATTRIBUTE_DEFINE
    {
        /// <summary>
        /// 当前生命值
        /// </summary>
        public const ushort HP = 1;
        /// <summary>
        /// 最大生命值
        /// </summary>
        public const ushort MAXHP = 2;
        /// <summary>
        /// 移动速度
        /// </summary>
        public const ushort MOVESPEED = 3;
        /// <summary>
        /// 攻击力
        /// </summary>
        public const ushort ATTACK = 4;
        /// <summary>
        /// 防御 (评分, 减伤 = 防御 / (防御 + DAMAGE_DEFINE.DEFENSE_BASE))
        /// </summary>
        public const ushort DEFENSE = 5;
        /// <summary>
        /// 暴击率 (百分点, 25 表示 25%)
        /// </summary>
        public const ushort CRIT = 6;
        /// <summary>
        /// 幸运 (预留给掉落/随机, 暂不参与战斗计算)
        /// </summary>
        public const ushort LUCK = 7;
    }
}