using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 技能面板翻译器
    /// 把技能冷却 / 连招窗口 / 时间轴进度汇总成一条 RIL_SKILL, 只给本地角色的 UI 用;
    /// 冷却与时间轴每帧都在变化, 因此这条 RIL 基本每帧都会重发
    /// </summary>
    public class SkillTranslator : Translator<SkillLauncherInfo, RIL_SKILL>
    {
        public override ushort id => RIL_DEFINE.SKILL;

        protected override int OnCalcHashCode(SkillLauncherInfo info)
        {
            int hash = 17;
            hash = hash * 31 + info.actor.GetHashCode();
            hash = hash * 31 + info.skill.GetHashCode();
            hash = hash * 31 + info.casting.GetHashCode();

            var slots = info.slots;
            if (null != slots)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    hash = hash * 31 + slots[i].GetHashCode();
                    hash = hash * 31 + Cooldown(info, slots[i]).GetHashCode();
                }
            }

            var remaining = Window(info, out var windowid, out _);
            hash = hash * 31 + windowid;
            hash = hash * 31 + remaining.GetHashCode();

            if (stage.SeekBehaviorInfo(info.actor, out TimelineInfo timelineinfo))
            {
                hash = hash * 31 + timelineinfo.id;
                hash = hash * 31 + timelineinfo.timeelapsed.GetHashCode();
            }

            return hash;
        }

        protected override void OnRIL(SkillLauncherInfo info, RIL_SKILL ril)
        {
            var slots = info.slots;
            for (int i = 0; i < SKILL_DEFINE.SLOT_COUNT; i++)
            {
                var skill = null != slots && i < slots.Length ? slots[i] : 0;

                ril.slots[i] = skill;
                ril.cooldowns[i] = Cooldown(info, skill);
                ril.durations[i] = Duration(skill);
            }

            ril.casting = info.casting ? info.skill : 0;

            ril.windowremaining = Window(info, out var windowid, out var windowduration);
            ril.window = windowid;
            ril.windowduration = windowduration;

            ril.timeline = 0;
            ril.timelineelapsed = FP.Zero;
            ril.timelinemax = FP.Zero;
            if (stage.SeekBehaviorInfo(info.actor, out TimelineInfo timelineinfo))
            {
                ril.timeline = timelineinfo.id;
                ril.timelineelapsed = timelineinfo.timeelapsed;
                ril.timelinemax = TimelineMax(timelineinfo.id);
            }
        }

        /// <summary>
        /// 读取槽位技能的剩余冷却
        /// </summary>
        /// <param name="info">技能发射器信息</param>
        /// <param name="skill">技能 ID</param>
        /// <returns>剩余冷却, 未载入返回 0</returns>
        private FP Cooldown(SkillLauncherInfo info, uint skill)
        {
            if (0 == skill) return FP.Zero;
            if (null == info.loadedskilldict) return FP.Zero;
            if (false == info.loadedskilldict.TryGetValue(skill, out var skillactor)) return FP.Zero;
            if (false == stage.SeekBehaviorInfo(skillactor, out SkillInfo skillinfo)) return FP.Zero;

            return skillinfo.cooldown;
        }

        /// <summary>
        /// 读取槽位技能的冷却总时长
        /// </summary>
        /// <param name="skill">技能 ID</param>
        /// <returns>冷却总时长, 配置不存在返回 0</returns>
        private FP Duration(uint skill)
        {
            if (0 == skill) return FP.Zero;
            if (null == stage.cfg) return FP.Zero;
            if (false == stage.cfg.location.TbSkillData.TryGetValue((int)skill, out var data)) return FP.Zero;

            return (FP)data.Cooldown;
        }

        /// <summary>
        /// 读取连招窗口
        /// </summary>
        /// <param name="info">技能发射器信息</param>
        /// <param name="buffid">提供窗口的 BUFF 配置 ID</param>
        /// <param name="duration">窗口总时长</param>
        /// <returns>窗口剩余时长</returns>
        private FP Window(SkillLauncherInfo info, out int buffid, out FP duration)
        {
            buffid = 0;
            duration = FP.Zero;
            if (false == stage.SeekBehavior(info.actor, out Buff buff)) return FP.Zero;

            return buff.Window(out buffid, out duration);
        }

        /// <summary>
        /// 读取时间轴总时长
        /// </summary>
        /// <param name="timeline">时间轴 ID</param>
        /// <returns>总时长, 配置不存在返回 0</returns>
        private FP TimelineMax(int timeline)
        {
            if (0 >= timeline) return FP.Zero;
            if (null == stage.cfg) return FP.Zero;
            if (false == stage.cfg.location.TbTimelineData.TryGetValue(timeline, out var data)) return FP.Zero;

            return (FP)data.MaxTime;
        }
    }
}
