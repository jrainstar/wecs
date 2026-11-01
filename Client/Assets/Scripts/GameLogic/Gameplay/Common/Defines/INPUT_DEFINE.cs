namespace GameLogic.Gameplay.Common
{
    /// <summary>
    /// 输入定义
    /// </summary>
    public class INPUT_DEFINE
    {
        /// <summary>
        /// 摇杆
        /// </summary>
        public const ushort JOYSTICK = 1001;
        /// <summary>
        /// 按钮 A 绑定鼠标左键
        /// </summary>
        public const ushort BA = 1002;
        /// <summary>
        /// 按钮 B 绑定键盘Q
        /// </summary>
        public const ushort BB = 1003;
        /// <summary>
        /// 按钮 C 绑定键盘E
        /// </summary>
        public const ushort BC = 1004;
        /// <summary>
        /// 按钮 D 绑定键盘R
        /// </summary>
        public const ushort BD = 1005;

        /// <summary>
        /// 需要每帧同步到逻辑层的输入类型
        /// 新增按键只需要在此登记, LocalDirector 会自动采集并下发
        /// 方向统一按相机相对处理, 由 Director 转换为世界方向
        /// </summary>
        public static ushort[] SYNC_INPUTS { get; private set; } =
        {
            JOYSTICK,
            BA,
            BB,
            BC,
            BD,
        };

        /// <summary>
        /// GM 指令
        /// </summary>
        public const ushort GM = 0;
        /// <summary>
        /// 时间缩放指令
        /// </summary>
        public const ushort TIMESCALE = 1;
    }
}