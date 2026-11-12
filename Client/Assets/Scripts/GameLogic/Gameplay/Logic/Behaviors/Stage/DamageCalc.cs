using GameConf.Gameplay;
using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 伤害计算
    /// 公式:
    ///   基础 = Damage + 施法者加成属性 * attack_rate%
    ///   减伤 = 目标防御属性 / (目标防御属性 + DEFENSE_BASE) * defense_rate%
    ///          真实伤害, 或 defense_type 配无时, 减伤为 0
    ///   暴击 = crit% 概率命中, 命中后乘暴击倍率
    ///   最终 = max(基础 * (1 - 减伤) * 暴击倍率, MIN_DAMAGE)
    /// 全过程使用定点数, 随机走 stage.random, 保证确定性与快照回滚一致
    /// </summary>
    public class DamageCalc : Behavior
    {
        /// <summary>
        /// 计算单次命中的伤害, 不含 HitCount 与扣血
        /// 每次调用都会单独判定一次暴击, 多段命中请循环调用
        /// </summary>
        /// <param name="e">伤害配置</param>
        /// <param name="caster">施法者</param>
        /// <param name="target">目标</param>
        /// <returns>本次命中伤害, 配置为空返回 0</returns>
        public FP Calc(DamageEvent e, ulong caster, ulong target)
        {
            if (null == e) return FP.Zero;

            // 基础伤害: 固定值 + 施法者加成属性 * 加成系数
            var value = (FP)e.Damage + Scale(caster, e.AttackType) * ((FP)e.AttackRate / (FP)DAMAGE_DEFINE.RATE);

            // 暴击
            if (e.CanCrit && Crit(caster)) value *= (FP)DAMAGE_DEFINE.CRIT_DAMAGE;

            // 减伤, 真实伤害无视防御
            if (DamageType.True != e.Type)
            {
                value *= FP.One - Mitigation(target, e.DefenseType, (FP)e.DefenseRate / (FP)DAMAGE_DEFINE.RATE);
            }

            return value < (FP)DAMAGE_DEFINE.MIN_DAMAGE ? (FP)DAMAGE_DEFINE.MIN_DAMAGE : value;
        }

        /// <summary>
        /// 结算伤害: 按 HitCount 逐段计算并扣目标当前生命
        /// </summary>
        /// <param name="e">伤害配置</param>
        /// <param name="caster">施法者</param>
        /// <param name="target">目标</param>
        /// <returns>本次结算造成的总伤害, 目标无效或没有数值信息时返回 0</returns>
        public FP Apply(DamageEvent e, ulong caster, ulong target)
        {
            if (null == e) return FP.Zero;
            if (0 == target || false == stage.cache.Valid(target)) return FP.Zero;

            var calc = stage.GetBehavior<AttributeCalc>(target);
            if (null == calc) return FP.Zero;

            // 多段命中, 每段单独判定暴击
            var hits = e.HitCount > 0 ? e.HitCount : 1;
            var total = FP.Zero;
            for (int i = 0; i < hits; i++) total += Calc(e, caster, target);
            if (total <= FP.Zero) return FP.Zero;

            // 当前生命是状态值, 直接写基础值, 不走修正
            var maxhp = calc.Get(ATTRIBUTE_DEFINE.MAXHP);
            var hp = calc.Get(ATTRIBUTE_DEFINE.HP) - total;
            if (hp < FP.Zero) hp = FP.Zero;
            if (maxhp > FP.Zero && hp > maxhp) hp = maxhp;

            calc.SetBase(ATTRIBUTE_DEFINE.HP, hp);

            return total;
        }

        /// <summary>
        /// 读取某个数值来源属性
        /// </summary>
        /// <param name="actor">Actor</param>
        /// <param name="type">来源</param>
        /// <returns>属性值, 单位没有数值信息或来源无效时返回 0</returns>
        public FP Scale(ulong actor, ScalingType type)
        {
            if (ScalingType.None == type) return FP.Zero;
            if (0 == actor || false == stage.cache.Valid(actor)) return FP.Zero;

            var calc = stage.GetBehavior<AttributeCalc>(actor);
            if (null == calc) return FP.Zero;

            switch (type)
            {
                case ScalingType.Atk: return calc.Get(ATTRIBUTE_DEFINE.ATTACK);
                case ScalingType.Def: return calc.Get(ATTRIBUTE_DEFINE.DEFENSE);
                case ScalingType.MaxHp: return calc.Get(ATTRIBUTE_DEFINE.MAXHP);
                case ScalingType.CurHp: return calc.Get(ATTRIBUTE_DEFINE.HP);
                case ScalingType.LostHp: return calc.Get(ATTRIBUTE_DEFINE.MAXHP) - calc.Get(ATTRIBUTE_DEFINE.HP);
                case ScalingType.MoveSpeed: return calc.Get(ATTRIBUTE_DEFINE.MOVESPEED);
                default: return FP.Zero;
            }
        }

        /// <summary>
        /// 计算减伤比例
        /// </summary>
        /// <param name="target">目标</param>
        /// <param name="type">防御来源</param>
        /// <param name="rate">防御系数 (0~1)</param>
        /// <returns>减伤比例 (0~1)</returns>
        private FP Mitigation(ulong target, ScalingType type, FP rate)
        {
            if (ScalingType.None == type) return FP.Zero;
            if (rate <= FP.Zero) return FP.Zero;

            var defense = Scale(target, type);
            if (defense <= FP.Zero) return FP.Zero;

            // 评分曲线: 防御越高收益越低, 防御等于基数时减伤 50%
            var mitigation = defense / (defense + (FP)DAMAGE_DEFINE.DEFENSE_BASE) * rate;
            if (mitigation > FP.One) return FP.One;
            if (mitigation < FP.Zero) return FP.Zero;

            return mitigation;
        }

        /// <summary>
        /// 暴击判定
        /// </summary>
        /// <param name="caster">施法者</param>
        /// <returns>YES/NO</returns>
        private bool Crit(ulong caster)
        {
            var calc = stage.GetBehavior<AttributeCalc>(caster);
            if (null == calc) return false;

            var chance = calc.Get(ATTRIBUTE_DEFINE.CRIT) / (FP)DAMAGE_DEFINE.RATE;
            if (chance <= FP.Zero) return false;
            if (chance >= FP.One) return true;
            if (null == stage.random) return false;

            return stage.random.Range(FP.Zero, FP.One) < chance;
        }
    }
}
