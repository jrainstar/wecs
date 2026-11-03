using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 外观信息
    /// </summary>
    public class FacadeInfo : BehaviorInfo
    {
        /// <summary>
        /// 模型 ID
        /// </summary>
        public int model { get; set; }
        /// <summary>
        /// 动画速度倍率, 0 为定格
        /// </summary>
        public FP animspeed { get; set; }
        /// <summary>
        /// 动画状态
        /// </summary>
        public byte animstate { get; set; }
        /// <summary>
        /// 动画名称
        /// </summary>
        public string animname { get; set; }
        /// <summary>
        /// 动画是否循环
        /// </summary>
        public bool animloop { get; set; }
        /// <summary>
        /// 流逝时间
        /// </summary>
        public FP animelapsed { get; set; }
        /// <summary>
        /// 是否应用动画关键帧位移
        /// 逻辑层据此在 Facade 中采样关键帧驱动角色位移, 渲染层据此锁住根骨骼,
        /// 避免动画自带的根骨骼位移与逻辑位移叠加
        /// </summary>
        public bool animmotion { get; set; }

        protected override void OnReady()
        {
            model = 0;
            animspeed = FP.One;
            animstate = 0;
            animname = null;
            animloop = true;
            animelapsed = FP.Zero;
            animmotion = false;
        }

        protected override void OnReset()
        {
            model = 0;
            animspeed = FP.One;
            animstate = 0;
            animname = null;
            animloop = true;
            animelapsed = FP.Zero;
            animmotion = false;
        }

        protected override BehaviorInfo OnClone()
        {
            var clone = ObjectCache.Ensure<FacadeInfo>();
            clone.Ready(actor);
            clone.model = model;
            clone.animstate = animstate;
            clone.animname = animname;
            clone.animloop = animloop;
            clone.animspeed = animspeed;
            clone.animelapsed = animelapsed;
            clone.animmotion = animmotion;

            return clone;
        }
    }
}