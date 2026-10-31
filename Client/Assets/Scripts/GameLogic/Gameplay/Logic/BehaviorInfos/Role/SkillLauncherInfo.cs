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


        protected override void OnReady()
        {
            skill = 0;
            timeline = 0;
            casting = false;
            loadedskills = ObjectCache.Ensure<List<uint>>();
            loadedskilldict = ObjectCache.Ensure<Dictionary<uint, ulong>>();
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

            return clone;
        }

    }
}