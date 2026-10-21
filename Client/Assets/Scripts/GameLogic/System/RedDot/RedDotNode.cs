using System;
using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 红点节点（支持多父节点的 DAG 结构）
    /// 叶子节点：直接通过 SetValue 设置数值
    /// 非叶子节点：数值由所有子节点聚合而来（默认求和，可自定义聚合器）
    /// </summary>
    public class RedDotNode
    {
        /// <summary>节点唯一路径 Key，例如 "Main/Mail/System"</summary>
        public string Key { get; private set; }

        /// <summary>当前红点数值（0 表示不显示）</summary>
        public int Value { get; private set; }

        /// <summary>是否为叶子节点（叶子节点的 Value 由外部直接设置）</summary>
        public bool IsLeaf { get; internal set; }

        /// <summary>自定义聚合器：将子节点值合并成当前节点值。默认：求和</summary>
        public Func<IReadOnlyList<RedDotNode>, int> Aggregator { get; set; }

        /// <summary>值变化事件 (oldValue, newValue)</summary>
        public event Action<int, int> OnValueChanged;

        // 多父节点 & 多子节点
        private readonly HashSet<RedDotNode> _parents = new HashSet<RedDotNode>();
        private readonly HashSet<RedDotNode> _children = new HashSet<RedDotNode>();

        public IReadOnlyCollection<RedDotNode> Parents => _parents;
        public IReadOnlyCollection<RedDotNode> Children => _children;

        // 脏标记：批量刷新用
        internal bool IsDirty { get; set; }

        internal RedDotNode(string key)
        {
            Key = key;
            Value = 0;
            IsLeaf = true;
        }

        #region 父子关系维护

        internal void AddParent(RedDotNode parent)
        {
            _parents.Add(parent);
        }

        internal void RemoveParent(RedDotNode parent)
        {
            _parents.Remove(parent);
        }

        internal void AddChild(RedDotNode child)
        {
            _children.Add(child);
            // 添加子节点后，自己就不再是叶子节点
            IsLeaf = false;
        }

        internal void RemoveChild(RedDotNode child)
        {
            _children.Remove(child);
            if (_children.Count == 0)
            {
                IsLeaf = true;
            }
        }

        #endregion

        #region 值设置与计算

        /// <summary>
        /// 直接设置叶子节点的值（非叶子节点调用将被忽略并告警）
        /// </summary>
        internal void SetLeafValue(int value)
        {
            if (!IsLeaf)
            {
                UnityEngine.Debug.LogWarning($"[RedDot] 节点 {Key} 不是叶子节点，不能直接 SetValue，请设置其子节点。");
                return;
            }
            ApplyValue(value);
        }

        /// <summary>
        /// 根据子节点重新计算值（非叶子节点用）
        /// </summary>
        internal void Recalculate()
        {
            if (IsLeaf) return;

            int newValue;
            if (Aggregator != null)
            {
                var list = new List<RedDotNode>(_children);
                newValue = Aggregator(list);
            }
            else
            {
                // 默认聚合：求和
                newValue = 0;
                foreach (var child in _children)
                {
                    newValue += child.Value;
                }
            }

            ApplyValue(newValue);
        }

        private void ApplyValue(int newValue)
        {
            if (newValue < 0) newValue = 0;
            if (newValue == Value) return;

            int oldValue = Value;
            Value = newValue;

            try
            {
                OnValueChanged?.Invoke(oldValue, newValue);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[RedDot] {Key} OnValueChanged 回调异常：{e}");
            }
        }

        #endregion

        /// <summary>是否有红点（值 > 0）</summary>
        public bool IsActive => Value > 0;

        public override string ToString()
        {
            return $"[RedDot:{Key} Value={Value} Leaf={IsLeaf} Parents={_parents.Count} Children={_children.Count}]";
        }
    }

}
