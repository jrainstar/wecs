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
            typeof(Movement),
            typeof(Facade),
            typeof(RILSync),
        };
    }
}
