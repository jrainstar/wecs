using Animancer;
using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;
using UnityEngine;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 动画代理
    /// </summary>
    public class AnimationAgent : Agent
    {
        /// <summary>
        /// 动画配置名称
        /// </summary>
        private string cfgname { get; set; } = default;
        /// <summary>
        /// 动画配置
        /// </summary>
        private AnimationConfig animcfg { get; set; } = default;
        /// <summary>
        /// Animancer 组件
        /// </summary>
        private AnimancerComponent animancer { get; set; }
        /// <summary>
        /// Animancer 播放状态
        /// </summary>
        private AnimancerState animstate { get; set; }
        /// <summary>
        /// 上一个动画名称
        /// </summary>
        private string preplayname { get; set; }
        /// <summary>
        /// 当前播放动画名称
        /// </summary>
        private string curplayname { get; set; }
        /// <summary>
        /// 动画名称
        /// </summary>
        private string playname { get; set; }
        /// <summary>
        /// 动画持续时间
        /// </summary>
        private float tarduration { get; set; }
        /// <summary>
        /// 混合持续时间
        /// </summary>
        private float mixduration { get; set; }
        /// <summary>
        /// 动画是否循环
        /// </summary>
        private bool animloop { get; set; } = true;
        /// <summary>
        /// 上一次播放的动画名称
        /// </summary>
        private string lastplayname { get; set; }
        /// <summary>
        /// 动画播放位置 (秒)
        /// 完全由脚本推进, 不依赖 Animancer 的内部时间, 切换动画时才重置
        /// </summary>
        private float playtime { get; set; }

        protected override void OnReady()
        {
            cfgname = null;
            animcfg = null;
            animancer = null;
            animstate = null;
            preplayname = null;
            curplayname = null;
            playname = null;
            tarduration = 0;
            mixduration = 0;
            animloop = true;
            lastplayname = null;
            playtime = 0;

            WatchRIL<RIL_FACADE_ANIMATION>(OnRILStateMachine);
        }

        protected override void OnReset()
        {
            cfgname = null;
            animcfg = null;
            animancer = null;
            animstate = null;
            preplayname = null;
            curplayname = null;
            playname = null;
            tarduration = 0;
            mixduration = 0;
            animloop = true;
            lastplayname = null;
            playtime = 0;
        }

        private void OnRILStateMachine(RIL_FACADE_ANIMATION ril)
        {
            RILConv2AnimData(ril);
        }

        /// <summary>
        /// 状态机状态转换为动画数据
        /// </summary>
        /// <param name="ril">状态机状态</param>
        private void RILConv2AnimData(RIL_FACADE_ANIMATION ril)
        {
            if (false == world.rilbucket.SeekRIL(ril.actor, out RIL_FACADE_MODEL facademodel)) return;
            if (0 >= facademodel.model) return;

            if (false == world.engine.config.location.TbRoleData.TryGetValue(facademodel.model, out var roledata)) return;
            if (false == world.engine.config.location.TbRoleModel.TryGetValue(roledata.ModelId, out var rolemodel)) return;

            if (string.IsNullOrEmpty(cfgname) || false == rolemodel.Animation.Equals(cfgname))
            {
                cfgname = rolemodel.Animation;
                if (null != animcfg) GameObject.Destroy(animcfg);
                animcfg = world.engine.gameres.location.LoadAnimationConfigSync(cfgname);
            }

            var animname = ril.animname;
            if (null == animname) animname = animcfg.GetAnimationName(ril.animstate);

            if (curplayname != animname)
            {
                preplayname = curplayname;
                curplayname = animname;
            }

            playname = curplayname;
            animloop = ril.animloop;
            mixduration = 0;

            var animinfo = animcfg.GetAnimationMixInfo(animname);
            if (null != animinfo) mixduration = animinfo.mixduration;

            // 时间轴手动推进的动画, 用逻辑层给出的流逝时间对齐播放位置
            // 自动推进的动画由 OnChase 自行累加, 不理会这个值
            if (ANIM_DEFINE.TICK_MANUAL == ril.animticktype)
            {
                playtime = ril.animelapsed * ConfigKit.Int2Float;
            }

            if (null == preplayname || null == animinfo) return;
            var beforeAnimInfo = animinfo.GetAnimationBeforeMixInfo(preplayname);
            if (null != beforeAnimInfo && playtime < beforeAnimInfo.duration)
            {
                playname = beforeAnimInfo.name;
                mixduration = beforeAnimInfo.mixduration;
            }
        }

        protected override void OnFlash()
        {
            base.OnFlash();

            if (null == playname) return;
            ApplyTime();
        }

        protected override void OnChase(float tick, float timescale)
        {
            base.OnChase(tick, timescale);
            if (null == animancer)
            {
                var model = world.GetAgent<ModelAgent>(actor);
                if (null == model || null == model.go) return;
                animancer = model.go.GetComponent<AnimancerComponent>();
            }

            if (null == animancer) return;
            if (string.IsNullOrEmpty(playname)) return;

            // 混合时间随 timescale 缩放, timescale 为 0 时按 1 处理, 避免除零
            var scale = timescale > 0 ? timescale : 1f;
            animstate = animancer.TryPlay(playname, mixduration * (1 / scale));
            if (null == animstate) return;

            // 动画切换时重置播放位置, 同一段动画继续往下播
            if (playname != lastplayname)
            {
                lastplayname = playname;
                playtime = 0;
            }

            // 关掉 Animancer 的自动推进, 全部由脚本控制时间
            animstate.Speed = 0;

            // 推进播放位置; 不能用状态时长以外的值作上限, 否则动画会静止
            playtime += tick * timescale;
            ApplyTime();
        }

        /// <summary>
        /// 把 playtime 应用到 Animancer 状态上
        /// 循环动画取模, 非循环动画停在末帧
        /// </summary>
        private void ApplyTime()
        {
            if (null == animstate) return;

            // 优先用 Animancer 的真实时长, 拿不到时回落到配置时长
            var duration = animstate.Length > 0 ? animstate.Length : tarduration;
            if (duration <= 0) return;

            var time = animloop ? Mathf.Repeat(playtime, duration) : Mathf.Clamp(playtime, 0, duration);
            animstate.Time = time;
        }
    }
}