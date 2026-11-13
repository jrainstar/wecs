using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 状态机, 用于管理实体的状态切换
    /// </summary>
    public class StateMachine : Behavior<StateMachineInfo>
    {
        /// <summary>
        /// 中断状态
        /// </summary>
        public void Break()
        {
            ChangeState(STATE_DEFINE.NONE);
        }

        /// <summary>
        /// 延迟中断状态
        /// </summary>
        /// <param name="delay">延迟时间</param>
        public void Break(FP delay)
        {
            info.usedelaybreak = true;
            info.delaybreak = delay;
        }

        /// <summary>
        /// 受击
        /// 强制进入受击状态并僵直一段时间, 期间 PASSES 限制不能移动/释放技能;
        /// 僵直结束自动中断状态, 由 Movement 在帧末切回待机/移动
        /// </summary>
        /// <param name="stun">僵直时长</param>
        public void Hit(FP stun)
        {
            // 死亡后不再受击
            if (STATE_DEFINE.DEATH == info.current) return;

            // 打断正在释放的技能 (含技能时间轴表现), 否则技能会继续播完, 受击动作被压住
            if (stage.SeekBehavior(actor, out SkillLauncher launcher)) launcher.Break();

            ChangeState(STATE_DEFINE.BEHIT);

            // 时间轴显式指定的动画 (AnimationExecutor 写的 animname) 会让 ChangeState 跳过动画切换,
            // 受击动作优先级最高, 这里强制切到受击动画, 同时清掉时间轴留下的动画名
            if (stage.SeekBehavior(actor, out Facade facade))
            {
                facade.SetAnimation(STATE_DEFINE.BEHIT);
                // 受击动画只播一次, 播完停在末帧, 剩余僵直时间不再重复播放
                facade.SetAnimationLoop(false);
            }

            // 僵直计时, 到点自动中断; 持续受击会不断刷新这个时长
            Break(stun);
        }

        /// <summary>
        /// 尝试切换状态
        /// </summary>
        /// <param name="state">状态</param>
        /// <returns>YES/NO</returns>
        public bool TryChangeState(byte state)
        {
            if (info.current == state) return true;
            if (false == QueryPassState(state)) return false;

            ChangeState(state);

            return true;
        }

        /// <summary>
        /// 切换到指定状态
        /// 状态切换只负责状态本身, 动画由下面的规则决定:
        ///   1. 进入 CASTING 时不设置动画: 技能动画由时间轴的 AnimationExecutor 驱动;
        ///      没有动画事件的技能保持当前动画 (待机/移动) 继续播放, 而不是切换到不存在的 state=7 动画
        ///   2. animname != null 表示动画被时间轴显式指定 (AnimationExecutor), 状态机不抢
        ///   3. animname == null 说明时间轴没在管动画, 状态机正常设置移动/待机动画
        ///   4. 时间轴播完后由 RefreshAnimation 把动画交还给状态机
        /// </summary>
        /// <param name="state">状态</param>
        public void ChangeState(byte state)
        {
            info.last = info.current;
            info.current = state;
            info.usedelaybreak = false;
            info.delaybreak = FP.Zero;

            if (STATE_DEFINE.CASTING == info.current)
            {
                // 动画交给时间轴, 状态机不动 Facade
                return;
            }

            if (false == stage.SeekBehavior(actor, out Facade facade)) return;
            // 动画已被时间轴显式指定, 状态机不抢;
            // 没被指定 (施法中但时间轴没有动画事件) 则照常切换移动/待机动画
            if (null != facade.info.animname) return;

            facade.SetAnimation(info.current);
        }

        /// <summary>
        /// 重新应用当前状态的动画
        /// 技能时间轴播完后由 SkillLauncher 调用, 把动画控制权交还状态机
        /// </summary>
        public void RefreshAnimation()
        {
            if (STATE_DEFINE.NONE == info.current) return;
            if (STATE_DEFINE.CASTING == info.current) return;
            if (false == stage.SeekBehavior(actor, out Facade facade)) return;

            facade.SetAnimation(info.current);
        }

        protected override void OnTick(FP tick)
        {
            base.OnTick(tick);
            if (false == info.usedelaybreak) return;
            info.delaybreak -= tick;
            if (info.delaybreak <= FP.Zero) Break();
        }

        /// <summary>
        /// 查询当前状态是否可以切换到指定状态
        /// </summary>
        /// <param name="state">状态</param>
        /// <returns>YES/NO</returns>
        public bool QueryPassState(byte state)
        {
            if (STATE_DEFINE.NONE == info.current) return true;
            if (STATE_DEFINE.PASSES.TryGetValue(info.current, out var passes) && passes.Contains(state))
            {
                return true;
            }

            return false;
        }
    }
}