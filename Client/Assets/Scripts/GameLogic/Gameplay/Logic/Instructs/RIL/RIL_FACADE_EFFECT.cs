using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 单个特效的渲染数据
    /// 渲染层的 EffectAgent 按 elapsed 采样 EffectController, 通过 EffectController 自身推进
    /// </summary>
    public class RIL_EFFECT
    {
        /// <summary>
        /// 特效编号, 同一个 Actor 上唯一
        /// </summary>
        public uint id { get; set; }
        /// <summary>
        /// 特效资源编号 (TbResourceData), 0 表示只有音效
        /// </summary>
        public int fx { get; set; }
        /// <summary>
        /// 音效资源编号 (TbResourceData), 0 表示不播
        /// </summary>
        public int sound { get; set; }
        /// <summary>
        /// 挂点骨骼
        /// </summary>
        public string anchor { get; set; }
        /// <summary>
        /// 是否跟随挂点
        /// </summary>
        public bool follow { get; set; }
        /// <summary>
        /// 相对挂点的偏移
        /// </summary>
        public FPVector3 offset { get; set; }
        /// <summary>
        /// 缩放
        /// </summary>
        public FP scale { get; set; }
        /// <summary>
        /// 已流逝时间 (下发时的逻辑时间, 渲染层据此对齐/回滚)
        /// </summary>
        public FP elapsed { get; set; }
        /// <summary>
        /// 存在时长
        /// </summary>
        public FP duration { get; set; }

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
            id = 0;
            fx = 0;
            sound = 0;
            anchor = null;
            follow = false;
            offset = FPVector3.Zero;
            scale = FP.One;
            elapsed = FP.Zero;
            duration = FP.Zero;
        }
    }

    /// <summary>
    /// 外观特效指令
    /// 携带该 Actor 上当前所有存活的特效, 以编号为键做增删对比
    /// </summary>
    public class RIL_FACADE_EFFECT : IRIL
    {
        public override ushort id => RIL_DEFINE.FACADE_EFFECT;

        /// <summary>
        /// 特效集合 (特效编号 -> 特效渲染数据)
        /// </summary>
        public Dictionary<uint, RIL_EFFECT> effectdict { get; set; }

        protected override void OnReady()
        {
            effectdict = RILCache.Ensure<Dictionary<uint, RIL_EFFECT>>();
            effectdict.Clear();
        }

        protected override void OnReset()
        {
            if (null == effectdict) return;

            foreach (var kv in effectdict)
            {
                kv.Value.Reset();
                RILCache.Set(kv.Value);
            }
            effectdict.Clear();
            RILCache.Set(effectdict);
            effectdict = null;
        }
    }
}
