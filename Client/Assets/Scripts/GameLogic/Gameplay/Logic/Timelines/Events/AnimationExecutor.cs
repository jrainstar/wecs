using GameConf.Gameplay;
using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 动画指令执行器
    /// 时间轴节点只负责切换动画名称, 不接管动画推进:
    ///   Speed &gt; 0  - TICK_AUTOMATIC, 由 Facade.OnTick 按逻辑帧累加 animelapsed
    ///   Speed == 0 - TICK_MANUAL, 动画定格, 由外部通过 Facade.SetAnimationElapsed 推进
    /// 时间轴播完后不会还原动画, 角色保持在节点指定的动画上, 直到
    /// Movement / StateMachine 等行为切换状态时再切回移动或待机
    /// </summary>
    public class AnimationExecutor : Executor<AnimationEvent>
    {
        public override void HandleEvent(AnimationEvent timelineevent)
        {
            var name = timelineevent.Name;
            if (string.IsNullOrEmpty(name)) return;

            // 执行器是单例被所有时间轴共享, 宿主通过 Timeline.current 获取
            var timeline = stage.timeline;
            if (null == timeline || null == timeline.current) return;

            var host = timeline.current.actor;
            if (false == stage.SeekBehavior(host, out Facade facade)) return;

            var animticktype = timelineevent.Speed > 0 ? ANIM_DEFINE.TICK_AUTOMATIC : ANIM_DEFINE.TICK_MANUAL;

            // 先设循环, SetAnimation 内部会重置 animelapsed
            facade.SetAnimation(name, animticktype);
            facade.SetAnimationLoop(timelineevent.Loop);
        }
    }
}
