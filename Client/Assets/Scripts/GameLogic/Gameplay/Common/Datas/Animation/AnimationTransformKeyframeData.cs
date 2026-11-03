using GameFrame;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Gameplay.Common
{
    /// <summary>
    /// AnimationClip 中节点（Transform）的「位置 + 欧拉角」关键帧 JSON 数据结构。
    /// 位置为以动画根原点为基准的世界坐标；欧拉角为相对父节点的局部角度。
    /// 与编辑器工具 AnimationTransformKeyframeExporter 的输出一一对应，可直接反序列化：
    /// <code>
    /// AnimationKeyframeClipData data = AnimationKeyframeClipData.FromJson(jsonText);
    /// AnimationKeyframeNodeData node = data.FindNode("Root/Hips/Spine");
    /// Vector3 pos   = node.SamplePosition(time);
    /// Vector3 euler = node.SampleEuler(time);
    /// </code>
    /// 字段名与 JSON 键完全一致（JsonUtility 不支持字段重命名），均为大驼峰。
    /// </summary>
    [Serializable]
    public class AnimationKeyframeClipData
    {
        /// <summary>Clip 名称</summary>
        public string Name;

        /// <summary>Clip 时长（秒）</summary>
        public float Length;

        public List<AnimationKeyframeGroupData> Groups = new List<AnimationKeyframeGroupData>();

        public static AnimationKeyframeClipData FromJson(string json)
        {
            return string.IsNullOrEmpty(json)
                ? null
                : JsonUtility.FromJson<AnimationKeyframeClipData>(json);
        }

        public static AnimationKeyframeClipData FromJson(TextAsset asset)
        {
            return asset != null ? FromJson(asset.text) : null;
        }

        public AnimationKeyframeGroupData GetGroup(string name)
        {
            if (Groups == null || string.IsNullOrEmpty(name)) return null;

            for (int i = 0; i < Groups.Count; i++)
            {
                if (Groups[i] != null && string.Equals(Groups[i].Name, name, StringComparison.Ordinal))
                    return Groups[i];
            }

            return null;
        }

        /// <summary>在所有分组中按 Transform 路径查找节点（先匹配到的优先）。</summary>
        public AnimationKeyframeNodeData FindNode(string path)
        {
            if (Groups == null) return null;

            for (int i = 0; i < Groups.Count; i++)
            {
                AnimationKeyframeNodeData node = Groups[i]?.FindNode(path);
                if (node != null) return node;
            }

            return null;
        }

        /// <summary>在指定分组中按 Transform 路径查找节点。</summary>
        public AnimationKeyframeNodeData FindNode(string groupName, string path)
        {
            return GetGroup(groupName)?.FindNode(path);
        }

        public int NodeCount
        {
            get
            {
                if (Groups == null) return 0;

                int count = 0;
                for (int i = 0; i < Groups.Count; i++)
                    count += Groups[i]?.Nodes?.Count ?? 0;

                return count;
            }
        }

        /// <summary>位移（根节点）所在分组的约定名称</summary>
        public const string MotionGroupName = "Root";

        /// <summary>
        /// 查找用于位移的关键帧节点：优先取 Root 分组，其次取第一个带位置关键帧的节点。
        /// 导出时把参与位移的根节点放在 Root 分组，其位置已累加为以动画根原点为基准的坐标。
        /// </summary>
        public AnimationKeyframeNodeData FindMotionNode()
        {
            var node = FindPositionNode(GetGroup(MotionGroupName)?.Nodes);
            if (node != null) return node;

            if (Groups == null) return null;

            for (int i = 0; i < Groups.Count; i++)
            {
                node = FindPositionNode(Groups[i]?.Nodes);
                if (node != null) return node;
            }

            return null;
        }

        private static AnimationKeyframeNodeData FindPositionNode(List<AnimationKeyframeNodeData> nodes)
        {
            if (nodes == null) return null;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node?.Position != null && node.Position.HasKey) return node;
            }

            return null;
        }

        /// <summary>
        /// 采样 [begin, end) 时间段内根节点的位置增量（模型空间）。
        /// 循环：每跨过一次片段末尾都从 0 重新累计，即片段位移被重复应用，直到 end；
        /// 不循环：只累计到片段末尾，之后不再产生位移（应用一次）。
        /// </summary>
        public FPVector3 SampleMotion(AnimationKeyframeNodeData node, float begin, float end, bool loop)
        {
            if (node == null || Length <= 0f || end <= begin) return FPVector3.zero;

            var motion = Vector3.zero;
            var bt = begin;

            while (bt < end)
            {
                // 当前循环段的末尾；不循环时就是片段末尾
                var cycleend = loop ? (Mathf.Floor(bt / Length) + 1f) * Length : Length;
                var cur = Mathf.Min(end, cycleend);
                // 浮点精度导致段末没有前进时直接结束, 避免死循环
                if (cur <= bt) break;

                // 循环时把时间折算回片段内 (每段都从 0 开始)，不循环时本来就在片段内
                var cyclebegin = cycleend - Length;

                motion += node.SamplePosition(cur - cyclebegin) - node.SamplePosition(bt - cyclebegin);
                bt = cur;

                // 不循环时片段末尾之后的位移不再应用
                if (false == loop) break;
            }

            return new FPVector3((FP)motion.x, (FP)motion.y, (FP)motion.z);
        }
    }

    /// <summary>
    /// 一个节点组（对应导出时配置的分组）。
    /// </summary>
    [Serializable]
    public class AnimationKeyframeGroupData
    {
        public string Name;
        public List<AnimationKeyframeNodeData> Nodes = new List<AnimationKeyframeNodeData>();

        public AnimationKeyframeNodeData FindNode(string path)
        {
            if (Nodes == null || string.IsNullOrEmpty(path)) return null;

            for (int i = 0; i < Nodes.Count; i++)
            {
                if (Nodes[i] != null && string.Equals(Nodes[i].Path, path, StringComparison.Ordinal))
                    return Nodes[i];
            }

            return null;
        }
    }

    /// <summary>
    /// 单个节点的关键帧数据：位置 XYZ + 欧拉角 XYZ（度）。
    /// </summary>
    [Serializable]
    public class AnimationKeyframeNodeData
    {
        /// <summary>Transform 完整层级路径；Animator 所在节点导出为 &lt;Root&gt;</summary>
        public string Path;

        public AnimationKeyframePositionData Position;

        /// <summary>局部欧拉角（度，相对父节点），缺失时按 0 处理</summary>
        public AnimationKeyframeEulerData Euler;

        public Vector3 SamplePosition(float time)
        {
            return Position != null ? Position.Evaluate(time, Vector3.zero) : Vector3.zero;
        }

        public Vector3 SampleEuler(float time)
        {
            return Euler != null ? Euler.Evaluate(time, Vector3.zero) : Vector3.zero;
        }

        /// <summary>只取绕 Y 轴角度（度）</summary>
        public float SampleAngleY(float time)
        {
            return Euler != null ? Euler.SampleY(time) : 0f;
        }
    }

    /// <summary>
    /// 位置的按轴关键帧，缺失轴（null 或空）按 0 处理。
    /// </summary>
    [Serializable]
    public class AnimationKeyframePositionData
    {
        public List<AnimationKeyframeKey> X;
        public List<AnimationKeyframeKey> Y;
        public List<AnimationKeyframeKey> Z;

        public bool HasKey
        {
            get { return Has(X) || Has(Y) || Has(Z); }
        }

        public float SampleX(float time, float defaultValue = 0f)
            => AnimationKeyframeSampler.Evaluate(X, time, defaultValue);

        public float SampleY(float time, float defaultValue = 0f)
            => AnimationKeyframeSampler.Evaluate(Y, time, defaultValue);

        public float SampleZ(float time, float defaultValue = 0f)
            => AnimationKeyframeSampler.Evaluate(Z, time, defaultValue);

        public Vector3 Evaluate(float time, Vector3 defaultValue)
        {
            return new Vector3(
                AnimationKeyframeSampler.Evaluate(X, time, defaultValue.x),
                AnimationKeyframeSampler.Evaluate(Y, time, defaultValue.y),
                AnimationKeyframeSampler.Evaluate(Z, time, defaultValue.z));
        }

        public Vector3 Evaluate(float time)
        {
            return Evaluate(time, Vector3.zero);
        }

        private static bool Has(List<AnimationKeyframeKey> keys)
        {
            return keys != null && keys.Count > 0;
        }
    }

    /// <summary>
    /// 欧拉角（度）的按轴关键帧，缺失轴（null 或空）按 0 处理。
    /// </summary>
    [Serializable]
    public class AnimationKeyframeEulerData
    {
        public List<AnimationKeyframeKey> X;
        public List<AnimationKeyframeKey> Y;
        public List<AnimationKeyframeKey> Z;

        public bool HasKey
        {
            get { return Has(X) || Has(Y) || Has(Z); }
        }

        public float SampleX(float time, float defaultValue = 0f)
            => AnimationKeyframeSampler.Evaluate(X, time, defaultValue);

        public float SampleY(float time, float defaultValue = 0f)
            => AnimationKeyframeSampler.Evaluate(Y, time, defaultValue);

        public float SampleZ(float time, float defaultValue = 0f)
            => AnimationKeyframeSampler.Evaluate(Z, time, defaultValue);

        public Vector3 Evaluate(float time, Vector3 defaultValue)
        {
            return new Vector3(
                AnimationKeyframeSampler.Evaluate(X, time, defaultValue.x),
                AnimationKeyframeSampler.Evaluate(Y, time, defaultValue.y),
                AnimationKeyframeSampler.Evaluate(Z, time, defaultValue.z));
        }

        public Vector3 Evaluate(float time)
        {
            return Evaluate(time, Vector3.zero);
        }

        private static bool Has(List<AnimationKeyframeKey> keys)
        {
            return keys != null && keys.Count > 0;
        }
    }

    /// <summary>
    /// 单个关键帧（字段名与 JSON 键一致）。
    /// </summary>
    [Serializable]
    public class AnimationKeyframeKey
    {
        /// <summary>时间（秒）</summary>
        public float T;

        /// <summary>值</summary>
        public float V;
    }

    /// <summary>
    /// 关键帧线性采样：时间越界取端点值，无关键帧返回默认值。
    /// </summary>
    public static class AnimationKeyframeSampler
    {
        public static float Evaluate(List<AnimationKeyframeKey> keys, float time, float defaultValue)
        {
            if (keys == null || keys.Count == 0) return defaultValue;

            int count = keys.Count;
            if (time <= keys[0].T) return keys[0].V;

            int last = count - 1;
            if (time >= keys[last].T) return keys[last].V;

            int index = FindIndex(keys, time);
            AnimationKeyframeKey k0 = keys[index];
            AnimationKeyframeKey k1 = keys[index + 1];

            float dt = k1.T - k0.T;
            if (dt <= 1e-6f) return k1.V;

            return k0.V + (k1.V - k0.V) * ((time - k0.T) / dt);
        }

        /// <summary>返回满足 keys[index].T &lt;= time &lt; keys[index + 1].T 的下标。</summary>
        private static int FindIndex(List<AnimationKeyframeKey> keys, float time)
        {
            int lo = 0;
            int hi = keys.Count - 1;

            while (lo + 1 < hi)
            {
                int mid = (lo + hi) >> 1;
                if (keys[mid].T <= time) lo = mid;
                else hi = mid;
            }

            return lo;
        }
    }

    /// <summary>
    /// 关键帧文件缓存：按动画名称加载 Assets/AssetRaws/AnimationKeyframes/{动画名}.json，
    /// 解析结果与资源实例分离，只加载/解析一次。
    /// 逻辑层 (Facade 的位移应用) 按动画名取用，不需要每帧读文件。
    /// </summary>
    public static class AnimationKeyframeCache
    {
        private static readonly Dictionary<string, AnimationKeyframeClipData> cache =
            new Dictionary<string, AnimationKeyframeClipData>(StringComparer.Ordinal);

        /// <summary>
        /// 获取动画的关键帧数据，文件不存在或解析失败返回 null
        /// 失败也会缓存，避免每帧重复加载
        /// </summary>
        /// <param name="name">动画名称（Clip 名称，也是文件名）</param>
        public static AnimationKeyframeClipData Load(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (cache.TryGetValue(name, out var data)) return data;

            var asset = ResourceModule.Instance.LoadAssetSync<TextAsset>(Location.animationkeyframes + name);
            data = asset != null ? AnimationKeyframeClipData.FromJson(asset.text) : null;
            cache.Add(name, data);

            return data;
        }
    }
}
