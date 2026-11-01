using System.Collections.Generic;

namespace GameLogic.Gameplay.Common
{
    /// <summary>
    /// 技能定义
    /// </summary>
    public class SKILL_DEFINE
    {
        /// <summary>
        /// 技能槽位数量, 与输入按键一一对应
        /// </summary>
        public const int SLOT_COUNT = 4;

        /// <summary>
        /// 槽位对应的输入按键, 下标即槽位序号
        /// </summary>
        public static ushort[] SLOT_INPUTS { get; private set; } =
        {
            INPUT_DEFINE.BA,
            INPUT_DEFINE.BB,
            INPUT_DEFINE.BC,
            INPUT_DEFINE.BD,
        };

        /// <summary>
        /// 主技能槽位 (普攻/主技能)
        /// </summary>
        public const int MAIN_SLOT = 2;
    }
}
