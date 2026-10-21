using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameLogic
{
    public class GameEntry : GameSystem<GameEntry>
    {
        private class GameNode
        {
            private GameState state;
            private Type[] system = null;
            private GameNode[] children = null;

            public GameNode(GameState state, Type[] system, GameNode[] children)
            {
                this.state = state;
                this.system = system ?? new Type[0];
                this.children = children ?? new GameNode[0];
            }

            public GameState State => state;
            public Type[] Systems => system;
            public GameNode[] Children => children;
        }


        private GameNode root;
        private GameNode current;
        private bool transitioning = false;
        private Stack<GameState> stateHistory = new Stack<GameState>();
        private Dictionary<Type, GameSystem> systemCache = new Dictionary<Type, GameSystem>();

        // 缓存当前节点到根节点的路径
        private List<GameNode> currentPath = new List<GameNode>();

        public void Awake()
        {
            Init();
        }

        public void Init()
        {
            base.OnInit();

            Debug.Log("=== 游戏管理器初始化 ===\n");

            // 构建状态树
            BuildTree();

            // 初始化所有系统
            InitSystem();

            // 初始化路径缓存
            UpdateCurrentPath();

            // 进入根节点
            EnterNode(root);

            DontDestroyOnLoad(gameObject);

            Debug.Log("\n=== 初始化完成 ===\n");
        }

        private void BuildTree()
        {
            root = new GameNode
            (
                GameState.Root,
                null,
                new[]
                {
                    new GameNode
                    (
                        GameState.Main,
                        new[]
                        {
                            typeof(MainSys)
                        },
                        null
                    ),
                    new GameNode
                    (
                        GameState.Hall,
                        new[]
                        {                            
                            typeof(HeroSys),
                            typeof(PackSys),
                            typeof(HallSys),
                        },
                        new[]
                        {
                            new GameNode
                            (
                                GameState.Task,
                                new[]
                                {
                                    typeof (TaskSys)
                                },
                                null
                            ),
                            new GameNode
                            (
                                GameState.Mail,
                                new[]
                                {
                                    typeof (MailSys)
                                },
                                null
                            ),
                            new GameNode
                            (
                                GameState.Mall,
                                new[]
                                {
                                    typeof (MallSys)
                                },
                                null
                            ),
                            new GameNode
                            (
                                GameState.Pack,
                                new[]
                                {
                                    typeof (PackSys)
                                },
                                null
                            ),
                            new GameNode
                            (
                                GameState.UIPack,
                                new[]
                                {
                                    typeof (PackUISys)
                                },
                                null
                            ),
                            new GameNode
                            (
                                GameState.UIHero,
                                new[]
                                {
                                    typeof (HeroUISys)
                                },
                                null
                            )
                        }
                    ),
                    new GameNode
                    (
                        GameState.Gameplay,
                        null,
                        new[]
                        {
                            new GameNode
                            (
                                GameState.GameplayLocal,
                                null,
                                null
                            )
                        }
                    )
                }
            );

            // 设置当前节点为根节点
            current = root;
        }

        private void InitSystem()
        {
            InitNodeSystem(root, transform);
        }

        private void InitNodeSystem(GameNode node, Transform parent)
        {
            // 创建当前节点对应的GameObject
            string nodeName = $"{node.State}";
            GameObject nodeGO = new GameObject(nodeName);
            nodeGO.transform.SetParent(parent);

            // 初始化当前节点的系统并挂载到GameObject上
            foreach (var systemType in node.Systems)
            {
                if (!systemCache.ContainsKey(systemType))
                {
                    // 创建系统组件并挂载到节点GameObject上
                    var system = nodeGO.AddComponent(systemType) as GameSystem;
                    system.OnInit();
                    systemCache[systemType] = system;
                    Debug.Log($"[初始化] {systemType.Name} 用于状态 {node.State}");
                }
            }

            // 递归初始化子节点，传入当前节点的Transform作为父节点
            foreach (var child in node.Children)
            {
                InitNodeSystem(child, nodeGO.transform);
            }
        }

        public void ChangeState(GameState targetState)
        {
            if (transitioning)
            {
                Debug.Log("正在转换中，忽略请求");
                return;
            }

            if (current.State == targetState)
            {
                Debug.Log($"已经在状态 {targetState}");
                return;
            }

            var targetNode = FindNodeByState(root, targetState);
            if (targetNode == null)
            {
                Debug.Log($"状态 {targetState} 未找到！");
                return;
            }

            transitioning = true;

            try
            {
                Debug.Log($"状态转换: {current.State} -> {targetState}");

                // 保存历史
                stateHistory.Push(current.State);

                // 查找公共祖先并执行转换
                var commonAncestor = FindCommonAncestor(current, targetNode);

                // 离开当前状态路径到公共祖先
                LeaveToAncestor(current, commonAncestor);

                // 从公共祖先进入目标状态
                EnterToTarget(commonAncestor, targetNode);

                // 更新当前节点
                current = targetNode;

                // 更新缓存路径
                UpdateCurrentPath();

                Debug.Log($"状态转换完成。当前状态: {current.State}");
            }
            finally
            {
                transitioning = false;
            }
        }

        private GameNode FindNodeByState(GameNode node, GameState state)
        {
            if (node.State == state)
                return node;

            foreach (var child in node.Children)
            {
                var result = FindNodeByState(child, state);
                if (result != null)
                    return result;
            }
            return null;
        }

        private GameNode FindCommonAncestor(GameNode node1, GameNode node2)
        {
            // 获取节点1的所有祖先（包括自身）
            var ancestors1 = new HashSet<GameNode>();
            var current = node1;
            while (current != null)
            {
                ancestors1.Add(current);
                current = GetParent(current);
            }

            // 查找节点2的祖先中第一个在ancestors1中的节点
            current = node2;
            while (current != null)
            {
                if (ancestors1.Contains(current))
                    return current;
                current = GetParent(current);
            }

            return null;
        }

        private GameNode GetParent(GameNode node)
        {
            // 遍历树查找父节点
            return FindParent(root, node);
        }

        private GameNode FindParent(GameNode currentNode, GameNode targetNode)
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

        private void LeaveToAncestor(GameNode fromNode, GameNode ancestor)
        {
            var current = fromNode;
            while (current != null && current != ancestor)
            {
                LeaveNode(current);
                current = GetParent(current);
            }
        }

        private void EnterToTarget(GameNode ancestor, GameNode targetNode)
        {
            // 获取从ancestor到targetNode的路径
            var path = new List<GameNode>();
            var current = targetNode;
            while (current != null && current != ancestor)
            {
                path.Insert(0, current);
                current = GetParent(current);
            }

            // 沿路径进入
            foreach (var node in path)
            {
                EnterNode(node);
            }
        }

        private void EnterNode(GameNode node)
        {
            Debug.Log($"[进入] 状态: {node.State}");

            // 启动节点的所有系统
            foreach (var systemType in node.Systems)
            {
                if (systemCache.TryGetValue(systemType, out var system))
                {
                    system.OnEnter();
                    Debug.Log($"  ├─ 系统启动: {systemType.Name}");
                }
            }
        }

        private void LeaveNode(GameNode node)
        {
            Debug.Log($"[离开] 状态: {node.State}");

            // 停止节点的所有系统
            foreach (var systemType in node.Systems)
            {
                if (systemCache.TryGetValue(systemType, out var system))
                {
                    system.OnLeave();
                    Debug.Log($"  ├─ 系统停止: {systemType.Name}");
                }
            }
        }

        // 更新当前节点到根节点的路径缓存
        private void UpdateCurrentPath()
        {
            currentPath.Clear();
            var node = current;

            // 收集从当前节点到根节点的路径
            var tempPath = new List<GameNode>();
            while (node != null)
            {
                tempPath.Add(node);
                node = GetParent(node);
            }

            // 反转顺序，从根到当前节点
            for (int i = tempPath.Count - 1; i >= 0; i--)
            {
                currentPath.Add(tempPath[i]);
            }
        }

        public void Update()
        {
            if (transitioning || current == null)
            {
                return;
            }

            // 直接使用缓存的路径，无需每次查找
            foreach (var node in currentPath)
            {
                foreach (var systemType in node.Systems)
                {
                    if (systemCache.TryGetValue(systemType, out var system))
                    {
                        system.OnUpdate();
                    }
                }
            }
        }

        public void FixedUpdate()
        {
            if (transitioning || current == null)
            {
                return;
            }

            foreach (var node in currentPath)
            {
                foreach (var systemType in node.Systems)
                {
                    if (systemCache.TryGetValue(systemType, out var system))
                    {
                        system.OnFixedUpdate();
                    }
                }
            }
        }

        public void GoBack()
        {
            if (stateHistory.Count > 0)
            {
                var previousState = stateHistory.Pop();
                Debug.Log($"返回上一个状态: {previousState}");
                ChangeState(previousState);
            }
            else
            {
                Debug.Log("没有历史状态可返回");
            }
        }

        public T GetSystem<T>() where T : GameSystem
        {
            systemCache.TryGetValue(typeof(T), out var system);
            return system as T;
        }

        public bool IsInState(GameState state)
        {
            return current != null && current.State == state;
        }

        public bool IsInStateOrChild(GameState state)
        {
            var stateNode = FindNodeByState(root, state);
            if (stateNode == null) return false;

            var current = this.current;
            while (current != null)
            {
                if (current == stateNode)
                {
                    return true;
                }
                current = GetParent(current);
            }
            return false;
        }

        // 获取当前路径（调试用）
        public List<GameState> GetCurrentPath()
        {
            return currentPath.Select(node => node.State).ToList();
        }

        public override void OnClear()
        {
            Debug.Log("\n=== 清理游戏管理器 ===\n");

            // 清理所有系统
            foreach (var system in systemCache.Values)
            {
                system.OnClear();
                Debug.Log($"[清理] 系统: {system.GetType().Name}");
            }

            systemCache.Clear();
            stateHistory.Clear();
            currentPath.Clear();
            current = null;
            root = null;
            transitioning = false;

            base.OnClear();

            Debug.Log("\n=== 清理完成 ===\n");
        }
    }
}
