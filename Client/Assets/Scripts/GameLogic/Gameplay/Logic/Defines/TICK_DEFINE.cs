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
            typeof(Detection),
            typeof(Movement),
            typeof(Timeline),
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
