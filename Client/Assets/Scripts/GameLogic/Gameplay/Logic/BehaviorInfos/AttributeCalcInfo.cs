using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 数值计算信息
    /// 最终值 = 覆盖值, 有覆盖时直接取覆盖值, 不再参与后面的运算
    ///          覆盖值 ?? (基础值 + 加法修正) * (1 + 百分比修正)
    /// 基础值来自配置/成长, 修正来自 BUFF/装备/光环等来源, 覆盖用于"强制指定"的场合 (变身/剧情/调试);
    /// 修改基础值或修正时只置脏, 查询时才重算, 同一帧内多次修改只算一次
    /// </summary>
    public class AttributeCalcInfo : BehaviorInfo
    {
        /// <summary>
        /// 基础值集合 (属性 KEY -> 基础值), 一般来自配置/成长
        /// </summary>
        public Dictionary<ushort, FP> bases { get; set; }
        /// <summary>
        /// 加法修正合计 (属性 KEY -> 固定值加成), 直接平铺叠加在基础值上
        /// </summary>
        public Dictionary<ushort, FP> adds { get; set; }
        /// <summary>
        /// 百分比修正合计 (属性 KEY -> 百分比), 0.1 表示 +10%
        /// </summary>
        public Dictionary<ushort, FP> percents { get; set; }
        /// <summary>
        /// 最终覆盖 (属性 KEY -> 覆盖值), 存在覆盖时基础值与修正全部失效
        /// </summary>
        public Dictionary<ushort, FP> overrides { get; set; }
        /// <summary>
        /// 最终值缓存 (属性 KEY -> 最终值)
        /// </summary>
        public Dictionary<ushort, FP> results { get; set; }
        /// <summary>
        /// 是否有未重算的修改
        /// </summary>
        public bool dirty { get; set; }

        protected override void OnReady()
        {
            bases = ObjectCache.Ensure<Dictionary<ushort, FP>>();
            adds = ObjectCache.Ensure<Dictionary<ushort, FP>>();
            percents = ObjectCache.Ensure<Dictionary<ushort, FP>>();
            overrides = ObjectCache.Ensure<Dictionary<ushort, FP>>();
            results = ObjectCache.Ensure<Dictionary<ushort, FP>>();
            dirty = true;
        }

        protected override void OnReset()
        {
            dirty = true;
            Recycle(bases);
            Recycle(adds);
            Recycle(percents);
            Recycle(overrides);
            Recycle(results);

            bases = null;
            adds = null;
            percents = null;
            overrides = null;
            results = null;
        }

        protected override BehaviorInfo OnClone()
        {
            var clone = ObjectCache.Ensure<AttributeCalcInfo>();
            clone.Ready(actor);

            Copy(bases, clone.bases);
            Copy(adds, clone.adds);
            Copy(percents, clone.percents);
            Copy(overrides, clone.overrides);
            Copy(results, clone.results);
            clone.dirty = dirty;

            return clone;
        }

        /// <summary>
        /// 回收一个集合, 清空后放回对象池
        /// </summary>
        /// <param name="dict">集合</param>
        private static void Recycle(Dictionary<ushort, FP> dict)
        {
            if (null == dict) return;

            dict.Clear();
            ObjectCache.Set(dict);
        }

        /// <summary>
        /// 拷贝集合
        /// </summary>
        /// <param name="from">来源</param>
        /// <param name="to">目标</param>
        private static void Copy(Dictionary<ushort, FP> from, Dictionary<ushort, FP> to)
        {
            if (null == from || null == to) return;

            foreach (var kv in from) to[kv.Key] = kv.Value;
        }
    }
}
