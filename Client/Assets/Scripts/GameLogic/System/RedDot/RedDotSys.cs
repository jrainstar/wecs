using GameLogic;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 红点系统管理器（单例）
    /// - 使用字符串 Key 作为节点唯一标识
    /// - 支持多父节点（DAG 有向无环图）
    /// - 自动脏标记 + 拓扑排序刷新，避免重复计算
    /// - 检测环形依赖
    /// </summary>
    public class RedDotSys : GameSystem<RedDotSys>
    {

        private readonly Dictionary<string, RedDotNode> nodes = new Dictionary<string, RedDotNode>();
        private readonly HashSet<RedDotNode> dirtyNodes = new HashSet<RedDotNode>();

        // 是否启用批量模式（True 时 SetValue 不会立刻刷新，需要手动 Flush）
        private bool batchMode = false;

        // 简易深度计算：节点到叶子最大距离（用于 Flush 排序）
        private readonly Dictionary<RedDotNode, int> depthCache = new Dictionary<RedDotNode, int>();
        #region 节点创建 / 获取

        /// <summary>
        /// 获取或创建节点
        /// </summary>
        public RedDotNode GetOrCreate(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("RedDot Key 不能为空");

            if (!nodes.TryGetValue(key, out var node))
            {
                node = new RedDotNode(key);
                nodes.Add(key, node);
            }
            return node;
        }

        public RedDotNode Get(string key)
        {
            nodes.TryGetValue(key, out var node);
            return node;
        }

        public bool Contains(string key) => nodes.ContainsKey(key);

        /// <summary>
        /// 获取所有已注册的节点 Key（调试/可视化用）
        /// </summary>
        public IEnumerable<string> GetAllKeys() => nodes.Keys;

        #endregion

        #region 关系建立 / 解除

        /// <summary>
        /// 建立父子关系：parentKey 会聚合 childKey 的值。
        /// 一个子节点可以拥有多个父节点。
        /// </summary>
        public void Link(string parentKey, string childKey)
        {
            if (parentKey == childKey)
            {
                Debug.LogError($"[RedDot] 不能把节点 {parentKey} 连接到自身。");
                return;
            }

            var parent = GetOrCreate(parentKey);
            var child = GetOrCreate(childKey);

            // 环检测：如果 parent 是 child 的后代，则会构成环
            if (IsDescendant(child, parent))
            {
                Debug.LogError($"[RedDot] 检测到环形依赖：{parentKey} <-> {childKey}，操作已取消。");
                return;
            }

            parent.AddChild(child);
            child.AddParent(parent);

            // 新关系需要刷新父节点
            MarkDirty(parent);
            FlushIfAuto();
        }

        /// <summary>
        /// 解除父子关系
        /// </summary>
        public void Unlink(string parentKey, string childKey)
        {
            var parent = Get(parentKey);
            var child = Get(childKey);
            if (parent == null || child == null) return;

            parent.RemoveChild(child);
            child.RemoveParent(parent);

            MarkDirty(parent);
            FlushIfAuto();
        }

        /// <summary>
        /// 彻底移除一个节点：断开它与所有父/子节点的关系，并从字典中删除。
        /// 适用于动态删除邮件等场景。
        /// </summary>
        public void RemoveNode(string key)
        {
            var node = Get(key);
            if (node == null) return;

            // 先断开与所有父节点的关系
            var parents = new List<RedDotNode>(node.Parents);
            foreach (var p in parents)
            {
                p.RemoveChild(node);
                MarkDirty(p);
            }

            // 再断开与所有子节点的关系
            var children = new List<RedDotNode>(node.Children);
            foreach (var c in children)
            {
                node.RemoveChild(c);
                c.RemoveParent(node);
            }

            nodes.Remove(key);
            dirtyNodes.Remove(node);

            FlushIfAuto();
        }

        /// <summary>
        /// 判断 maybeAncestor 是否为 node 的祖先（沿 parents 向上走能否找到）
        /// 用来在 Link 时检测环。这里从 node 出发往 children 方向走，看是否能到达 maybeAncestor。
        /// </summary>
        private bool IsDescendant(RedDotNode node, RedDotNode target)
        {
            if (node == target) return true;
            var stack = new Stack<RedDotNode>();
            var visited = new HashSet<RedDotNode>();
            stack.Push(node);
            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                if (!visited.Add(cur)) continue;
                foreach (var c in cur.Children)
                {
                    if (c == target) return true;
                    stack.Push(c);
                }
            }
            return false;
        }

        #endregion

        #region 设置叶子节点值

        /// <summary>
        /// 设置叶子节点数值（会自动向上传播到所有父节点）。
        /// </summary>
        public void SetValue(string key, int value)
        {
            var node = GetOrCreate(key);
            if (!node.IsLeaf)
            {
                Debug.LogWarning($"[RedDot] {key} 已有子节点，不能直接 SetValue。");
                return;
            }

            int old = node.Value;
            node.SetLeafValue(value);

            if (old != node.Value)
            {
                // 向上传播：所有父节点标脏
                PropagateDirty(node);
                FlushIfAuto();
            }
        }

        /// <summary>为叶子节点设 1 / 0 的语法糖</summary>
        public void SetActive(string key, bool active) => SetValue(key, active ? 1 : 0);

        /// <summary>数值加法（叶子节点）</summary>
        public void Add(string key, int delta)
        {
            var node = GetOrCreate(key);
            SetValue(key, node.Value + delta);
        }

        #endregion

        #region 监听

        /// <summary>监听节点值变化（节点会自动创建）</summary>
        public void Listen(string key, Action<int, int> callback)
        {
            var node = GetOrCreate(key);
            node.OnValueChanged += callback;
        }

        public void Unlisten(string key, Action<int, int> callback)
        {
            var node = Get(key);
            if (node != null) node.OnValueChanged -= callback;
        }

        #endregion

        #region 脏标记 / 刷新

        private void MarkDirty(RedDotNode node)
        {
            if (node == null) return;
            dirtyNodes.Add(node);
        }

        /// <summary>将 node 的所有祖先节点都标记为脏（不包含 node 本身，因为叶子节点已经改过）</summary>
        private void PropagateDirty(RedDotNode node)
        {
            var queue = new Queue<RedDotNode>();
            var visited = new HashSet<RedDotNode>();
            foreach (var p in node.Parents) queue.Enqueue(p);

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (!visited.Add(cur)) continue;
                dirtyNodes.Add(cur);
                foreach (var pp in cur.Parents) queue.Enqueue(pp);
            }
        }

        private void FlushIfAuto()
        {
            if (!batchMode) Flush();
        }

        /// <summary>
        /// 立即刷新所有脏节点。按"深度自下而上"的顺序重算，
        /// 确保一个父节点被计算时，它所有子节点的值都是最新的。
        /// </summary>
        public void Flush()
        {
            if (dirtyNodes.Count == 0) return;

            // 复制一份再清空，防止重算过程中回调里又产生新脏节点导致集合被修改
            var dirtyList = new List<RedDotNode>(dirtyNodes);
            dirtyNodes.Clear();

            // 按"到叶子的最大深度"升序排序（叶子=0，往上越来越大）
            // 这样我们从"最靠近叶子的"父节点开始算，保证它的 children 都已是最新值
            dirtyList.Sort((a, b) => GetDepthToLeaf(a).CompareTo(GetDepthToLeaf(b)));

            foreach (var node in dirtyList)
            {
                int old = node.Value;
                node.Recalculate();
                if (old != node.Value)
                {
                    // 值发生变化，继续向上传播
                    foreach (var p in node.Parents)
                    {
                        dirtyNodes.Add(p);
                    }
                }
            }

            // 可能传播过程中又产生了新脏节点，递归 Flush
            if (dirtyNodes.Count > 0)
            {
                Flush();
            }
        }

        private int GetDepthToLeaf(RedDotNode node)
        {
            depthCache.Clear();
            return CalcDepth(node, new HashSet<RedDotNode>());
        }
        private int CalcDepth(RedDotNode node, HashSet<RedDotNode> visiting)
        {
            if (node.IsLeaf) return 0;
            if (depthCache.TryGetValue(node, out var d)) return d;
            if (!visiting.Add(node)) return 0; // 防御性：理论上不会有环

            int max = 0;
            foreach (var c in node.Children)
            {
                int cd = CalcDepth(c, visiting) + 1;
                if (cd > max) max = cd;
            }
            visiting.Remove(node);
            depthCache[node] = max;
            return max;
        }

        #endregion

        #region 调试

        /// <summary>打印整张红点图（调试用）</summary>
        public string Dump()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("========== RedDot Dump ==========");
            foreach (var kv in nodes)
            {
                var n = kv.Value;
                sb.Append(n.ToString());
                if (n.Parents.Count > 0)
                {
                    sb.Append(" Parents=[");
                    foreach (var p in n.Parents) sb.Append(p.Key).Append(',');
                    sb.Append("]");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        /// <summary>清空所有红点数据（一般用于切换账号、重启）</summary>
        public void Clear()
        {
            nodes.Clear();
            dirtyNodes.Clear();
            depthCache.Clear();
        }

        #endregion
    }
}