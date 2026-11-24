using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 技能面板渲染指令
    /// 携带一个角色当前的技能冷却 / 连招窗口 / 时间轴进度, 只给 UI 显示, 不参与逻辑;
    /// 数组字段首次分配后一直复用, OnReset 只清数据不归还, 避免每帧重新分配
    /// </summary>
    public class RIL_SKILL : IRIL
    {
        public override ushort id => RIL_DEFINE.SKILL;

        /// <summary>
        /// 槽位技能 ID, 0 表示空槽位
        /// </summary>
        public uint[] slots { get; set; }
        /// <summary>
        /// 槽位技能剩余冷却 (秒)
        /// </summary>
        public FP[] cooldowns { get; set; }
        /// <summary>
        /// 槽位技能冷却总时长 (秒)
        /// </summary>
        public FP[] durations { get; set; }
        /// <summary>
        /// 正在释放的技能 ID, 0 表示没有在释放
        /// </summary>
        public uint casting { get; set; }
        /// <summary>
        /// 连招窗口 BUFF ID, 0 表示没有窗口
        /// </summary>
        public int window { get; set; }
        /// <summary>
        /// 连招窗口剩余时长 (秒)
        /// </summary>
        public FP windowremaining { get; set; }
        /// <summary>
        /// 连招窗口总时长 (秒)
        /// </summary>
        public FP windowduration { get; set; }
        /// <summary>
        /// 时间轴 ID, 0 表示没有时间轴
        /// </summary>
        public int timeline { get; set; }
        /// <summary>
        /// 时间轴已运行时间 (秒)
        /// </summary>
        public FP timelineelapsed { get; set; }
        /// <summary>
        /// 时间轴总时长 (秒)
        /// </summary>
        public FP timelinemax { get; set; }

        protected override void OnReady()
        {
            if (null == slots || slots.Length != SKILL_DEFINE.SLOT_COUNT)
            {
                slots = new uint[SKILL_DEFINE.SLOT_COUNT];
                cooldowns = new FP[SKILL_DEFINE.SLOT_COUNT];
                durations = new FP[SKILL_DEFINE.SLOT_COUNT];
            }

            Clear();
        }

        protected override void OnReset()
        {
            Clear();
        }

        /// <summary>
        /// 清空数据 (数组保留复用)
        /// </summary>
        private void Clear()
        {
            if (null != slots)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    slots[i] = 0;
                    if (null != cooldowns && i < cooldowns.Length) cooldowns[i] = FP.Zero;
                    if (null != durations && i < durations.Length) durations[i] = FP.Zero;
                }
            }

            casting = 0;
            window = 0;
            windowremaining = FP.Zero;
            windowduration = FP.Zero;
            timeline = 0;
            timelineelapsed = FP.Zero;
            timelinemax = FP.Zero;
        }
    }
}
