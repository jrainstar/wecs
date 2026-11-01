using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 技能发射器信息
    /// </summary>
    public class SkillLauncherInfo : BehaviorInfo
    {
        /// <summary>
        /// 正在进行的技能 ID
        /// </summary>
        public uint skill { get; set; }

        /// <summary>
        /// 是否有技能在释放中
        /// </summary>
        public bool casting { get; set; }

        /// <summary>
        /// 技能管线
        /// </summary>
        public ulong timeline { get; set; }

        /// <summary>
        /// 技能列表
        /// </summary>
        public List<uint> loadedskills { get; set; }

        /// <summary>
        /// 技能字典, 键为技能 id, 值为技能actor
        /// </summary>
        public Dictionary<uint, ulong> loadedskilldict { get; set; }

        /// <summary>
        /// 技能槽位表, 下标为槽位序号, 值为技能 ID (0 表示该槽位没有技能)
        /// 按键与槽位一一对应, 见 SKILL_DEFINE.SLOT_INPUTS
        /// </summary>
        public uint[] slots { get; set; }


        protected override void OnReady()
        {
            skill = 0;
            timeline = 0;
            casting = false;
            loadedskills = ObjectCache.Ensure<List<uint>>();
            loadedskilldict = ObjectCache.Ensure<Dictionary<uint, ulong>>();
            slots = new uint[SKILL_DEFINE.SLOT_COUNT];
        }

        protected override void OnReset()
        {
            skill = 0;
            timeline = 0;
            casting = false;
            loadedskills.Clear();
            ObjectCache.Set(loadedskills);
            loadedskilldict.Clear();
            ObjectCache.Set(loadedskilldict);
            System.Array.Clear(slots, 0, slots.Length);
        }

        protected override BehaviorInfo OnClone()
        {
            var clone = ObjectCache.Ensure<SkillLauncherInfo>();
            clone.Ready(actor);
            clone.skill = skill;
            clone.casting = casting;
            clone.timeline = timeline;
            clone.loadedskills.AddRange(loadedskills);
            foreach (var kv in loadedskilldict) clone.loadedskilldict.Add(kv.Key, kv.Value);
            System.Array.Copy(slots, clone.slots, slots.Length);

            return clone;
        }

    }
}