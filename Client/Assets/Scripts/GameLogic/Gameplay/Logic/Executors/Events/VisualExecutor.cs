using GameConf.Gameplay;
using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 表现指令执行器
    /// 时间轴上的表现节点不直接画东西, 只往宿主的 Effect 行为上挂一条特效数据,
    /// 之后由 Effect 行为计时、由 RIL_FACADE_EFFECT 驱动渲染层的 EffectAgent 采样播放
    /// 执行器是无状态的单例, 宿主/施法者通过 Timeline.current 获取
    /// </summary>
    public class VisualExecutor : Executor<EffectEvent>
    {
        public override void HandleEvent(EffectEvent timelineevent)
        {
            if (null == timelineevent) return;

            var timeline = stage.timeline;
            if (null == timeline || null == timeline.current) return;

            // 施法者优先取时间轴上记录的 caster, 没有 (非技能时间轴) 则用时间轴宿主
            var caster = timeline.current.caster;
            if (0 == caster || false == stage.cache.Valid(caster)) caster = timeline.current.actor;
            if (0 == caster || false == stage.cache.Valid(caster)) return;

            var effect = stage.GetBehavior<Effect>(caster);
            if (null == effect) effect = stage.AddBehavior<Effect>(caster);

            effect.Play(
                timelineevent.Fx,
                timelineevent.Anchor,
                timelineevent.Follow,
                Offset(timelineevent.Offset),
                Rotate(timelineevent.Rotate),
                (FP)timelineevent.Scale,
                (FP)timelineevent.Duration);
        }

        /// <summary>
        /// 偏移 x|y|z, 缺省为零
        /// </summary>
        /// <param name="offset">配置的偏移</param>
        /// <returns>偏移</returns>
        private static FPVector3 Offset(float[] offset)
        {
            if (null == offset || offset.Length < 3) return FPVector3.Zero;

            return new FPVector3(offset[0], offset[1], offset[2]);
        }

        /// <summary>
        /// 旋转 x|y|z (欧拉角, 度), 缺省为零
        /// </summary>
        /// <param name="rotate">配置的旋转</param>
        /// <returns>旋转</returns>
        private static FPVector3 Rotate(float[] rotate)
        {
            if (null == rotate || rotate.Length < 3) return FPVector3.Zero;

            return new FPVector3(rotate[0], rotate[1], rotate[2]);
        }
    }
}
