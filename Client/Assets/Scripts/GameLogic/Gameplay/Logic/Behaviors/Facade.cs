using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 外观行为
    /// </summary>
    public class Facade : Behavior<FacadeInfo>
    {
        /// <summary>
        /// 设置模型
        /// </summary>
        /// <param name="model">heroid</param>
        public void SetModel(int model)
        {
            info.model = model;
        }

        /// <summary>
        /// 设置动画状态
        /// </summary>
        /// <param name="state">动画状态</param>
        public void SetAnimation(byte state)
        {
            // 同一状态且动画未被时间轴显式指定时, 视为同一动画继续播放, 不重置进度
            // 场景: 施法中移动, CASTING→MOVE 切换会再次 SetAnimation(MOVE), 跑动动画不应从头重播
            if (info.animstate == state && null == info.animname) return;

            info.animstate = state;
            info.animname = null;
            info.animloop = true;
            info.animelapsed = 0;
            info.animspeed = FP.One;
        }

        /// <summary>
        /// 设置动画名称
        /// </summary>
        /// <param name="animname">动画名称</param>
        public void SetAnimation(string animname)
        {
            info.animname = animname;
            info.animelapsed = 0;
            info.animspeed = FP.One;
        }

        /// <summary>
        /// 设置动画速度倍率, 0 为定格
        /// </summary>
        /// <param name="speed">速度倍率</param>
        public void SetAnimationSpeed(FP speed)
        {
            info.animspeed = speed;
        }

        /// <summary>
        /// 设置动画是否循环
        /// </summary>
        /// <param name="loop">是否循环</param>
        public void SetAnimationLoop(bool loop)
        {
            info.animloop = loop;
        }

        protected override void OnTick(FP tick)
        {
            base.OnTick(tick);
            info.animelapsed += tick * info.animspeed;

        }
    }
}