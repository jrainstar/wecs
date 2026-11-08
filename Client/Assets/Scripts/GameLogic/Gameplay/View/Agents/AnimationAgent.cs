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
        private AnimancerComponentPro animancer { get; set; }

        /// <summary>
        /// Animancer 播放状态
        /// </summary>
        private AnimancerState animstate { get; set; }
        /// <summary>
        /// 动画是否循环
        /// </summary>
        private bool animloop { get; set; }

        // RIL
        // preplayname curplayname
        // 逻辑层想要的动画是什么、从什么切过来的

        /// <summary>
        /// 上一个动画名称
        /// </summary>
        private string preplayname { get; set; }
        /// <summary>
        /// 当前播放动画名称
        /// </summary>
        private string curplayname { get; set; }

        /// <summary>
        /// 动画速度倍率, 来自逻辑层 RIL (1 = 常速, 0 = 定格)
        /// </summary>
        private float animspeed { get; set; }

        /// <summary>
        /// 是否由逻辑层应用关键帧位移, 来自逻辑层 RIL
        /// 为 true 时锁住根骨骼, 动画自带的根骨骼位移不再作用到模型上,
        /// 位移完全由逻辑层采样关键帧驱动, 避免叠加两遍
        /// </summary>
        private bool animmotion { get; set; }

        /// <summary>
        /// 动画持续时间
        /// </summary>
        private float tarduration { get; set; }

        /// <summary>
        /// 混合持续时间
        /// </summary>
        private float mixduration { get; set; }

        // 播放层
        // playname lastplayname
        // 这一帧真正交给 Animancer 的是什么、上一帧播的是什么

        /// <summary>
        /// 动画名称
        /// </summary>
        private string playname { get; set; }

        /// <summary>
        /// 上一次播放的动画名称
        /// </summary>
        private string lastplayname { get; set; }

        /// <summary>
        /// 动画播放位置 (秒)
        /// 完全由脚本推进, 不依赖 Animancer 的内部时间, 切换动画时才重置
        /// </summary>
        private float playtime { get; set; }

        /// <summary>
        /// 前置混合动画已播放时间 (秒)
        /// 从逻辑层切换到目标动画开始计时, 只随逻辑层切换动画归零.
        /// 不能复用 playtime: 前置动画播完切到目标动画时 playtime 会归零,
        /// 判定条件 "还在前置动画时长内" 会被重新打开, 两段动画就会反复来回切
        /// </summary>
        private float mixtime { get; set; }

        /// <summary>
        /// 逻辑层这一帧决定要播的 playname 是否为前置混合动画 (一次性过渡片段)
        /// 与 playname 一起生效, 由 OnChase 取到 playbeforemix
        /// </summary>
        private bool convbeforemix { get; set; }

        /// <summary>
        /// 当前正在播放的 playname 是否为前置混合动画
        /// 过渡片段一律按一次性播放 (停在末帧), 不能跟随目标动画的循环标记取模,
        /// 否则片段播到末帧会被拽回第 0 帧, 视觉上就是从过渡动作瞬移到目标动作
        /// </summary>
        private bool playbeforemix { get; set; }

        /// <summary>
        /// 上一次 RIL 的逻辑层流逝时间 (毫秒)
        /// 用于检测逻辑时间倒退 (重播/回滚/循环回绕), 倒退时才重新对齐播放位置
        /// </summary>
        private uint lastelapsed { get; set; }

        /// <summary>
        /// 根骨骼锁定剩余时间 (秒)
        /// 从带位移的动画切到不带位移的动画时, 继续锁住根骨骼直到混合结束,
        /// 否则解锁瞬间上一段动画的根骨骼位移会显形 (向前窜一下再被拉回来)
        /// </summary>
        private float rootlockremaining { get; set; }

        /// <summary>
        /// 上一帧是否锁住根骨骼
        /// </summary>
        private bool rootlocked { get; set; }

        protected override void OnReady()
        {
            cfgname = null;
            animcfg = null;
            animancer = null;
            animstate = null;
            preplayname = null;
            curplayname = null;
            playname = null;
            animspeed = 1;
            animmotion = false;
            tarduration = 0;
            mixduration = 0;
            animloop = true;
            lastplayname = null;
            playtime = 0;
            mixtime = 0;
            convbeforemix = false;
            playbeforemix = false;
            lastelapsed = 0;
            rootlockremaining = 0;
            rootlocked = false;

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
            animspeed = 1;
            animmotion = false;
            tarduration = 0;
            mixduration = 0;
            animloop = true;
            lastplayname = null;
            playtime = 0;
            mixtime = 0;
            convbeforemix = false;
            playbeforemix = false;
            lastelapsed = 0;
            rootlockremaining = 0;
            rootlocked = false;
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

            var switched = curplayname != animname;
            if (switched)
            {
                preplayname = curplayname;
                curplayname = animname;
            }

            playname = curplayname;
            convbeforemix = false;
            animloop = ril.animloop;
            animmotion = ril.animmotion;
            // 先取配置的默认混合时间, 动画自己配置了 (大于 0) 再覆盖
            mixduration = animcfg.defaultfadeduration;

            var animinfo = animcfg.GetAnimationMixInfo(animname);
            if (null != animinfo && animinfo.mixduration > 0) mixduration = animinfo.mixduration;

            animspeed = ril.animspeed * ConfigKit.Int2Float;

            // 播放位置只在以下情况对齐逻辑层的 animelapsed:
            // 1. 动画切换 (SetAnimation 会把 animelapsed 归零)
            // 2. 逻辑时间倒退 (重播/快照回滚/循环回绕, 回绕后 ApplyTime 取模等价连续)
            // 其余情况渲染层领先逻辑层不足一帧, 每次都对齐会被拽回造成抖动,
            // 渲染帧之间由 OnChase 按 tick * timescale * animspeed 自主插值推进
            if (switched || ril.animelapsed < lastelapsed)
            {
                playtime = ril.animelapsed * ConfigKit.Int2Float;
                // 前置混合动画从这里开始计时, 之后只由 OnChase 推进, 不随播放位置归零
                mixtime = playtime;
            }
            lastelapsed = ril.animelapsed;

            if (null == preplayname || null == animinfo) return;
            var beforeAnimInfo = animinfo.GetAnimationBeforeMixInfo(preplayname);
            if (null != beforeAnimInfo && mixtime < beforeAnimInfo.duration)
            {
                playname = beforeAnimInfo.name;
                convbeforemix = true;
                // 前置混合动画同样没配置时回落到默认混合时间
                mixduration = beforeAnimInfo.mixduration > 0 ? beforeAnimInfo.mixduration : animcfg.defaultfadeduration;
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
                animancer = model.go.GetComponent<AnimancerComponentPro>();
            }

            if (null == animancer) return;
            if (string.IsNullOrEmpty(playname)) return;

            // 混合时间随 timescale 缩放, timescale 为 0 时按 1 处理, 避免除零
            var scale = timescale > 0 ? timescale : 1f;
            var fade = mixduration * (1 / scale);

            // 动画切换时重置播放位置, 同一段动画继续往下播
            if (playname != lastplayname)
            {
                lastplayname = playname;
                playbeforemix = convbeforemix;
                playtime = 0;
                // 从带位移的动画切到不带位移的动画时, 继续锁到混合结束:
                // 此时上一段动画还在淡出, 根骨骼还带着片段末尾的位移, 立刻解锁会看见
                // 角色向前窜一下再被混合拉回绑定姿态!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                rootlockremaining = (false == animmotion && rootlocked) ? fade : 0f;
            }
            else if (rootlockremaining > 0f)
            {
                rootlockremaining -= tick;
                if (rootlockremaining < 0f) rootlockremaining = 0f;
            }

            // lockRoot: 带位移的动画一直锁; 不带位移时只在离开位移动画的混合期间继续锁
            var lockroot = animmotion || rootlockremaining > 0f;
            animstate = animancer.PlayNamed(playname, fade, lockroot);
            rootlocked = lockroot;
            if (null == animstate) return;

            // 关掉 Animancer 的自动推进, 全部由脚本控制时间
            animstate.Speed = 0;

            // 推进播放位置; 不能用状态时长以外的值作上限, 否则动画会静止
            // 倍率来自逻辑层 (如时间轴节点的 Speed), 定格 (0) 时渲染层同样不推进
            playtime += tick * timescale * animspeed;
            mixtime += tick * timescale * animspeed;
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

            // 前置混合动画是过渡片段, 一律按一次性播放:
            // animloop 取自目标动画 (如待机 = 循环), 若跟着取模, 片段播到末帧只要还差一个
            // 逻辑帧没切换到目标, 播放位置就会被拽回第 0 帧, 看到的是一次瞬移而不是混合
            var loop = animloop && false == playbeforemix;
            var time = loop ? Mathf.Repeat(playtime, duration) : Mathf.Clamp(playtime, 0, duration);
            animstate.Time = time;
        }
    }
}