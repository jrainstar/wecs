using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 单个特效的数据
    /// 特效的存在时长由逻辑层持有, 渲染层只按 elapsed 采样, 保证快照/回滚后表现可复现
    /// </summary>
    public class EffectData
    {
        /// <summary>
        /// 特效资源编号 (TbResourceData), 0 表示不播
        /// </summary>
        public int fx { get; set; }
        /// <summary>
        /// 挂点骨骼, 空表示挂在单位根节点
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
        /// 相对挂点的旋转 (欧拉角, 度)
        /// </summary>
        public FPVector3 rotate { get; set; }
        /// <summary>
        /// 缩放
        /// </summary>
        public FP scale { get; set; }
        /// <summary>
        /// 已流逝时间
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
            fx = 0;
            anchor = null;
            follow = false;
            offset = FPVector3.Zero;
            rotate = FPVector3.Zero;
            scale = FP.One;
            elapsed = FP.Zero;
            duration = FP.Zero;
        }

        /// <summary>
        /// 拷贝
        /// </summary>
        /// <param name="data">来源</param>
        public void Copy(EffectData data)
        {
            fx = data.fx;
            anchor = data.anchor;
            follow = data.follow;
            offset = data.offset;
            rotate = data.rotate;
            scale = data.scale;
            elapsed = data.elapsed;
            duration = data.duration;
        }
    }

    /// <summary>
    /// 特效信息
    /// </summary>
    public class EffectInfo : BehaviorInfo
    {
        /// <summary>
        /// 特效集合 (特效编号 -> 特效数据)
        /// </summary>
        public Dictionary<uint, EffectData> effectdict { get; set; }
        /// <summary>
        /// 特效编号自增, 用于区分同一个单位上的多个特效
        /// </summary>
        public uint increment { get; set; }

        protected override void OnReady()
        {
            increment = 0;
            effectdict = ObjectCache.Ensure<Dictionary<uint, EffectData>>();
            effectdict.Clear();
        }

        protected override void OnReset()
        {
            increment = 0;
            Clear();
        }

        protected override BehaviorInfo OnClone()
        {
            var clone = ObjectCache.Ensure<EffectInfo>();
            clone.Ready(actor);
            clone.increment = increment;

            if (null != effectdict)
            {
                foreach (var kv in effectdict)
                {
                    var data = ObjectCache.Ensure<EffectData>();
                    data.Copy(kv.Value);
                    clone.effectdict.Add(kv.Key, data);
                }
            }

            return clone;
        }

        /// <summary>
        /// 清空特效集合
        /// </summary>
        private void Clear()
        {
            if (null == effectdict) return;

            foreach (var kv in effectdict)
            {
                kv.Value.Reset();
                ObjectCache.Set(kv.Value);
            }
            effectdict.Clear();
            ObjectCache.Set(effectdict);
            effectdict = null;
        }
    }
}
