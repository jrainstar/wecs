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
}
