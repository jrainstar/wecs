using GameConf.Gameplay;
using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// AOE 预制信息
    /// </summary>
    public class AoePrefabInfo : PrefabInfo
    {
        /// <summary>
        /// AOE 配置 ID
        /// </summary>
        public int aoe { get; set; }
        /// <summary>
        /// 施法者
        /// </summary>
        public ulong caster { get; set; }
        /// <summary>
        /// 轨道配置 ID
        /// </summary>
        public int track { get; set; }
        /// <summary>
        /// 速度配置 ID
        /// </summary>
        public int speed { get; set; }
        /// <summary>
        /// 出生点偏移 (相对施法者)
        /// </summary>
        public FPVector3 offset { get; set; }
        /// <summary>
        /// 发射延迟, 0 表示立即发射
        /// </summary>
        public FP delay { get; set; }
    }

    /// <summary>
    /// AOE 预制创建器
    /// AOE 自身即飞行体: 出生在轨道起点 (施法者位置 + 偏移, 偏移随施法者朝向旋转),
    /// 之后由 Aoe 行为按轨道与速度推进, 到点销毁
    /// </summary>
    public class AoePrefab : Prefab<AoePrefabInfo>
    {
        public override byte type => ACTOR_DEFINE.AOE;

        protected override void OnProcessing(ulong actor, AoePrefabInfo prefab)
        {
            if (null == stage.cfg
                || false == stage.cfg.location.TbAoeData.TryGetValue(prefab.aoe, out var data)
                || null == data)
            {
                // 配置缺失, 不留只有 Tag 的空 Actor
                stage.RmvActor(actor);
                return;
            }

            // 出生点与发射方向: 施法者位置 + 偏移, 偏移按施法者朝向转到世界方向
            var origin = prefab.offset;
            var forward = FPVector3.forward;
            var center = prefab.offset;

            if (stage.SeekBehaviorInfo(prefab.caster, out SpatialInfo casterspatial))
            {
                var yaw = casterspatial.euler.y * FPMath.Deg2Rad;
                var sin = FPMath.Sin(yaw);
                var cos = FPMath.Cos(yaw);

                forward = new FPVector3(sin, FP.Zero, cos);
                center = casterspatial.position;
                origin = casterspatial.position + new FPVector3(
                    prefab.offset.x * cos + prefab.offset.z * sin,
                    prefab.offset.y,
                    prefab.offset.z * cos - prefab.offset.x * sin);
            }

            var aoe = stage.AddBehavior<Aoe>(actor);
            var aoeinfo = aoe.info;

            aoeinfo.id = data.Id;
            aoeinfo.caster = prefab.caster;
            aoeinfo.track = prefab.track;
            aoeinfo.speed = prefab.speed;
            aoeinfo.delay = prefab.delay;
            // 存在时长同时是轨道总时长, 小于等于 0 表示不限制
            aoeinfo.duration = data.Duration;
            aoeinfo.origin = origin;
            aoeinfo.center = center;
            aoeinfo.forward = forward;
            aoeinfo.right = new FPVector3(forward.z, FP.Zero, -forward.x);
            aoeinfo.direction = forward;
            // 记录配置了哪些触发时机, Aoe 行为据此决定要不要做每帧重叠检测
            aoeinfo.triggers = Triggers(data);
            aoeinfo.target = 0;
            // 只有追踪轨道才需要提前锁定目标, 其它轨道不用做这次遍历
            if (stage.cfg.location.TbEmitterTrackData.TryGetValue(prefab.track, out var trackdata)
                && null != trackdata
                && EmitterTrackType.Homing == trackdata.Type)
            {
                aoeinfo.target = FindTarget(actor, prefab.caster, origin);
            }

            var spatial = stage.AddBehaviorInfo<SpatialInfo>(actor);
            spatial.position = origin;
            spatial.euler = FPVector3.up * (FPMath.Atan2(forward.x, forward.z) * FPMath.Rad2Deg);

            // 无延迟的 AOE 立即挂外观与碰撞盒; 有延迟的等延迟结束由 Aoe 行为挂上
            if (aoeinfo.delay <= FP.Zero) aoe.Release();
        }

        /// <summary>
        /// 计算配置了哪些触发时机 (AoeTrigger 位掩码)
        /// </summary>
        /// <param name="data">AOE 配置</param>
        /// <returns>触发时机位掩码</returns>
        private static ushort Triggers(AoeData data)
        {
            ushort triggers = 0;
            if (null == data || null == data.Events) return triggers;

            foreach (var e in data.Events)
            {
                if (null == e) continue;

                triggers |= (ushort)(1 << (int)e.Trigger);
            }

            return triggers;
        }

        /// <summary>
        /// 查找追踪目标
        /// 单机版本还没有阵营概念, 先取离出生点最近的英雄 (排除施法者自身), 没有目标时返回 0, AOE 沿直线飞行
        /// </summary>
        /// <param name="self">自身 Actor</param>
        /// <param name="caster">施法者</param>
        /// <param name="origin">出生点</param>
        /// <returns>目标 Actor, 没有目标返回 0</returns>
        private ulong FindTarget(ulong self, ulong caster, FPVector3 origin)
        {
            if (false == stage.SeekBehaviorInfos(out List<ColliderInfo> colliders)) return 0;

            ulong target = 0;
            var best = FP.MaxValue;

            foreach (var collider in colliders)
            {
                if (null == collider) continue;
                if (collider.actor == self || collider.actor == caster) continue;
                if (false == stage.SeekBehavior(collider.actor, out Tag tag)) continue;
                if (false == tag.Get(TAG_DEFINE.ACTOR_TYPE, out var type)) continue;
                if (type != ACTOR_DEFINE.HERO) continue;
                if (false == stage.SeekBehaviorInfo(collider.actor, out SpatialInfo spatial)) continue;

                var offset = spatial.position - origin;
                offset.y = FP.Zero;
                if (offset.sqrmagnitude >= best) continue;

                best = offset.sqrmagnitude;
                target = collider.actor;
            }

            return target;
        }
    }
}
