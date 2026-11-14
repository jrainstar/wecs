using System;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// Tick 定义
    /// </summary>
    public class TICK_DEFINE
    {
        /// <summary>
        /// Tick 时序
        /// 注意: 目前只登记了已移植的 System, 后续移植新的 System 时, 需要按驱动顺序补充到此处
        /// </summary>
        public static List<Type> TICK_TYPE_LIST { get; private set; } = new()
        {            
            typeof(Gamepad),
            // 状态机只做僵直计时 (延迟中断), 必须排在 Movement 之前:
            // 僵直结束的当帧就能被 Movement 接管, 少一帧输入延迟
            typeof(StateMachine),
            typeof(Detection),
            typeof(Movement),
            typeof(Timeline),
            // 定时器排在 Timeline 之后: 定时任务本帧触发的效果 (AOE/技能) 当帧就能被驱动
            typeof(Timer),
            // AOE 必须排在 Timeline 之后: 时间轴本帧发射的 AOE 当帧就能被驱动
            typeof(Aoe),
            typeof(SkillLauncher),
            typeof(Facade),
            // 特效计时, 必须在 RILSync 之前把本帧的 elapsed 同步给渲染层
            typeof(Effect),
            typeof(RILSync),
        };
    }
}
