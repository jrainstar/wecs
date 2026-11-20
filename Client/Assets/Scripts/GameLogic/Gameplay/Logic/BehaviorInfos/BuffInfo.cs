using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 单个 BUFF 的数据
    /// 同一单位可以同时挂多个 BUFF, 同一个 BUFF 还能叠层, 所以层用列表保存:
    /// 刷新/叠加策略共享一份剩余时长, 独立策略每层各自计时
    /// </summary>
    public class BuffSlot
    {
        /// <summary>
        /// BUFF 配置 ID
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// 施加者, 触发负载时作为 caster 传入
        /// </summary>
        public ulong caster { get; set; }
        /// <summary>
        /// 配置的存在时长, 小于等于 0 表示永久
        /// </summary>
        public FP duration { get; set; }
        /// <summary>
        /// 配置了哪些触发时机 (BuffTrigger 位掩码)
        /// 没有配周期触发的 BUFF 不做周期累加
        /// </summary>
        public ushort triggers { get; set; }
        /// <summary>
        /// 周期触发累计时间
        /// </summary>
        public FP tickelapsed { get; set; }
        /// <summary>
        /// 每层的剩余时长, 永久 BUFF 同样占位, 只是不倒计时
        /// </summary>
        public List<FP> layers { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Ready()
        {
            id = 0;
            caster = 0;
            duration = FP.Zero;
            triggers = 0;
            tickelapsed = FP.Zero;

            if (null == layers) layers = ObjectCache.Ensure<List<FP>>();
            layers.Clear();
        }

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
            id = 0;
            caster = 0;
            duration = FP.Zero;
            triggers = 0;
            tickelapsed = FP.Zero;

            if (null == layers) return;

            layers.Clear();
            ObjectCache.Set(layers);
            layers = null;
        }

        /// <summary>
        /// 拷贝
        /// </summary>
        /// <param name="slot">来源</param>
        public void Copy(BuffSlot slot)
        {
            id = slot.id;
            caster = slot.caster;
            duration = slot.duration;
            triggers = slot.triggers;
            tickelapsed = slot.tickelapsed;

            if (null == layers) layers = ObjectCache.Ensure<List<FP>>();
            layers.Clear();
            if (null != slot.layers) layers.AddRange(slot.layers);
        }
    }

    /// <summary>
    /// BUFF 信息
    /// </summary>
    public class BuffInfo : BehaviorInfo
    {
        /// <summary>
        /// BUFF 集合, 按施加顺序排列
        /// </summary>
        public List<BuffSlot> slots { get; set; }

        protected override void OnReady()
        {
            if (null == slots) slots = ObjectCache.Ensure<List<BuffSlot>>();
            slots.Clear();
        }

        protected override void OnReset()
        {
            if (null == slots) return;

            foreach (var slot in slots)
            {
                slot.Reset();
                ObjectCache.Set(slot);
            }
            slots.Clear();
            ObjectCache.Set(slots);
            slots = null;
        }

        protected override BehaviorInfo OnClone()
        {
            var clone = ObjectCache.Ensure<BuffInfo>();
            clone.Ready(actor);

            if (null != slots)
            {
                foreach (var slot in slots)
                {
                    var copy = ObjectCache.Ensure<BuffSlot>();
                    copy.Ready();
                    copy.Copy(slot);
                    clone.slots.Add(copy);
                }
            }

            return clone;
        }
    }
}
