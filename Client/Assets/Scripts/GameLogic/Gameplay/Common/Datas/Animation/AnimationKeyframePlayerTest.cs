using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Gameplay.Common
{
    /// <summary>
    /// 导出的关键帧 JSON 测试播放器。
    /// 把 AnimationKeyframeClipData 采样出来，驱动指定根节点下的同名层级物体：
    /// 位置按世界坐标设置，欧拉角按局部角度设置。
    ///
    /// 用法：
    /// 1. 把本脚本挂到模型的根物体上（带 Animator 的那个，例如 Chun）；
    /// 2. 把导出的 JSON（TextAsset）拖到 Json 字段；
    /// 3. 运行场景即可播放，可勾选 Loop / 调整 Speed 对比 Unity 动画效果。
    /// 若挂到其他物体上，用 Root 指定路径匹配的根节点。
    ///
    /// 绑定方式：
    /// - Path：按 JSON 里的完整路径（Armature/Root）匹配；
    /// - Name：按节点名（Root、Bip001Spine）匹配，可手动配置表、自动查找或运行时代码绑定。
    ///
    /// 运行时按名字绑定示例：
    /// <code>
    /// player.Bind("Bip001Spine", spineTransform);   // 绑定并立即开始驱动
    /// player.Unbind("Bip001Spine");
    /// Transform t = player.GetBound("Bip001Spine");
    /// </code>
    /// </summary>
    public class AnimationKeyframePlayerTest : MonoBehaviour
    {
        public enum BindMode
        {
            Path = 0,
            Name = 1,
        }

        [Serializable]
        public class NameTarget
        {
            [Tooltip("节点名（JSON 里 Path 的最后一段）")]
            public string Name;

            [Tooltip("要驱动的场景物体")]
            public Transform Target;
        }

        [Header("数据")]
        [Tooltip("导出工具生成的 JSON（可拖入 .json 资源）")]
        [SerializeField] private TextAsset _Json;

        [Tooltip("路径/名字匹配的根节点，留空则使用本物体；导出的 Path 相对于模型根（Animator 所在物体）")]
        [SerializeField] private Transform _Root;

        [Header("绑定方式")]
        [SerializeField] private BindMode _BindMode = BindMode.Path;

        [Tooltip("按名字绑定时，在 Root 下递归按名字自动查找（同名只取第一个）")]
        [SerializeField] private bool _AutoBindByName = true;

        [Tooltip("手动绑定表：节点名 → 物体，优先级高于自动查找；可在运行时修改")]
        [SerializeField] private List<NameTarget> _NameTargets = new List<NameTarget>();

        [Header("播放")]
        [SerializeField] private bool _PlayOnStart = true;
        [SerializeField] private bool _Loop = true;
        [Tooltip("播放速度倍率")]
        [SerializeField] private float _Speed = 1f;

        [Header("应用")]
        [SerializeField] private bool _ApplyPosition = true;
        [Tooltip("欧拉角是相对父节点的局部角度，因此写入 localEulerAngles")]
        [SerializeField] private bool _ApplyEuler = true;
        [SerializeField] private bool _DrawGizmos = true;

        private AnimationKeyframeClipData _Data;
        private readonly Dictionary<string, Transform> _TransformMap = new Dictionary<string, Transform>();
        private readonly Dictionary<string, Transform> _NameMap = new Dictionary<string, Transform>(StringComparer.Ordinal);
        private readonly Dictionary<string, Transform> _RuntimeBindings = new Dictionary<string, Transform>(StringComparer.Ordinal);
        private readonly List<AnimationKeyframeNodeData> _Nodes = new List<AnimationKeyframeNodeData>();
        private readonly List<Transform> _NodeTransforms = new List<Transform>();

        private float _Time;
        private bool _Playing;

        public bool IsPlaying => _Playing;
        public float CurrentTime => _Time;
        public float Length => _Data != null ? _Data.Length : 0f;
        public int MatchedNodeCount => _Nodes.Count;

        private void Awake()
        {
            Reload();
        }

        private void OnEnable()
        {
            if (_PlayOnStart && _Data != null)
                PlayFromStart();
        }

        private void Update()
        {
            if (!_Playing || _Data == null) return;

            _Time += Time.deltaTime * _Speed;

            if (_Time >= _Data.Length)
            {
                if (_Loop)
                    _Time = _Data.Length > 0f ? Mathf.Repeat(_Time, _Data.Length) : 0f;
                else
                {
                    _Time = _Data.Length;
                    _Playing = false;
                }
            }

            Apply(_Time);
        }

        /// <summary>重新读取 JSON 并重建路径映射。</summary>
        [ContextMenu("重新加载 JSON")]
        public void Reload()
        {
            _Data = _Json != null ? AnimationKeyframeClipData.FromJson(_Json) : null;

            _TransformMap.Clear();
            _Nodes.Clear();
            _NodeTransforms.Clear();

            if (_Data == null)
            {
                if (_Json != null)
                    Debug.LogError($"[AnimationKeyframePlayerTest] JSON 解析失败：{_Json.name}", this);
                return;
            }

            BuildTransformMap();
            MatchNodes();

            Debug.Log(
                $"[AnimationKeyframePlayerTest] {_Json.name}：{_Data.Name}，长度 {_Data.Length:0.###}s，" +
                $"共 {_Data.NodeCount} 个节点，匹配到 {_Nodes.Count} 个 Transform。",
                this);
        }

        [ContextMenu("从头播放")]
        public void PlayFromStart()
        {
            _Time = 0f;
            _Playing = _Data != null;
            Apply(_Time);
        }

        public void Pause()
        {
            _Playing = false;
        }

        public void Resume()
        {
            _Playing = _Data != null;
        }

        public void Stop()
        {
            _Playing = false;
            _Time = 0f;
            Apply(_Time);
        }

        /// <summary>把时间定位到指定位置并立即应用。</summary>
        public void SetTime(float time)
        {
            if (_Data == null) return;

            _Time = Mathf.Clamp(time, 0f, _Data.Length);
            Apply(_Time);
        }

        public void SetJson(TextAsset json, bool play = true)
        {
            _Json = json;
            Reload();

            if (play) PlayFromStart();
        }

        /// <summary>
        /// 运行时按节点名绑定物体，例如：player.Bind("Bip001Spine", spineTransform)。
        /// 会自动切换到按名字绑定模式并立即重新匹配。
        /// </summary>
        public void Bind(string nodeName, Transform target)
        {
            if (string.IsNullOrEmpty(nodeName) || target == null) return;

            _RuntimeBindings[nodeName] = target;
            _BindMode = BindMode.Name;
            MatchNodes();
        }

        public void Bind(string nodeName, GameObject target)
        {
            Bind(nodeName, target != null ? target.transform : null);
        }

        /// <summary>移除运行时绑定。</summary>
        public bool Unbind(string nodeName)
        {
            if (string.IsNullOrEmpty(nodeName)) return false;
            if (!_RuntimeBindings.Remove(nodeName)) return false;

            MatchNodes();
            return true;
        }

        /// <summary>取节点名当前绑定的物体（运行时绑定 &gt; 手动表 &gt; 自动查找），不存在返回 null。</summary>
        public Transform GetBound(string nodeName)
        {
            if (string.IsNullOrEmpty(nodeName)) return null;

            if (_RuntimeBindings.TryGetValue(nodeName, out Transform runtime) && runtime != null)
                return runtime;

            if (TryGetManualTarget(nodeName, out Transform manual))
                return manual;

            return _NameMap.TryGetValue(nodeName, out Transform auto) ? auto : null;
        }

        public void SetBindMode(BindMode mode)
        {
            _BindMode = mode;
            MatchNodes();
        }

        /// <summary>当前已匹配到的节点名列表，便于运行时排查。</summary>
        public List<string> GetBoundNodeNames()
        {
            var names = new List<string>(_Nodes.Count);
            for (int i = 0; i < _Nodes.Count; i++)
                names.Add(GetNodeName(_Nodes[i].Path));

            return names;
        }

        private void BuildTransformMap()
        {
            Transform root = _Root != null ? _Root : transform;
            if (root == null) return;

            Register(root, string.Empty);
            Register(root, "<Root>");
            RegisterChildren(root, string.Empty);

            _NameMap.Clear();
            if (_AutoBindByName)
            {
                RegisterName(root);
                RegisterNameChildren(root);
            }
        }

        private void RegisterName(Transform target)
        {
            if (target == null || _NameMap.ContainsKey(target.name)) return;

            _NameMap[target.name] = target;
        }

        private void RegisterNameChildren(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                RegisterName(child);
                RegisterNameChildren(child);
            }
        }

        private void RegisterChildren(Transform parent, string parentPath)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                string path = string.IsNullOrEmpty(parentPath) ? child.name : parentPath + "/" + child.name;

                Register(child, path);
                RegisterChildren(child, path);
            }
        }

        private void Register(Transform target, string path)
        {
            if (target == null) return;

            if (!_TransformMap.ContainsKey(path))
                _TransformMap[path] = target;
        }

        private void MatchNodes()
        {
            _Nodes.Clear();
            _NodeTransforms.Clear();

            if (_Data.Groups == null) return;

            for (int g = 0; g < _Data.Groups.Count; g++)
            {
                AnimationKeyframeGroupData group = _Data.Groups[g];
                if (group?.Nodes == null) continue;

                for (int n = 0; n < group.Nodes.Count; n++)
                {
                    AnimationKeyframeNodeData node = group.Nodes[n];
                    if (node == null) continue;

                    string nodeName = GetNodeName(node.Path);
                    Transform target = ResolveTarget(node.Path, nodeName);

                    if (target == null)
                    {
                        Debug.LogWarning(
                            _BindMode == BindMode.Name
                                ? $"[AnimationKeyframePlayerTest] 未绑定节点名：{nodeName}"
                                : $"[AnimationKeyframePlayerTest] 找不到路径对应的 Transform：{node.Path}",
                            this);
                        continue;
                    }

                    _Nodes.Add(node);
                    _NodeTransforms.Add(target);
                }
            }
        }

        /// <summary>解析节点对应的物体：运行时绑定 &gt; 手动表 &gt; 自动按名字查找。</summary>
        private Transform ResolveTarget(string path, string nodeName)
        {
            if (_BindMode == BindMode.Path)
            {
                return _TransformMap.TryGetValue(path ?? string.Empty, out Transform byPath) ? byPath : null;
            }

            if (string.IsNullOrEmpty(nodeName)) return null;

            if (_RuntimeBindings.TryGetValue(nodeName, out Transform runtime) && runtime != null)
                return runtime;

            if (TryGetManualTarget(nodeName, out Transform manual))
                return manual;

            return _NameMap.TryGetValue(nodeName, out Transform auto) ? auto : null;
        }

        private bool TryGetManualTarget(string nodeName, out Transform target)
        {
            target = null;
            if (_NameTargets == null) return false;

            for (int i = 0; i < _NameTargets.Count; i++)
            {
                NameTarget entry = _NameTargets[i];
                if (entry == null || entry.Target == null) continue;
                if (!string.Equals(entry.Name, nodeName, StringComparison.OrdinalIgnoreCase)) continue;

                target = entry.Target;
                return true;
            }

            return false;
        }

        private static string GetNodeName(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            if (path == "<Root>") return path;

            int slash = path.LastIndexOf('/');
            return slash < 0 ? path : path.Substring(slash + 1);
        }

        private void Apply(float time)
        {
            // 先统一设置旋转，再设置世界位置：避免父级旋转导致子级世界位置被二次偏移
            if (_ApplyEuler)
            {
                for (int i = 0; i < _Nodes.Count; i++)
                {
                    Transform target = _NodeTransforms[i];
                    if (target == null) continue;

                    target.localEulerAngles = _Nodes[i].SampleEuler(time);
                }
            }

            if (_ApplyPosition)
            {
                for (int i = 0; i < _Nodes.Count; i++)
                {
                    Transform target = _NodeTransforms[i];
                    if (target == null) continue;

                    target.position = _Nodes[i].SamplePosition(time);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!_DrawGizmos || _Data == null || _Nodes.Count == 0) return;

            Gizmos.color = new Color(0.1f, 0.9f, 1f, 0.9f);
            for (int i = 0; i < _Nodes.Count; i++)
            {
                if (_NodeTransforms[i] == null) continue;

                Vector3 position = _Nodes[i].SamplePosition(_Time);
                Gizmos.DrawWireSphere(position, 0.03f);
            }
        }
    }
}
