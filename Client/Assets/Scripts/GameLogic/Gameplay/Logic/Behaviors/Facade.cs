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
            info.animmotion = false;
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
            info.animmotion = false;
        }

        /// <summary>
        /// 设置是否应用动画关键帧位移
        /// 必须在 SetAnimation 之后调用 (SetAnimation 会清掉该标记)
        /// 渲染层通过 RIL 拿到该标记锁根骨骼, 位移本身由本行为每帧采样关键帧应用
        /// </summary>
        /// <param name="motion">是否应用位移</param>
        public void SetAnimationMotion(bool motion)
        {
            info.animmotion = motion;
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
            var begin = info.animelapsed;
            info.animelapsed += tick * info.animspeed;
            ApplyMotion(begin, info.animelapsed);
        }

        /// <summary>
        /// 应用动画关键帧位移
        /// 采样关键帧文件中根节点 (Root 分组) 的位置, 把本帧的位置增量按角色朝向转到
        /// 世界方向后叠加到空间位置上:
        ///   不循环: 片段时长内应用一次, 播完后不再产生位移
        ///   循环:   每循环一次应用一次, 直到时间轴结束
        /// 位移由时间轴动画节点的 ApplyMotion 声明, 时间轴结束 (TimelineInfo 移除) 后
        /// 不再应用, 因此循环动画的位移不会无限叠加;
        /// FacadeInfo.animmotion 只负责告诉渲染层锁根骨骼, 会一直保留到动画被切换
        /// </summary>
        /// <param name="begin">本帧动画起始时间</param>
        /// <param name="end">本帧动画结束时间</param>
        private void ApplyMotion(FP begin, FP end)
        {
            if (end <= begin) return;
            // 定格 (速度 0) 时动画不推进, 自然也没有位移
            if (info.animspeed <= FP.Zero) return;
            if (string.IsNullOrEmpty(info.animname)) return;
            // 位移是时间轴动画节点的功能, 时间轴结束后 TimelineInfo 被移除, 位移随之停止
            if (false == stage.SeekBehaviorInfo(actor, out TimelineInfo timelineinfo)) return;
            if (false == timelineinfo.motionapply) return;
            if (false == stage.SeekBehaviorInfo(actor, out SpatialInfo spatial)) return;

            // ApplyMotion 为 false 时不会走到这里, 也就不读取关键帧文件
            var clipdata = AnimationKeyframeCache.Load($"{info.model}/{info.animname}");
            if (null == clipdata) return;

            var node = clipdata.FindMotionNode();
            if (null == node) return;

            // 采样全程使用 FP, 不能走浮点, 否则不同步
            var motion = clipdata.SampleMotion(node, begin, end, info.animloop);
            if (FP.Zero == motion.x && FP.Zero == motion.y && FP.Zero == motion.z) return;

            // 关键帧位置以动画根原点为基准 (模型空间), 按角色当前朝向转到世界方向
            var yaw = spatial.euler.y * FPMath.Deg2Rad;
            var sin = FPMath.Sin(yaw);
            var cos = FPMath.Cos(yaw);
            spatial.position += new FPVector3(
                motion.x * cos + motion.z * sin,
                motion.y,
                motion.z * cos - motion.x * sin);
        }
    }
}