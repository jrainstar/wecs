using GameConf.Gameplay;
using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 发射指令执行器
    /// 在时间轴节点上按配置创建一个飞行 AOE: 出生在轨道起点, 之后完全由 Aoe 行为
    /// 按轨道与速度推进, 到点销毁; 执行器是无状态的单例, 不保存任何发射中的 AOE
    /// </summary>
    public class EmitterExecutor : Executor<EmitterEvent>
    {
        public override void HandleEvent(EmitterEvent timelineevent)
        {
            if (null == timelineevent) return;
            if (null == stage.cfg) return;

            // 执行器是单例被所有时间轴共享, 宿主/施法者通过 Timeline.current 获取
            var timeline = stage.timeline;
            if (null == timeline || null == timeline.current) return;

            var tables = stage.cfg.location;
            if (false == tables.TbAoeData.TryGetValue(timelineevent.Aoe, out var aoe) || null == aoe)
            {
                UnityEngine.Debug.LogWarning($"EmitterEvent: aoe {timelineevent.Aoe} is not exist.");
                return;
            }

            if (false == tables.TbEmitterTrackData.TryGetValue(timelineevent.Track, out var track) || null == track)
            {
                UnityEngine.Debug.LogWarning($"EmitterEvent: emitter track {timelineevent.Track} is not exist.");
                return;
            }

            if (false == tables.TbEmitterSpeedData.TryGetValue(timelineevent.Speed, out var speed) || null == speed)
            {
                UnityEngine.Debug.LogWarning($"EmitterEvent: emitter speed {timelineevent.Speed} is not exist.");
                return;
            }

            // 施法者优先取时间轴上记录的 caster, 没有 (非技能时间轴) 则用时间轴宿主
            var caster = timeline.current.caster;
            if (0 == caster || false == stage.cache.Valid(caster)) caster = timeline.current.actor;

            stage.Spawn(new AoePrefabInfo
            {
                aoe = aoe.Id,
                caster = caster,
                track = track.Id,
                speed = speed.Id,
                offset = Offset(timelineevent.Offset),
                delay = timelineevent.Delay,
            });
        }

        /// <summary>
        /// 出生点偏移 x|y|z, 缺省为零
        /// </summary>
        /// <param name="offset">配置的偏移</param>
        /// <returns>偏移</returns>
        private static FPVector3 Offset(float[] offset)
        {
            if (null == offset || offset.Length < 3) return FPVector3.zero;

            return new FPVector3(offset[0], offset[1], offset[2]);
        }
    }
}
