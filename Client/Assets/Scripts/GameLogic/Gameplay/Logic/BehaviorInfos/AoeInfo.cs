using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// AOE 信息
    /// </summary>
    public class AoeInfo : BehaviorInfo
    {
        /// <summary>
        /// AOE 配置 ID
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// 施法者
        /// </summary>
        public ulong caster { get; set; }
        /// <summary>
        /// 轨道配置 ID
        /// </summary>
        public int track { get; set; }
        /// <summary>
        /// 速度配置 ID
        /// </summary>
        public int speed { get; set; }
        /// <summary>
        /// 发射延迟剩余时间, 大于 0 时 AOE 不推进
        /// </summary>
        public FP delay { get; set; }
        /// <summary>
        /// 存在时长, 同时也是轨道总时长; 小于等于 0 表示不限制
        /// </summary>
        public FP duration { get; set; }
        /// <summary>
        /// 已飞行时间
        /// </summary>
        public FP elapsed { get; set; }
        /// <summary>
        /// 已飞行路程
        /// </summary>
        public FP distance { get; set; }
        /// <summary>
        /// 轨道起点 (出生点)
        /// </summary>
        public FPVector3 origin { get; set; }
        /// <summary>
        /// 环绕中心
        /// </summary>
        public FPVector3 center { get; set; }
        /// <summary>
        /// 发射方向 (出生时确定, 一直不变)
        /// </summary>
        public FPVector3 forward { get; set; }
        /// <summary>
        /// 发射方向的右侧 (回旋镖的横向弧使用)
        /// </summary>
        public FPVector3 right { get; set; }
        /// <summary>
        /// 当前朝向 (单位向量), 每帧按位移方向刷新
        /// </summary>
        public FPVector3 direction { get; set; }
        /// <summary>
        /// 追踪目标
        /// </summary>
        public ulong target { get; set; }
        /// <summary>
        /// 是否已释放, 延迟结束才挂上外观与碰撞盒
        /// </summary>
        public bool released { get; set; }
        /// <summary>
        /// 配置了哪些触发时机 (AoeTrigger 位掩码)
        /// 用来决定要不要做每帧重叠检测, 纯表现 AOE 保持为 0, 不产生任何检测开销
        /// </summary>
        public ushort triggers { get; set; }
        /// <summary>
        /// 周期触发累计时间
        /// </summary>
        public FP tickelapsed { get; set; }
        /// <summary>
        /// 上一帧的重叠目标, 用于判定进入/离开
        /// </summary>
        public List<ulong> overlaps { get; set; }

        protected override void OnReady()
        {
            Reset();
            overlaps = ObjectCache.Ensure<List<ulong>>();
        }

        protected override void OnReset()
        {
            Reset();
            if (null != overlaps)
            {
                overlaps.Clear();
                ObjectCache.Set(overlaps);
                overlaps = null;
            }
        }

        protected override BehaviorInfo OnClone()
        {
            var clone = ObjectCache.Ensure<AoeInfo>();
            clone.Ready(actor);

            clone.id = id;
            clone.caster = caster;
            clone.track = track;
            clone.speed = speed;
            clone.delay = delay;
            clone.duration = duration;
            clone.elapsed = elapsed;
            clone.distance = distance;
            clone.origin = origin;
            clone.center = center;
            clone.forward = forward;
            clone.right = right;
            clone.direction = direction;
            clone.target = target;
            clone.released = released;
            clone.triggers = triggers;
            clone.tickelapsed = tickelapsed;

            if (null != overlaps && null != clone.overlaps)
            {
                foreach (var id in overlaps) clone.overlaps.Add(id);
            }

            return clone;
        }

        /// <summary>
        /// 重置标量字段
        /// </summary>
        private void Reset()
        {
            id = 0;
            caster = 0;
            track = 0;
            speed = 0;
            delay = FP.Zero;
            duration = FP.Zero;
            elapsed = FP.Zero;
            distance = FP.Zero;
            origin = FPVector3.zero;
            center = FPVector3.zero;
            forward = FPVector3.forward;
            right = FPVector3.right;
            direction = FPVector3.forward;
            target = 0;
            released = false;
            triggers = 0;
            tickelapsed = FP.Zero;
        }
    }
}
