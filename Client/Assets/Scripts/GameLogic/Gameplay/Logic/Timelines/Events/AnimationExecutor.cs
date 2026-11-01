using GameConf.Gameplay;
using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 动画指令执行器
    /// 时间轴节点负责切换动画名称与速度倍率, 不接管动画推进:
    /// 动画由 Facade.OnTick 按逻辑帧累加 animelapsed, 倍率由节点的 Speed 决定 (0 为定格)
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

            // 动画节点同时声明施法期间是否锁定移动 (Move=false 锁定),
            // 写在时间轴信息上, Movement 每帧检查; 时间轴结束随 info 移除自动解锁
            timeline.current.lockmove = false == timelineevent.Move;

            facade.SetAnimation(name);
            // SetAnimation 会把速度重置为 1, 必须在其后设置
            facade.SetAnimationSpeed((FP)timelineevent.Speed);
            facade.SetAnimationLoop(timelineevent.Loop);
        }
    }
}
