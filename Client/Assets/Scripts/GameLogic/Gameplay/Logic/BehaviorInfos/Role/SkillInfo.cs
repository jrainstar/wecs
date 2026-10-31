using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 技能信息
    /// </summary>
    public class SkillInfo : BehaviorInfo
    {
        /// <summary>
        /// 技能 ID
        /// </summary>
        public int id { get; set; }

        /// <summary>
        /// 拥有者
        /// </summary>
        public ulong owner { get; set; }

        /// <summary>
        /// 技能冷却
        /// </summary>
        public FP cooldown { get; set; }

        protected override void OnReady()
        {
            OnReset();
        }

        protected override void OnReset()
        {
            id = 0;
            owner = 0;
            cooldown = 0;
        }

        protected override BehaviorInfo OnClone()
        {
            var clone = ObjectCache.Ensure<SkillInfo>();
            clone.Ready(actor);

            clone.id = id;
            clone.owner = owner;
            clone.cooldown = cooldown;

            return clone;
        }

    }
}