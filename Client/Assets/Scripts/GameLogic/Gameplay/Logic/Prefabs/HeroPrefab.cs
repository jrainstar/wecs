using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 英雄预制信息
    /// </summary>
    public class HeroPrefabInfo : PrefabInfo
    {
        /// <summary>
        /// 英雄 ID
        /// </summary>
        public int hero { get; set; }
        /// <summary>
        /// 空间信息
        /// </summary>
        public SpatialData spatial { get; set; }
    }

    /// <summary>
    /// 英雄预制创建器
    /// </summary>
    public class HeroPrefab : Prefab<HeroPrefabInfo>
    {
        public override byte type => ACTOR_DEFINE.HERO;

        protected override void OnProcessing(ulong actor, HeroPrefabInfo info)
        {
            // 角色配置, 属性与碰撞盒都从这里取; 后面还要用, 只查一次
            var hasrole = stage.cfg.location.TbRoleData.TryGetValue(info.hero, out var roledata);

            stage.AddBehavior<StateMachine>(actor);
            stage.AddBehavior<Movement>(actor);

            // 数值计算, 角色的属性都由它统一算
            // 表格里的数值直接作为基础值, 单位换算 (crit 是百分点, spd 是评分) 交给使用方解释;
            // 之后接 BUFF/装备时只需要往 AttributeCalc 上加修正, 这里不用再改
            var calc = stage.AddBehavior<AttributeCalc>(actor);
            if (hasrole)
            {
                // 当前生命与最大生命同源, 之后掉血只改 HP
                calc.SetBase(ATTRIBUTE_DEFINE.MAXHP, roledata.Hp);
                calc.SetBase(ATTRIBUTE_DEFINE.HP, roledata.Hp);
                calc.SetBase(ATTRIBUTE_DEFINE.ATTACK, roledata.Atk);
                calc.SetBase(ATTRIBUTE_DEFINE.DEFENSE, roledata.Def);
                calc.SetBase(ATTRIBUTE_DEFINE.MOVESPEED, roledata.Spd);
                calc.SetBase(ATTRIBUTE_DEFINE.CRIT, roledata.Crit);
                calc.SetBase(ATTRIBUTE_DEFINE.LUCK, roledata.Luck);
            }

            // 技能学习, 按英雄 ID 从 TbRoleSkill 读取该角色的技能列表并逐个载入
            var launcher = stage.AddBehavior<SkillLauncher>(actor);
            if (stage.cfg.location.TbRoleSkill.TryGetValue(info.hero, out var roleskill))
            {
                foreach (var skill in roleskill.Skills) launcher.Load((uint)skill);
            }

            var facade = stage.AddBehavior<Facade>(actor);
            // 模型 ID 必须大于 0, 否则渲染层 (ModelEnchant) 会认为没有模型而不创建 ModelAgent
            facade.SetModel(info.hero);

            stage.AddBehaviorInfo<TickerInfo>(actor);

            var spatial = stage.AddBehaviorInfo<SpatialInfo>(actor);
            spatial.position = info.spatial.position;
            spatial.euler = info.spatial.euler;
            spatial.scale = info.spatial.scale;

            // 碰撞盒, 按英雄的角色配置载入; 同时用于检测与移动推挤
            var collider = stage.AddBehaviorInfo<ColliderInfo>(actor);
            if (hasrole)
            {
                stage.detection.SetColliderInfo(collider, roledata.Collider);
            }

            // 角色可被推动; 负数表示无限大(不可推动), 墙体等静态物体在各自的 Prefab 里设 -1
            collider.mass = FP.One;

        }
    }
}