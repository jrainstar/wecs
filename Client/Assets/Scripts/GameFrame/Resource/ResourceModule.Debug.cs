using System.Collections.Generic;
using YooAsset;
using Object = UnityEngine.Object;
using SceneHandle = YooAsset.SceneHandle;

namespace GameEngine
{
    /// <summary>
    /// ResourceModule - 调试 / Inspector 支持分部
    /// 提供只读快照 API，供 Editor Inspector 与运行时调试面板使用。
    /// </summary>
    public partial class ResourceModule
    {
        /// <summary>已加载资源条目数</summary>
        public int AssetCount => m_AssetObjectDict.Count;

        /// <summary>已实例化对象数（含 GameObject / Component）</summary>
        public int InstanceCount => m_InstanceToLocation.Count;

        /// <summary>已登记的场景句柄数</summary>
        public int SceneCount => m_SceneHandles.Count;

        /// <summary>自动回收扫描间隔（秒）——只读快照</summary>
        public int AutoCheckUnloadInterval => m_AutoCheckUnloadTimeInterval;

        /// <summary>资源闲置多长时间会被自动回收（秒）——只读快照</summary>
        public int NoUseUnloadTime => m_NoUseUnloadTime;

        /// <summary>当前自动回收计时器累计秒数（0 ~ AutoCheckUnloadInterval）</summary>
        public float AutoCheckUnloadTimer => m_CheckUnloadTimer;

        /// <summary>距离下次自动扫描剩余的秒数（未开启自动回收则返回 -1）</summary>
        public float NextAutoUnloadCountdown
        {
            get
            {
                if (!OpenUnloadTimer) return -1f;
                var remain = m_AutoCheckUnloadTimeInterval - m_CheckUnloadTimer;
                return remain < 0f ? 0f : remain;
            }
        }

        /// <summary>所有资源引用计数之和</summary>
        public int TotalRefCount
        {
            get
            {
                int sum = 0;
                foreach (var kv in m_AssetObjectDict) sum += kv.Value.ReferenceCount;
                return sum;
            }
        }

        /// <summary>只读枚举所有资源条目（用于 Inspector 展示，禁止修改）</summary>
        public IEnumerable<AssetObject> EnumerateAssetObjects()
        {
            foreach (var kv in m_AssetObjectDict) yield return kv.Value;
        }

        /// <summary>只读枚举所有实例：Key=实例对象，Value=对应 location</summary>
        public IEnumerable<KeyValuePair<Object, string>> EnumerateInstances()
        {
            foreach (var kv in m_InstanceToLocation) yield return kv;
        }

        /// <summary>只读枚举所有场景句柄</summary>
        public IEnumerable<KeyValuePair<string, SceneHandle>> EnumerateScenes()
        {
            foreach (var kv in m_SceneHandles) yield return kv;
        }
    }
}
