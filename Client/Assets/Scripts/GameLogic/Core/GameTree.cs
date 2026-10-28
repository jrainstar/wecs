using System;
using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 游戏状态树：定义状态的树形层级（GameState → 系统 Type[] → 子节点），
    /// 并提供树操作（查找节点、公共祖先、路径收集、当前路径缓存）。
    /// 对应旧版 CGameRootCfg / CGameState。
    /// </summary>
    public class GameTree
    {
        /// <summary>状态树节点</summary>
        public class Node
        {
            public GameState State { get; }
            public Type[] Systems { get; }
            public Node[] Children { get; }

            public Node(GameState state, Type[] systems = null, Node[] children = null)
            {
                State = state;
                Systems = systems ?? Type.EmptyTypes;
                Children = children ?? Array.Empty<Node>();
            }
        }

        private readonly Node m_Root;
        private Node m_Current;
        private readonly List<Node> m_CurrentPath = new List<Node>();

        public Node Root => m_Root;
        public Node Current => m_Current;
        public GameState CurState => m_Current?.State ?? GameState.Root;

        /// <summary>当前节点到根节点的路径（根 → 当前，已缓存）</summary>
        public IReadOnlyList<Node> CurrentPath => m_CurrentPath;

        public GameTree()
        {
            m_Root = Build();
            m_Current = m_Root;
            UpdateCurrentPath();
        }

        /// <summary>设置当前节点并刷新路径缓存</summary>
        public void SetCurrent(Node node)
        {
            m_Current = node;
            UpdateCurrentPath();
        }

        // ── 树构建（硬编码）────────────────────────────────────

        private static Node Build()
        {
            return new Node(
                GameState.Root,
                null,
                new[]
                {
                    new Node(
                        GameState.Main,
                        new[] { typeof(MainSys) }
                    ),
                    new Node(
                        GameState.Hall,
                        new[] { typeof(HeroSys), typeof(PackSys), typeof(HallSys) },
                        new[]
                        {
                            new Node(GameState.Task,   new[] { typeof(TaskSys) }),
                            new Node(GameState.Mail,   new[] { typeof(MailSys) }),
                            new Node(GameState.Mall,   new[] { typeof(MallSys) }),
                            new Node(GameState.Pack,   new[] { typeof(PackSys) }),
                            new Node(GameState.UIPack, new[] { typeof(PackUISys) }),
                            new Node(GameState.UIHero, new[] { typeof(HeroUISys) }),
                        }
                    ),
                    new Node(
                        GameState.Gameplay,
                        null,
                        new[]
                        {
                            new Node(GameState.GameplayLocal),
                        }
                    ),
                }
            );
        }

        // ── 树操作 ─────────────────────────────────────────────

        public Node FindNode(GameState state) => FindNode(m_Root, state);

        private static Node FindNode(Node node, GameState state)
        {
            if (node.State == state)
                return node;

            foreach (var child in node.Children)
            {
                var result = FindNode(child, state);
                if (result != null)
                    return result;
            }
            return null;
        }

        public Node FindCommonAncestor(Node node1, Node node2)
        {
            // 收集 node1 的所有祖先（含自身）
            var ancestors = new HashSet<Node>();
            var current = node1;
            while (current != null)
            {
                ancestors.Add(current);
                current = GetParent(current);
            }

            // 自 node2 向上找第一个交集
            current = node2;
            while (current != null)
            {
                if (ancestors.Contains(current))
                    return current;
                current = GetParent(current);
            }
            return null;
        }

        public Node GetParent(Node node) => FindParent(m_Root, node);

        private static Node FindParent(Node currentNode, Node targetNode)
        {
            foreach (var child in currentNode.Children)
            {
                if (child == targetNode)
                    return currentNode;

                var result = FindParent(child, targetNode);
                if (result != null)
                    return result;
            }
            return null;
        }

        /// <summary>收集 fromNode 到 ancestor（不含 ancestor）的路径，自下而上</summary>
        public List<Node> CollectToAncestor(Node fromNode, Node ancestor)
        {
            var path = new List<Node>();
            var current = fromNode;
            while (current != null && current != ancestor)
            {
                path.Add(current);
                current = GetParent(current);
            }
            return path;
        }

        /// <summary>收集 ancestor 到 targetNode（不含 ancestor）的路径，自上而下</summary>
        public List<Node> CollectFromAncestor(Node ancestor, Node targetNode)
        {
            var path = new List<Node>();
            var current = targetNode;
            while (current != null && current != ancestor)
            {
                path.Insert(0, current);
                current = GetParent(current);
            }
            return path;
        }

        private void UpdateCurrentPath()
        {
            m_CurrentPath.Clear();

            var temp = new List<Node>();
            var node = m_Current;
            while (node != null)
            {
                temp.Add(node);
                node = GetParent(node);
            }

            for (int i = temp.Count - 1; i >= 0; i--)
            {
                m_CurrentPath.Add(temp[i]);
            }
        }
    }
}
