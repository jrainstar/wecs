using GameConf.Gameplay;
using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 技能释放校验结果
    /// </summary>
    public enum LAUNCH_RESULT
    {
        /// <summary>
        /// 可以释放
        /// </summary>
        OK = 0,
        /// <summary>
        /// 技能 ID 无效
        /// </summary>
        INVALID,
        /// <summary>
        /// 技能未载入
        /// </summary>
        UNLOADED,
        /// <summary>
        /// 冷却中
        /// </summary>
        COOLDOWN,
        /// <summary>
        /// 条件不满足
        /// </summary>
        COND,
        /// <summary>
        /// 消耗不足
        /// </summary>
        COST,
        /// <summary>
        /// 状态不允许 (例如死亡/受击/其它技能释放中)
        /// </summary>
        STATE,
    }

    /// <summary>
    /// 技能发射器
    /// 负责技能的装载/卸载/释放, 技能的流程表现统一由 Timeline 驱动
    /// </summary>
    public class SkillLauncher : Behavior<SkillLauncherInfo>
    {
        /// <summary>
        /// 默认时间缩放
        /// </summary>
        private static readonly FP DEFAULT_TIMESCALE = FP.One;

        protected override void OnTick(FP tick)
        {
            base.OnTick(tick);
            // 按键触发释放
            LaunchCheck();
            // 冷却计时, 按逻辑帧步长 (已含 Ticker 缩放) 递减
            var skills = info.loadedskills;
            if (null == skills || 0 == skills.Count) return;
            for (int i = 0; i < skills.Count; i++)
            {
                if (false == info.loadedskilldict.TryGetValue(skills[i], out var skillactor)) continue;
                if (false == stage.SeekBehaviorInfo(skillactor, out SkillInfo skillinfo)) continue;
                if (skillinfo.cooldown <= FP.Zero) continue;
                skillinfo.cooldown -= tick;
                if (skillinfo.cooldown <= FP.Zero) skillinfo.cooldown = FP.Zero;
            }
            // 释放中的技能, 时间轴播完即视为释放结束
            IsCasting();
        }

        /// <summary>
        /// 载入技能
        /// 同一个技能重复载入会直接返回已存在的技能 Actor
        /// 载入后自动占用一个空闲槽位, 按键即可释放, 无需额外代码
        /// </summary>
        /// <param name="skill">技能 ID</param>
        /// <returns>技能 Actor, 配置不存在或已卸载时返回 0</returns>
        public ulong Load(uint skill)
        {
            if (0 == skill) return 0;
            if (false == stage.cache.Valid(actor)) return 0;
            // 已载入, 直接复用 (含同帧内刚卸载尚未回收的)
            if (info.loadedskilldict.TryGetValue(skill, out var exist)) return exist;
            if (false == stage.cfg.location.TbSkillData.TryGetValue((int)skill, out var skilldata)) return 0;

            var skillactor = stage.Spawn(new SkillPrefabInfo
            {
                owner = actor,
                skill = skilldata.Id,
            });

            info.loadedskilldict.Add(skill, skillactor);
            info.loadedskills.Add(skill);
            // 自动占位, 顺序即槽位顺序
            Slot(skill);

            return skillactor;
        }

        /// <summary>
        /// 载入技能并指定槽位
        /// </summary>
        /// <param name="skill">技能 ID</param>
        /// <param name="slot">槽位序号, 见 SKILL_DEFINE.SLOT_INPUTS</param>
        /// <returns>技能 Actor, 槽位越界或配置不存在时返回 0</returns>
        public ulong Load(uint skill, int slot)
        {
            if (0 > slot || SKILL_DEFINE.SLOT_COUNT <= slot) return 0;
            if (0 == Load(skill)) return 0;

            info.slots[slot] = skill;

            return info.loadedskilldict[skill];
        }

        /// <summary>
        /// 卸载技能
        /// 正在释放中的技能不允许卸载
        /// </summary>
        /// <param name="skill">技能 ID</param>
        public void Unload(uint skill)
        {
            if (false == info.loadedskilldict.TryGetValue(skill, out var skillactor)) return;
            // 释放中的技能, 等管线播完再卸载
            if (info.casting && info.skill == skill) return;

            info.loadedskilldict.Remove(skill);
            info.loadedskills.Remove(skill);
            // 释放占用的槽位
            for (int i = 0; i < info.slots.Length; i++)
            {
                if (info.slots[i] != skill) continue;
                info.slots[i] = 0;
            }

            stage.RmvActor(skillactor);
        }

        /// <summary>
        /// 技能占用槽位
        /// </summary>
        /// <param name="skill">技能 ID</param>
        /// <returns>槽位序号, 已占位返回原槽位, 无空闲槽位返回 -1</returns>
        private int Slot(uint skill)
        {
            for (int i = 0; i < info.slots.Length; i++)
            {
                if (info.slots[i] == skill) return i;
            }

            for (int i = 0; i < info.slots.Length; i++)
            {
                if (0 != info.slots[i]) continue;

                info.slots[i] = skill;

                return i;
            }

            return -1;
        }

        /// <summary>
        /// 查询槽位上的技能
        /// </summary>
        /// <param name="slot">槽位序号</param>
        /// <returns>技能 ID, 空槽位返回 0</returns>
        public uint QuerySlot(int slot)
        {
            if (0 > slot || SKILL_DEFINE.SLOT_COUNT <= slot) return 0;

            return info.slots[slot];
        }

        /// <summary>
        /// 释放技能
        /// 校验通过后切换 CASTING 状态, 并在自己身上挂起技能管线 (Timeline)
        /// 同一条时间轴播完会自动回收, 期间 casting 保持为 true
        /// </summary>
        /// <param name="skill">技能 ID</param>
        /// <returns>YES/NO 是否成功释放</returns>
        public bool Launch(uint skill)
        {
            if (LAUNCH_RESULT.OK != CanLaunch(skill, out var skillinfo, out var skilldata)) return false;

            // 切换到技能状态, 切换失败 (例如死亡/受击中) 则不能释放
            if (stage.SeekBehavior(actor, out StateMachine machine))
            {
                if (false == machine.TryChangeState(STATE_DEFINE.CASTING)) return false;
            }

            // 扣除消耗
            if (false == Cost(skilldata.Cost))
            {
                // 扣费失败回退状态
                if (null != machine) machine.Break();
                return false;
            }

            // 进入冷却
            skillinfo.cooldown = (FP)skilldata.Cooldown;

            info.skill = skill;
            info.casting = true;
            info.timeline = 0;

            // 技能管线, 挂在释放者身上, caster 指向自己
            if (skilldata.Timeline > 0)
            {
                if (null != stage.timeline)
                {
                    var timelineinfo = stage.timeline.Play(actor, skilldata.Timeline, DEFAULT_TIMESCALE, actor);
                    if (null != timelineinfo) info.timeline = timelineinfo.actor;
                }
            }

            return true;
        }

        /// <summary>
        /// 中断当前技能
        /// </summary>
        public void Break()
        {
            if (false == info.casting) return;
            if (stage.SeekBehaviorInfo(info.timeline, out TimelineInfo timelineinfo)) stage.RmvBehaviorInfo(timelineinfo);

            info.skill = 0;
            info.timeline = 0;
            info.casting = false;

            if (false == stage.SeekBehavior(actor, out StateMachine machine)) return;
            if (STATE_DEFINE.CASTING != machine.info.current) return;
            machine.Break();
        }

        /// <summary>
        /// 按键触发释放技能
        /// 遍历槽位, 槽位按键按下即尝试释放该槽位的技能
        /// 新增技能只要 Load 到槽位即可, 不需要再为每个技能新增代码
        /// 注意: 按键判定用的是 press (按住), 释放中 CanLaunch 会拦下重入,
        /// 因此同一次长按只会在一次释放内生效; 如需要"按一次放一次", 应改用按下沿 (wasPressedThisFrame)
        /// </summary>
        private void LaunchCheck()
        {
            if (false == stage.SeekBehavior(actor, out Gamepad gamepad)) return;

            for (int i = 0; i < SKILL_DEFINE.SLOT_COUNT; i++)
            {
                var skill = info.slots[i];
                if (0 == skill) continue;

                var input = gamepad.GetInput(SKILL_DEFINE.SLOT_INPUTS[i]);
                if (false == input.press) continue;

                if (Launch(skill)) return;
            }
        }

        /// <summary>
        /// 是否正在释放技能
        /// </summary>
        /// <returns>YES/NO</returns>
        public bool IsCasting()
        {
            if (false == info.casting) return false;
            // 时间轴已经播完 (BehaviorInfo 被移除), 技能释放结束
            if (0 == info.timeline) return true;
            if (stage.SeekBehaviorInfo<TimelineInfo>(info.timeline, out _, true)) return true;

            info.skill = 0;
            info.timeline = 0;
            info.casting = false;

            return false;
        }

        /// <summary>
        /// 查询技能冷却剩余时间
        /// </summary>
        /// <param name="skill">技能 ID</param>
        /// <returns>剩余冷却, 未载入返回 0</returns>
        public FP QueryCooldown(uint skill)
        {
            if (false == info.loadedskilldict.TryGetValue(skill, out var skillactor)) return FP.Zero;
            if (false == stage.SeekBehaviorInfo(skillactor, out SkillInfo skillinfo)) return FP.Zero;

            return skillinfo.cooldown;
        }

        /// <summary>
        /// 查询技能是否可用
        /// </summary>
        /// <param name="skill">技能 ID</param>
        /// <returns>YES/NO</returns>
        public bool CanLaunch(uint skill)
        {
            return LAUNCH_RESULT.OK == CanLaunch(skill, out _, out _);
        }

        /// <summary>
        /// 技能释放校验, 所有校验集中在此处, 便于 AI/UI 复用
        /// </summary>
        /// <param name="skill">技能 ID</param>
        /// <param name="skillinfo">技能运行时信息</param>
        /// <param name="skilldata">技能静态配置</param>
        /// <returns>校验结果, OK 表示可以释放</returns>
        public LAUNCH_RESULT CanLaunch(uint skill, out SkillInfo skillinfo, out SkillData skilldata)
        {
            skillinfo = null;
            skilldata = null;

            if (0 == skill) return LAUNCH_RESULT.INVALID;
            if (false == stage.cache.Valid(actor)) return LAUNCH_RESULT.INVALID;
            // 已经有技能在释放中, 不允许打断
            if (info.casting) return LAUNCH_RESULT.STATE;
            if (false == info.loadedskilldict.TryGetValue(skill, out var skillactor)) return LAUNCH_RESULT.UNLOADED;
            if (false == stage.SeekBehaviorInfo(skillactor, out skillinfo)) return LAUNCH_RESULT.UNLOADED;
            if (skillinfo.cooldown > FP.Zero) return LAUNCH_RESULT.COOLDOWN;
            if (false == stage.cfg.location.TbSkillData.TryGetValue((int)skill, out skilldata)) return LAUNCH_RESULT.INVALID;
            if (false == CheckCond(skilldata.Cond)) return LAUNCH_RESULT.COND;
            if (false == Cost(skilldata.Cost)) return LAUNCH_RESULT.COST;
            // 释放状态是否可进入
            if (stage.SeekBehavior(actor, out StateMachine machine))
            {
                if (false == machine.QueryPassState(STATE_DEFINE.CASTING)) return LAUNCH_RESULT.STATE;
            }

            return LAUNCH_RESULT.OK;
        }

        /// <summary>
        /// 校验释放条件
        /// </summary>
        /// <param name="cond">条件</param>
        /// <returns>YES/NO</returns>
        private bool CheckCond(SkillCond cond)
        {
            // 没有条件配置视为无条件
            if (null == cond) return true;

            if (cond is RoleResourceCond resourcecond) return CheckRoleResource(resourcecond.Fixed, resourcecond.Percent);

            return true;
        }

        /// <summary>
        /// 扣除释放消耗
        /// 注意: 此方法会被 CanLaunch 用于校验, 不具备扣减副作用
        /// </summary>
        /// <param name="cost">消耗</param>
        /// <returns>YES/NO</returns>
        private bool Cost(SkillCost cost)
        {
            // 没有消耗配置视为无消耗
            if (null == cost) return true;

            // TODO 属性系统 (Attribute) 尚未落地, 暂时只做校验不做扣减
            if (cost is RoleResourceCost resourcecost) return CheckRoleResource(resourcecost.Fixed, resourcecost.Percent);

            return true;
        }

        /// <summary>
        /// 校验资源条件
        /// </summary>
        /// <param name="fixedvalue">固定值</param>
        /// <param name="percent">百分比</param>
        /// <returns>YES/NO</returns>
        private bool CheckRoleResource(Dictionary<RoleAttr, int> fixedvalue, Dictionary<RoleAttr, int> percent)
        {
            // TODO 属性系统 (Attribute) 尚未落地, 暂时视为条件恒成立
            if (null == fixedvalue) return true;

            return true;
        }
    }
}
