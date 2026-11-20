using GameConf.Gameplay;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// BUFF 指令执行器
    /// 事件只描述"给什么 BUFF", 作用对象由上下文 target 给出, 没有目标时落到施法者自己身上;
    /// BUFF 行为按需挂载, 目标不要求预先带 Buff 行为;
    /// 执行器本身无状态, 不跨事件保存 Actor
    /// </summary>
    public class BuffExecutor : Executor<BuffEvent>
    {
        public override void HandleEvent(BuffEvent buffevent)
        {
            if (null == buffevent) return;
            if (0 >= buffevent.BuffId) return;

            // 命中类负载带明确目标, 时间轴 / 周期触发这类没有目标时加在自己身上
            var bufftarget = 0 != target ? target : caster;
            if (0 == bufftarget) return;
            if (false == stage.cache.Valid(bufftarget)) return;

            var buff = stage.GetBehavior<Buff>(bufftarget);
            if (null == buff) buff = stage.AddBehavior<Buff>(bufftarget);

            buff.Apply(buffevent.BuffId, caster, buffevent.Stack);
        }
    }
}
