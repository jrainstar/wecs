using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 技能预制信息
    /// </summary>
    public class SkillPrefabInfo : PrefabInfo
    {
        /// <summary>
        /// 持有者 ID
        /// </summary>
        public ulong owner { get; set; }
        /// <summary>
        /// 技能信息
        /// </summary>
        public int skill { get; set; }
    }

    /// <summary>
    /// 英雄预制创建器
    /// </summary>
    public class SkillPrefab : Prefab<SkillPrefabInfo>
    {
        public override byte type => ACTOR_DEFINE.SKILL;

        protected override void OnProcessing(ulong actor, SkillPrefabInfo info)
        {
            var skill = stage.AddBehaviorInfo<SkillInfo>(actor);
            skill.id = info.skill;
            skill.owner = info.owner;
            skill.cooldown = 0;
        }
    }
}