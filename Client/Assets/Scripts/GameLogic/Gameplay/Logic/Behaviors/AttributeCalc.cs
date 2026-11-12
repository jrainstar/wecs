using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 数值计算行为
    /// 单位的所有数值统一在这里算, 其它行为只通过 Get 取最终值, 不直接读基础值;
    /// 计算顺序: 最终覆盖 > (基础值 + 加法修正) * (1 + 百分比修正)
    /// 以后接 BUFF/装备/成长时只需要往这里加修正, 调用方不用改
    /// 查询时才按 dirty 标记重算, 没有逐帧工作, 因此不登记到 TICK_DEFINE
    /// </summary>
    public class AttributeCalc : Behavior<AttributeCalcInfo>
    {
        /// <summary>
        /// 读取最终值
        /// </summary>
        /// <param name="key">属性 KEY, 见 ATTRIBUTE_DEFINE</param>
        /// <returns>最终值, 没有该属性返回 0</returns>
        public FP Get(ushort key)
        {
            return Get(key, FP.Zero);
        }

        /// <summary>
        /// 读取最终值
        /// </summary>
        /// <param name="key">属性 KEY, 见 ATTRIBUTE_DEFINE</param>
        /// <param name="defaultvalue">没有该属性时的缺省值</param>
        /// <returns>最终值</returns>
        public FP Get(ushort key, FP defaultvalue)
        {
            var calc = info;
            if (null == calc) return defaultvalue;
            if (calc.dirty) Calc(calc);
            if (null == calc.results) return defaultvalue;
            if (false == calc.results.TryGetValue(key, out var value)) return defaultvalue;

            return value;
        }

        /// <summary>
        /// 读取基础值, 不含任何修正
        /// </summary>
        /// <param name="key">属性 KEY, 见 ATTRIBUTE_DEFINE</param>
        /// <returns>基础值, 没有该属性返回 0</returns>
        public FP GetBase(ushort key)
        {
            var calc = info;
            if (null == calc || null == calc.bases) return FP.Zero;
            if (false == calc.bases.TryGetValue(key, out var value)) return FP.Zero;

            return value;
        }

        /// <summary>
        /// 是否拥有该属性
        /// </summary>
        /// <param name="key">属性 KEY, 见 ATTRIBUTE_DEFINE</param>
        /// <returns>YES/NO</returns>
        public bool Has(ushort key)
        {
            var calc = info;
            if (null == calc) return false;
            if (calc.dirty) Calc(calc);
            if (null == calc.results) return false;

            return calc.results.ContainsKey(key);
        }

        /// <summary>
        /// 设置基础值
        /// </summary>
        /// <param name="key">属性 KEY, 见 ATTRIBUTE_DEFINE</param>
        /// <param name="value">基础值</param>
        public void SetBase(ushort key, FP value)
        {
            var calc = info;
            if (null == calc || null == calc.bases) return;

            calc.bases[key] = value;
            calc.dirty = true;
        }

        /// <summary>
        /// 累加基础值, 用于升级成长这类永久增量
        /// </summary>
        /// <param name="key">属性 KEY, 见 ATTRIBUTE_DEFINE</param>
        /// <param name="value">增量, 负值为减</param>
        public void AddBase(ushort key, FP value)
        {
            SetBase(key, GetBase(key) + value);
        }

        /// <summary>
        /// 累加加法修正, 同 KEY 的修正会累加, 不会覆盖
        /// </summary>
        /// <param name="key">属性 KEY, 见 ATTRIBUTE_DEFINE</param>
        /// <param name="value">修正值, 负值为减益</param>
        public void Add(ushort key, FP value)
        {
            var calc = info;
            if (null == calc || null == calc.adds) return;

            calc.adds.TryGetValue(key, out var exist);
            calc.adds[key] = exist + value;
            calc.dirty = true;
        }

        /// <summary>
        /// 累加百分比修正, 同 KEY 的修正会累加, 不会覆盖
        /// </summary>
        /// <param name="key">属性 KEY, 见 ATTRIBUTE_DEFINE</param>
        /// <param name="value">百分比, 0.1 表示 +10%, 负值为减益</param>
        public void AddPercent(ushort key, FP value)
        {
            var calc = info;
            if (null == calc || null == calc.percents) return;

            calc.percents.TryGetValue(key, out var exist);
            calc.percents[key] = exist + value;
            calc.dirty = true;
        }

        /// <summary>
        /// 设置最终覆盖
        /// 有覆盖时基础值与修正全部失效, Get 直接返回覆盖值;
        /// 用于变身/剧情/调试这类"强制指定"的场合, 普通加成请用 Add/AddPercent
        /// </summary>
        /// <param name="key">属性 KEY, 见 ATTRIBUTE_DEFINE</param>
        /// <param name="value">覆盖值</param>
        public void SetOverride(ushort key, FP value)
        {
            var calc = info;
            if (null == calc || null == calc.overrides) return;

            calc.overrides[key] = value;
            calc.dirty = true;
        }

        /// <summary>
        /// 是否设置了最终覆盖
        /// </summary>
        /// <param name="key">属性 KEY, 见 ATTRIBUTE_DEFINE</param>
        /// <returns>YES/NO</returns>
        public bool HasOverride(ushort key)
        {
            var calc = info;
            if (null == calc || null == calc.overrides) return false;

            return calc.overrides.ContainsKey(key);
        }

        /// <summary>
        /// 清除某个属性的最终覆盖, 清除后重新按基础值与修正计算
        /// </summary>
        /// <param name="key">属性 KEY, 见 ATTRIBUTE_DEFINE</param>
        public void ClearOverride(ushort key)
        {
            var calc = info;
            if (null == calc || null == calc.overrides) return;
            if (false == calc.overrides.Remove(key)) return;

            calc.dirty = true;
        }

        /// <summary>
        /// 清除全部最终覆盖
        /// </summary>
        public void ClearOverride()
        {
            var calc = info;
            if (null == calc || null == calc.overrides) return;
            if (0 == calc.overrides.Count) return;

            calc.overrides.Clear();
            calc.dirty = true;
        }

        /// <summary>
        /// 清除某个属性的全部修正, 不包含最终覆盖 (覆盖需用 ClearOverride)
        /// </summary>
        /// <param name="key">属性 KEY, 见 ATTRIBUTE_DEFINE</param>
        public void ClearModifier(ushort key)
        {
            var calc = info;
            if (null == calc) return;

            if (null != calc.adds) calc.adds.Remove(key);
            if (null != calc.percents) calc.percents.Remove(key);
            calc.dirty = true;
        }

        /// <summary>
        /// 清除全部修正, 不包含最终覆盖 (覆盖需用 ClearOverride)
        /// </summary>
        public void ClearModifier()
        {
            var calc = info;
            if (null == calc) return;

            if (null != calc.adds) calc.adds.Clear();
            if (null != calc.percents) calc.percents.Clear();
            calc.dirty = true;
        }

        /// <summary>
        /// 立即重算, 一般不需要外部调用, Get 时会自动按 dirty 标记重算
        /// </summary>
        public void ReCalc()
        {
            var calc = info;
            if (null == calc) return;

            Calc(calc);
        }

        /// <summary>
        /// 重算全部属性
        /// </summary>
        /// <param name="calc">数值信息</param>
        private static void Calc(AttributeCalcInfo calc)
        {
            if (null == calc || null == calc.results) return;

            calc.results.Clear();

            // 最终覆盖优先, 写进去之后该属性不再参与基础值与修正的运算
            if (null != calc.overrides)
            {
                foreach (var kv in calc.overrides)
                {
                    calc.results[kv.Key] = kv.Value;
                }
            }

            // 基础值 + 加法修正, 再乘百分比
            if (null != calc.bases)
            {
                foreach (var kv in calc.bases)
                {
                    if (calc.results.ContainsKey(kv.Key)) continue;

                    calc.results.Add(kv.Key, Result(calc, kv.Key, kv.Value));
                }
            }

            // 只有修正没有基础值的属性 (纯 BUFF 加成) 同样要能读到
            if (null != calc.adds)
            {
                foreach (var kv in calc.adds)
                {
                    if (calc.results.ContainsKey(kv.Key)) continue;

                    calc.results.Add(kv.Key, Result(calc, kv.Key, FP.Zero));
                }
            }

            calc.dirty = false;
        }

        /// <summary>
        /// 计算单个属性的最终值
        /// </summary>
        /// <param name="calc">数值信息</param>
        /// <param name="key">属性 KEY</param>
        /// <param name="basevalue">基础值</param>
        /// <returns>最终值</returns>
        private static FP Result(AttributeCalcInfo calc, ushort key, FP basevalue)
        {
            var value = basevalue;

            if (null != calc.adds && calc.adds.TryGetValue(key, out var add)) value += add;
            if (null != calc.percents && calc.percents.TryGetValue(key, out var percent)) value *= FP.One + percent;

            return value;
        }
    }
}
