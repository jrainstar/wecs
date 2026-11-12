using GameConf.Gameplay;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 伤害指令执行器
    /// 事件只描述伤害怎么算, 打谁由上下文 target 给出;
    /// 执行器本身无状态, 上下文由派发方 (AOE 重叠 / 时间轴) 每次传入
    /// </summary>
    public class DamageExecutor : Executor<DamageEvent>
    {
        public override void HandleEvent(DamageEvent timelineevent)
        {
            if (null == timelineevent) return;
            // 没有目标 (例如只做表现的时间轴节点) 不结算
            if (0 == target) return;

            stage.damage.Apply(timelineevent, caster, target);
        }
    }
}
