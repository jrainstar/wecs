using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 动画配置缓存
    /// </summary>
    public class EffectController : MonoBehaviour
    {
        /// <summary>
        /// 粒子系统集合
        /// </summary>
        [SerializeField]
        private ParticleSystem[] pss;

        /// <summary>
        /// 当前时间
        /// </summary>
        public float time { get; private set; }

        /// <summary>
        /// 重置特效控制器
        /// </summary>
        public void Reset()
        {
            time = 0;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 一键绑定子物体下所有粒子系统到 pss（编辑器用）
        /// </summary>
        [Button("绑定子物体粒子", ButtonSizes.Medium)]
        [ContextMenu("绑定子物体粒子")]
        private void BindChildParticles()
        {
            Undo.RecordObject(this, "绑定子物体粒子");
            pss = GetComponentsInChildren<ParticleSystem>(true);
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// 预览帧率, 滑动条每一格代表一帧
        /// </summary>
        [ShowInInspector]
        [BoxGroup("逐帧预览")]
        [LabelText("预览帧率")]
        [PropertyRange(1, 120)]
        [OnValueChanged("Preview")]
        private int previewfps = 30;

        /// <summary>
        /// 预览帧号, 拖动滑动条按绝对时间采样, 停在哪一帧就显示哪一帧
        /// </summary>
        [ShowInInspector]
        [BoxGroup("逐帧预览")]
        [LabelText("预览帧")]
        [PropertyRange(0, "PreviewMaxFrame")]
        [OnValueChanged("Preview")]
        private int previewframe;

        /// <summary>
        /// 当前预览时间 (秒)
        /// </summary>
        [ShowInInspector]
        [BoxGroup("逐帧预览")]
        [LabelText("预览时间")]
        [SuffixLabel("秒", true)]
        [ReadOnly]
        private float previewtime;

        /// <summary>
        /// 预览帧号上限, 取所有粒子系统里覆盖时间最长的一个
        /// </summary>
        private int PreviewMaxFrame
        {
            get
            {
                var duration = 1f;
                if (null != pss)
                {
                    foreach (var ps in pss)
                    {
                        if (null == ps) continue;
                        var main = ps.main;
                        // 系统时长 + 粒子最长存活时间, 循环系统再加一轮, 保证能看完一次完整循环
                        var max = main.duration + main.startLifetime.constantMax;
                        if (main.loop) max += main.duration;
                        if (max > duration) duration = max;
                    }
                }

                return Mathf.CeilToInt(duration * Mathf.Max(1, previewfps));
            }
        }

        /// <summary>
        /// 上一帧
        /// </summary>
        [BoxGroup("逐帧预览")]
        [HorizontalGroup("逐帧预览/操作")]
        [Button("上一帧", ButtonSizes.Small)]
        private void PreviewPrevFrame()
        {
            previewframe = Mathf.Max(0, previewframe - 1);
            Preview();
        }

        /// <summary>
        /// 下一帧
        /// </summary>
        [BoxGroup("逐帧预览")]
        [HorizontalGroup("逐帧预览/操作")]
        [Button("下一帧", ButtonSizes.Small)]
        private void PreviewNextFrame()
        {
            previewframe = Mathf.Min(PreviewMaxFrame, previewframe + 1);
            Preview();
        }

        /// <summary>
        /// 回到第一帧
        /// </summary>
        [BoxGroup("逐帧预览")]
        [HorizontalGroup("逐帧预览/操作")]
        [Button("回到起点", ButtonSizes.Small)]
        private void PreviewReplay()
        {
            previewframe = 0;
            Preview();
        }

        /// <summary>
        /// 清空粒子并回到第一帧
        /// </summary>
        [BoxGroup("逐帧预览")]
        [HorizontalGroup("逐帧预览/操作")]
        [Button("清空", ButtonSizes.Small)]
        private void PreviewClear()
        {
            EnsureParticles();
            previewframe = 0;
            previewtime = 0f;

            if (null != pss)
            {
                foreach (var ps in pss) if (null != ps) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            SceneView.RepaintAll();
        }

        /// <summary>
        /// 按当前帧号采样一次特效, 拖动滑动条时逐帧查看
        /// </summary>
        private void Preview()
        {
            EnsureParticles();
            previewfps = Mathf.Max(1, previewfps);
            previewframe = Mathf.Clamp(previewframe, 0, PreviewMaxFrame);
            previewtime = previewframe / (float)previewfps;

            // 先停掉自动播放, 再按绝对时间快进, 拖动滑动条才能稳定停在某一帧
            if (null != pss)
            {
                foreach (var ps in pss)
                {
                    if (null == ps || false == ps.gameObject.activeInHierarchy) continue;
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            Simulate(previewtime);
            SceneView.RepaintAll();
        }
#endif

        /// <summary>
        /// 运行时兜底绑定子物体下所有粒子系统
        /// 预制体上没有绑定粒子时由 EffectAgent 调用, 保证 Simulate 能采样到
        /// </summary>
        public void EnsureParticles()
        {
            if (null == pss || 0 == pss.Length) pss = GetComponentsInChildren<ParticleSystem>(true);
        }

        /// <summary>
        /// 特效采样
        /// </summary>
        /// <param name="time">时间</param>
        public void Simulate(float time)
        {
            this.time = time;
            if (null == pss) return;
            // 粒子系统采样
            foreach (var ps in pss) if (ps.gameObject.activeInHierarchy) ps.Simulate(time, true, true);
        }
    }

}