using GameConf.Gameplay;
using GameLogic.Gameplay.Common;
using System.Collections.Generic;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// AOE 行为
    /// AOE 自身即飞行体: 创建在轨道起点 (施法者位置 + 偏移), 按轨道 (EmitterTrackData) 与
    /// 速度 (EmitterSpeedData) 推进, 存在时长 (AoeData.Duration) 到点即销毁, 也就是轨道终点
    /// 全程使用定点数, 保证确定性与快照回滚
    /// </summary>
    public class Aoe : Behavior<AoeInfo>
    {
        /// <summary>
        /// 释放: 挂上外观与碰撞盒
        /// 有发射延迟的 AOE 在延迟结束时才调用, 延迟期间只有空间信息,
        /// 既不出现在画面里, 也不会被其它单位检测到
        /// </summary>
        public void Release()
        {
            var aoe = info;
            if (null == aoe) return;
            if (aoe.released) return;
            aoe.released = true;

            if (false == Config(out var data)) return;

            // 外观: AOE 的资源不走角色模型表, 直接把资源编号交给渲染层的模型代理,
            // 由它按 TbResourceData 解析路径加载
            if (data.Model > 0)
            {
                stage.AddBehavior<Facade>(actor).SetResource(data.Model);
            }

            // 碰撞盒: 只用于 AOE 自身的重叠检测 (触发 进入/离开/周期),
            // 独立的碰撞层 + 不可推动, 因此不会推挤角色, 也不会挡住角色
            if (data.Collider > 0)
            {
                var collider = stage.AddBehaviorInfo<ColliderInfo>(actor);
                stage.detection.SetColliderInfo(collider, data.Collider);
                collider.layer = COLLISION_DEFINE.LAYER_AOE;
                collider.mass = -FP.One;
            }

            // 创建触发: 出生时对范围内的单位结算一次
            Trigger(aoe, data, AoeTrigger.Create);
        }

        protected override void OnTick(FP tick)
        {
            base.OnTick(tick);

            if (false == Config(out var data))
            {
                // 配置不存在 (漏配/配置热更), 直接销毁, 不留下空转的 Actor
                stage.RmvActor(actor);
                return;
            }

            var aoe = info;
            if (null == aoe) return;

            if (false == stage.SeekBehaviorInfo(actor, out SpatialInfo spatial)) return;

            // 发射延迟: 未到点前不推进, 到点后挂上外观/碰撞, 并把本帧剩余的步长继续用掉
            if (aoe.delay > FP.Zero)
            {
                aoe.delay -= tick;
                if (aoe.delay > FP.Zero) return;

                tick = -aoe.delay;
                aoe.delay = FP.Zero;
                Release();
            }

            // 速度 = 基础速度 * 缓动倍率, 倍率按进度 (elapsed / duration) 采样速度曲线
            var progress = aoe.duration > FP.Zero ? FPMath.Clamp01(aoe.elapsed / aoe.duration) : FP.One;
            var step = EMITTER_DEFINE.SPEED * SpeedFactor(aoe.speed, progress) * tick;

            aoe.distance += step;
            aoe.elapsed += tick;

            QueryTrack(aoe.track, out var track);
            Apply(aoe, track, spatial, step);

            // 先移动再结算, 保证触发用的是本帧位置
            Triggers(aoe, data, tick);

            // 到达存在时长即销毁, 也就是轨道终点
            if (aoe.duration > FP.Zero && aoe.elapsed >= aoe.duration)
            {
                // 销毁触发: AOE 消失前最后结算一次
                Trigger(aoe, data, AoeTrigger.Destroy);
                stage.RmvActor(actor);
            }
        }

        /// <summary>
        /// 计算本帧位置与朝向
        /// </summary>
        /// <param name="aoe">AOE 信息</param>
        /// <param name="track">轨道配置, 可以为空 (按直线处理)</param>
        /// <param name="spatial">空间信息</param>
        /// <param name="step">本帧位移距离</param>
        private void Apply(AoeInfo aoe, EmitterTrackData track, SpatialInfo spatial, FP step)
        {
            var prev = spatial.position;

            switch (null == track ? EmitterTrackType.Line : track.Type)
            {
                case EmitterTrackType.Homing:
                    Homing(aoe, spatial);
                    spatial.position = prev + aoe.direction * step;
                    break;
                case EmitterTrackType.Orbit:
                    ApplyOrbit(aoe, track, spatial);
                    break;
                case EmitterTrackType.Boomerang:
                    ApplyBoomerang(aoe, track, spatial);
                    break;
                default:
                    // 直线: 从出生点沿发射方向按路程推进
                    spatial.position = aoe.origin + aoe.forward * aoe.distance;
                    break;
            }

            // 朝向 = 本帧的位移方向, 没有位移时保持上一帧的朝向
            var motion = spatial.position - prev;
            motion.y = FP.Zero;
            if (motion.sqrmagnitude > FPMath.Epsilon) aoe.direction = motion.normalized;

            spatial.euler = FPVector3.up * (FPMath.Atan2(aoe.direction.x, aoe.direction.z) * FPMath.Rad2Deg);
        }

        /// <summary>
        /// 结算需要持续跟踪的触发: 进入 / 离开 / 周期
        /// 每帧只做一次重叠检测, 三种触发共用同一份结果
        /// </summary>
        /// <param name="aoe">AOE 信息</param>
        /// <param name="data">AOE 配置</param>
        /// <param name="tick">步长</param>
        private void Triggers(AoeInfo aoe, AoeData data, FP tick)
        {
            var enter = Has(aoe, AoeTrigger.Enter);
            var leave = Has(aoe, AoeTrigger.Leave);
            var period = Has(aoe, AoeTrigger.Tick);
            // 只配了创建/销毁的 AOE 每帧不做检测, 纯表现 AOE 完全零开销
            if (false == enter && false == leave && false == period) return;

            List<ulong> currents = Collect(aoe);
            if (null == currents) return;

            if (enter)
            {
                foreach (var target in currents)
                {
                    if (aoe.overlaps.Contains(target)) continue;

                    Fire(aoe, data, AoeTrigger.Enter, target);
                }
            }

            if (leave)
            {
                foreach (var target in aoe.overlaps)
                {
                    if (currents.Contains(target)) continue;

                    Fire(aoe, data, AoeTrigger.Leave, target);
                }
            }

            if (period) Period(aoe, data, tick, currents);

            // 记录本帧重叠, 供下一帧比较
            aoe.overlaps.Clear();
            foreach (var target in currents) aoe.overlaps.Add(target);

            currents.Clear();
            ObjectCache.Set(currents);
        }

        /// <summary>
        /// 周期触发
        /// 到点只补一次, 卡帧时不连续爆发多段
        /// </summary>
        /// <param name="aoe">AOE 信息</param>
        /// <param name="data">AOE 配置</param>
        /// <param name="tick">步长</param>
        /// <param name="targets">当前重叠目标</param>
        private void Period(AoeInfo aoe, AoeData data, FP tick, List<ulong> targets)
        {
            var period = (FP)data.TickTime;
            if (period < FP.Zero) period = FP.Zero;

            aoe.tickelapsed += tick;
            if (aoe.tickelapsed < period) return;

            aoe.tickelapsed -= period;
            if (aoe.tickelapsed < FP.Zero) aoe.tickelapsed = FP.Zero;

            foreach (var target in targets) Fire(aoe, data, AoeTrigger.Tick, target);
        }

        /// <summary>
        /// 一次性触发: 立即结算当前范围内的所有目标
        /// </summary>
        /// <param name="aoe">AOE 信息</param>
        /// <param name="data">AOE 配置</param>
        /// <param name="trigger">触发时机</param>
        private void Trigger(AoeInfo aoe, AoeData data, AoeTrigger trigger)
        {
            if (false == Has(aoe, trigger)) return;

            List<ulong> targets = Collect(aoe);
            if (null == targets)
            {
                // 没有碰撞盒就没有作用范围, 只派发一次无目标负载, 让表现类效果照常执行
                Fire(aoe, data, trigger, 0);
                return;
            }

            foreach (var target in targets) Fire(aoe, data, trigger, target);

            targets.Clear();
            ObjectCache.Set(targets);
        }

        /// <summary>
        /// 收集当前与 AOE 重叠的目标
        /// </summary>
        /// <param name="aoe">AOE 信息</param>
        /// <returns>目标集合, 用完必须清空并归还对象池; 没有碰撞盒时返回 null</returns>
        private List<ulong> Collect(AoeInfo aoe)
        {
            if (false == stage.SeekBehaviorInfo(actor, out ColliderInfo collider)) return null;

            var targets = ObjectCache.Ensure<List<ulong>>();

            var hit = stage.detection.Overlap(collider);
            if (false == hit.hit || null == hit.colliders) return targets;

            foreach (var c in hit.colliders)
            {
                if (Skip(aoe, c.actor)) continue;

                targets.Add(c.actor);
            }

            return targets;
        }

        /// <summary>
        /// 是否跳过该目标: 自身与施法者不参与结算
        /// </summary>
        /// <param name="aoe">AOE 信息</param>
        /// <param name="target">目标</param>
        /// <returns>YES/NO</returns>
        private bool Skip(AoeInfo aoe, ulong target)
        {
            if (0 == target) return true;
            if (target == actor) return true;
            if (0 != aoe.caster && target == aoe.caster) return true;

            return false;
        }

        /// <summary>
        /// 派发指定触发时机的负载
        /// </summary>
        /// <param name="aoe">AOE 信息</param>
        /// <param name="data">AOE 配置</param>
        /// <param name="trigger">触发时机</param>
        /// <param name="target">目标, 0 表示没有目标</param>
        private void Fire(AoeInfo aoe, AoeData data, AoeTrigger trigger, ulong target)
        {
            if (null == data.Events) return;

            foreach (var e in data.Events)
            {
                if (null == e) continue;
                if (e.Trigger != trigger) continue;
                if (0 >= e.PayloadId) continue;

                stage.payload.Play(e.PayloadId, aoe.caster, target);
            }
        }

        /// <summary>
        /// 是否配置了指定触发时机
        /// </summary>
        /// <param name="aoe">AOE 信息</param>
        /// <param name="trigger">触发时机</param>
        /// <returns>YES/NO</returns>
        private static bool Has(AoeInfo aoe, AoeTrigger trigger)
        {
            if (null == aoe) return false;

            return 0 != (aoe.triggers & (1 << (int)trigger));
        }

        /// <summary>
        /// 追踪轨道: 每帧朝目标修正方向, 目标消失 (死亡/销毁) 后沿最后一次的方向继续飞
        /// </summary>
        /// <param name="aoe">AOE 信息</param>
        /// <param name="spatial">空间信息</param>
        private void Homing(AoeInfo aoe, SpatialInfo spatial)
        {
            if (0 == aoe.target) return;
            if (false == stage.cache.Valid(aoe.target)) return;
            if (false == stage.SeekBehaviorInfo(aoe.target, out SpatialInfo target)) return;

            var dire = target.position - spatial.position;
            dire.y = FP.Zero;
            if (dire.sqrmagnitude <= FPMath.Epsilon) return;

            aoe.direction = dire.normalized;
        }

        /// <summary>
        /// 环绕轨道: 绕着施法者转圈, 参数 [半径|初始角度(度)]
        /// </summary>
        /// <param name="aoe">AOE 信息</param>
        /// <param name="track">轨道配置</param>
        /// <param name="spatial">空间信息</param>
        private void ApplyOrbit(AoeInfo aoe, EmitterTrackData track, SpatialInfo spatial)
        {
            var radius = Value(track, 0, FP.Zero);
            if (radius <= FP.Zero)
            {
                spatial.position = aoe.center;
                return;
            }

            // 按路程换算角度: 角度 = 路程 / 半径, 初始角度由配置给出
            var angle = Value(track, 1, FP.Zero) + aoe.distance / radius * FPMath.Rad2Deg;
            var radian = angle * FPMath.Deg2Rad;

            spatial.position = aoe.center + new FPVector3(FPMath.Sin(radian), FP.Zero, FPMath.Cos(radian)) * radius;
        }

        /// <summary>
        /// 回旋镖轨道: 飞向最远点再飞回出生点, 参数 [到最远点距离|单侧弧半径]
        /// 弧半径取 射程/2 时轨迹为正圆环, 为 0 时退化成往返直线
        /// </summary>
        /// <param name="aoe">AOE 信息</param>
        /// <param name="track">轨道配置</param>
        /// <param name="spatial">空间信息</param>
        private void ApplyBoomerang(AoeInfo aoe, EmitterTrackData track, SpatialInfo spatial)
        {
            var max = Value(track, 0, FP.Zero);
            if (max <= FP.Zero)
            {
                spatial.position = aoe.origin;
                return;
            }

            // 路程在 [0, 2 * 最远点距离] 上循环: 前半程飞出, 后半程飞回
            var phase = aoe.distance % (max * 2);
            var radial = phase <= max ? phase : max * 2 - phase;

            // 单侧弧: 横向偏移随前进距离鼓出, 到最远点归零, 因此去程与回程的弧线是对称的
            var arc = Value(track, 1, FP.Zero);
            var lateral = arc > FP.Zero ? arc * FPMath.Sin(FPMath.Pi * radial / max) : FP.Zero;

            spatial.position = aoe.origin + aoe.forward * radial + aoe.right * lateral;
        }

        /// <summary>
        /// 速度曲线采样
        /// speed_data 按 进度|倍率 成对配置, 进度需递增; 未配置时按匀速处理
        /// </summary>
        /// <param name="id">速度配置 ID</param>
        /// <param name="progress">进度 (0~1)</param>
        /// <returns>速度倍率</returns>
        private FP SpeedFactor(int id, FP progress)
        {
            if (0 >= id) return FP.One;
            if (null == stage.cfg) return FP.One;
            if (false == stage.cfg.location.TbEmitterSpeedData.TryGetValue(id, out var speed)) return FP.One;
            if (null == speed || null == speed.SpeedData || speed.SpeedData.Length < 2) return FP.One;

            var data = speed.SpeedData;
            var prevprogress = FP.Zero;
            var prevfactor = (FP)data[1];

            for (int i = 0; i + 1 < data.Length; i += 2)
            {
                FP p = data[i];
                FP v = data[i + 1];
                if (progress <= p)
                {
                    if (p <= prevprogress) return v;
                    return FPMath.Lerp(prevfactor, v, (progress - prevprogress) / (p - prevprogress));
                }

                prevprogress = p;
                prevfactor = v;
            }

            return prevfactor;
        }

        /// <summary>
        /// 读取轨道配置
        /// </summary>
        /// <param name="id">轨道配置 ID</param>
        /// <param name="track">轨道配置</param>
        /// <returns>YES/NO</returns>
        private bool QueryTrack(int id, out EmitterTrackData track)
        {
            track = null;
            if (0 >= id) return false;
            if (null == stage.cfg) return false;

            return stage.cfg.location.TbEmitterTrackData.TryGetValue(id, out track) && null != track;
        }

        /// <summary>
        /// 读取轨道参数
        /// </summary>
        /// <param name="track">轨道配置</param>
        /// <param name="index">参数下标</param>
        /// <param name="defaultvalue">缺省值</param>
        /// <returns>参数值</returns>
        private static FP Value(EmitterTrackData track, int index, FP defaultvalue)
        {
            if (null == track || null == track.TrackData) return defaultvalue;
            if (0 > index || index >= track.TrackData.Length) return defaultvalue;

            return track.TrackData[index];
        }

        /// <summary>
        /// 读取 AOE 配置
        /// </summary>
        /// <param name="data">AOE 配置</param>
        /// <returns>YES/NO</returns>
        private bool Config(out AoeData data)
        {
            data = null;

            var aoe = info;
            if (null == aoe) return false;
            if (null == stage.cfg) return false;

            return stage.cfg.location.TbAoeData.TryGetValue(aoe.id, out data) && null != data;
        }
    }
}
