using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 属性翻译器
    /// 只把渲染需要的数值 (血量/移速/攻击) 上行, 其它属性留在逻辑层;
    /// AttributeCalcInfo 里存的是未重算的脏数据, 取值必须走 AttributeCalc.Get
    /// </summary>
    public class AttributeTranslator : Translator<AttributeCalcInfo, RIL_ATTRIBUTE>
    {
        public override ushort id => RIL_DEFINE.ATTRIBUTE;

        protected override int OnCalcHashCode(AttributeCalcInfo info)
        {
            var calc = stage.GetBehavior<AttributeCalc>(info.actor);
            if (null == calc) return 0;

            int hash = 17;
            hash = hash * 31 + info.actor.GetHashCode();
            hash = hash * 31 + calc.Get(ATTRIBUTE_DEFINE.HP).GetHashCode();
            hash = hash * 31 + calc.Get(ATTRIBUTE_DEFINE.MAXHP).GetHashCode();
            hash = hash * 31 + calc.Get(ATTRIBUTE_DEFINE.MOVESPEED).GetHashCode();
            hash = hash * 31 + calc.Get(ATTRIBUTE_DEFINE.ATTACK).GetHashCode();

            return hash;
        }

        protected override void OnRIL(AttributeCalcInfo info, RIL_ATTRIBUTE ril)
        {
            var calc = stage.GetBehavior<AttributeCalc>(info.actor);
            if (null == calc) return;

            ril.hp = calc.Get(ATTRIBUTE_DEFINE.HP).AsInt();
            ril.maxhp = calc.Get(ATTRIBUTE_DEFINE.MAXHP).AsInt();
            ril.movespeed = calc.Get(ATTRIBUTE_DEFINE.MOVESPEED).AsInt();
            ril.attack = calc.Get(ATTRIBUTE_DEFINE.ATTACK).AsInt();
        }
    }
}
