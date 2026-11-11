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