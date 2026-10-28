using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    public class GameEntry : GameSystem<GameEntry>
    {
        private GameTree m_Tree;
        private Stack<GameState> m_StateHistory = new Stack<GameState>();
        private Dictionary<Type, GameSystem> m_SystemCache = new Dictionary<Type, GameSystem>();

        // ── 状态切换队列（串行）────────────────────────────────
        private readonly Queue<GameState> m_SwitchQueue = new Queue<GameState>();
        private bool m_Running = false;        // 切换队列处理中
        private bool m_SceneLoading = false;   // 场景加载中（切场景期间暂停系统更新）
        private GameState? m_NextState;        // 目标状态

        /// <summary>切换前触发（old, new）</summary>
        public event Action<GameState, GameState> OnPreStateChange;

        /// <summary>离开旧系统之后、进入新系统之前触发（old, new）</summary>
        public event Action<GameState, GameState> OnStateChange;

        /// <summary>切换完成后触发（old, new）</summary>
        public event Action<GameState, GameState> OnPostStateChange;

        /// <summary>当前状态</summary>
        public GameState CurState => m_Tree?.CurState ?? GameState.Root;

        /// <summary>目标状态（切换队列中）</summary>
        public GameState? NextState => m_NextState;

        /// <summary>是否正在切换（含场景加载）</summary>
        public bool IsSwitching => m_Running || m_SceneLoading;

        public void Awake()
        {
            Init();
        }

        public void Init()
        {
            base.OnInit();

            Debug.Log("=== 游戏管理器初始化 ===\n");

            // 构建状态树
            m_Tree = new GameTree();

            // 初始化所有系统
            InitSystem();

            // 进入根节点
            EnterNode(m_Tree.Root);

            DontDestroyOnLoad(gameObject);

            Debug.Log("\n=== 初始化完成 ===\n");
        }

        private void InitSystem()
        {
            InitNodeSystem(m_Tree.Root, transform);
        }

        private void InitNodeSystem(GameTree.Node node, Transform parent)
        {
            // 创建当前节点对应的GameObject
            string nodeName = $"{node.State}";
            GameObject nodeGO = new GameObject(nodeName);
            nodeGO.transform.SetParent(parent);

            // 初始化当前节点的系统并挂载到GameObject上
            foreach (var systemType in node.Systems)
            {
                if (!m_SystemCache.ContainsKey(systemType))
                {
                    // 创建系统组件并挂载到节点GameObject上
                    var system = nodeGO.AddComponent(systemType) as GameSystem;
                    system.OnInit();
                    m_SystemCache[systemType] = system;
                    Debug.Log($"[初始化] {systemType.Name} 用于状态 {node.State}");
                }
            }

            // 递归初始化子节点，传入当前节点的Transform作为父节点
            foreach (var child in node.Children)
            {
                InitNodeSystem(child, nodeGO.transform);
            }
        }

        // ─────────────────────────────────────────────────────
        // 状态切换：同步入队 + 串行异步处理
        // ─────────────────────────────────────────────────────

        public void ChangeState(GameState targetState)
        {
            // 已在目标状态，短路
            if (m_Tree != null && m_Tree.CurState == targetState)
            {
                Debug.Log($"已经在状态 {targetState}");
                return;
            }

            m_SwitchQueue.Enqueue(targetState);
            m_NextState = targetState;

            if (!m_Running)
            {
                HandleSwitchQueueAsync().Forget();
            }
        }

        /// <summary>串行处理切换队列</summary>
        private async UniTaskVoid HandleSwitchQueueAsync()
        {
            m_Running = true;
            try
            {
                while (m_SwitchQueue.Count > 0)
                {
                    var target = m_SwitchQueue.Dequeue();
                    await SwitchStateAsync(target);
                }
            }
            finally
            {
                m_Running = false;
                m_NextState = null;
            }
        }

        /// <summary>单个状态切换（异步）</summary>
        private async UniTask SwitchStateAsync(GameState targetState)
        {
            // 相同状态短路
            if (m_Tree.CurState == targetState)
                return;

            var targetNode = m_Tree.FindNode(targetState);
            if (targetNode == null)
            {
                Debug.Log($"状态 {targetState} 未找到！");
                return;
            }

            var oldState = m_Tree.CurState;

            // 1. 触发切换前事件
            OnPreStateChange?.Invoke(oldState, targetState);

            // 2. 查找公共祖先
            var commonAncestor = m_Tree.FindCommonAncestor(m_Tree.Current, targetNode);

            // 3. 离开旧状态路径到公共祖先（自下而上）
            var leaveNodes = m_Tree.CollectToAncestor(m_Tree.Current, commonAncestor);
            for (int i = leaveNodes.Count - 1; i >= 0; i--)
                LeaveNode(leaveNodes[i]);

            // 4. 触发状态切换事件
            OnStateChange?.Invoke(oldState, targetState);

            // 5. 场景切换（场景名不同才真正加载，切场景期间暂停系统更新）
            m_SceneLoading = true;
            try
            {
                await GameScene.ChangeSceneAsync(targetState, oldState);
            }
            finally
            {
                m_SceneLoading = false;
            }

            // 6. 从公共祖先进入目标状态（自上而下，含异步进入流程）
            var enterNodes = m_Tree.CollectFromAncestor(commonAncestor, targetNode);

            foreach (var node in enterNodes)
            {
                await EnterNodeAsync(node);
            }

            // 7. 新系统收尾（OnStateChangeFinish）
            foreach (var node in enterNodes)
            {
                foreach (var systemType in node.Systems)
                {
                    if (m_SystemCache.TryGetValue(systemType, out var s))
                        s.OnStateChangeFinish();
                }
            }

            // 8. 更新状态与路径缓存
            m_StateHistory.Push(oldState);
            m_Tree.SetCurrent(targetNode);

            Debug.Log($"状态转换完成。当前状态: {m_Tree.CurState}");

            // 9. 触发切换完成事件
            OnPostStateChange?.Invoke(oldState, targetState);
        }

        /// <summary>同步进入节点（供 Init 使用）</summary>
        private void EnterNode(GameTree.Node node)
        {
            Debug.Log($"[进入] 状态: {node.State}");

            foreach (var systemType in node.Systems)
            {
                if (m_SystemCache.TryGetValue(systemType, out var system))
                {
                    system.OnEnter();
                    Debug.Log($"  ├─ 系统启动: {systemType.Name}");
                }
            }
        }

        /// <summary>异步进入节点（含异步进入流程）</summary>
        private async UniTask EnterNodeAsync(GameTree.Node node)
        {
            Debug.Log($"[进入] 状态: {node.State}");

            foreach (var systemType in node.Systems)
            {
                if (m_SystemCache.TryGetValue(systemType, out var system))
                {
                    system.OnEnter();
                    await system.OnEnterAsync();
                    Debug.Log($"  ├─ 系统启动: {systemType.Name}");
                }
            }
        }

        private void LeaveNode(GameTree.Node node)
        {
            Debug.Log($"[离开] 状态: {node.State}");

            foreach (var systemType in node.Systems)
            {
                if (m_SystemCache.TryGetValue(systemType, out var system))
                {
                    system.OnLeave();
                    Debug.Log($"  ├─ 系统停止: {systemType.Name}");
                }
            }
        }

        public void Update()
        {
            // 切换中或场景加载中暂停系统更新
            if (m_Tree == null || m_Running || m_SceneLoading)
            {
                return;
            }

            // 直接使用缓存的路径，无需每次查找
            foreach (var node in m_Tree.CurrentPath)
            {
                foreach (var systemType in node.Systems)
                {
                    if (m_SystemCache.TryGetValue(systemType, out var system))
                    {
                        system.OnUpdate();
                    }
                }
            }
        }

        public void FixedUpdate()
        {
            if (m_Tree == null || m_Running || m_SceneLoading)
            {
                return;
            }

            foreach (var node in m_Tree.CurrentPath)
            {
                foreach (var systemType in node.Systems)
                {
                    if (m_SystemCache.TryGetValue(systemType, out var system))
                    {
                        system.OnFixedUpdate();
                    }
                }
            }
        }

        public void GoBack()
        {
            if (m_StateHistory.Count > 0)
            {
                var previousState = m_StateHistory.Pop();
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
            m_SystemCache.TryGetValue(typeof(T), out var system);
            return system as T;
        }

        public bool IsInState(GameState state)
        {
            return m_Tree != null && m_Tree.CurState == state;
        }

        public bool IsInStateOrChild(GameState state)
        {
            if (m_Tree == null) return false;

            var stateNode = m_Tree.FindNode(state);
            if (stateNode == null) return false;

            var current = m_Tree.Current;
            while (current != null)
            {
                if (current == stateNode)
                {
                    return true;
                }
                current = m_Tree.GetParent(current);
            }
            return false;
        }

        // 获取当前路径（调试用）
        public List<GameState> GetCurrentPath()
        {
            return m_Tree?.CurrentPath.Select(node => node.State).ToList() ?? new List<GameState>();
        }

        public override void OnClear()
        {
            Debug.Log("\n=== 清理游戏管理器 ===\n");

            OnPreStateChange = null;
            OnStateChange = null;
            OnPostStateChange = null;

            // 清理所有系统
            foreach (var system in m_SystemCache.Values)
            {
                system.OnClear();
                Debug.Log($"[清理] 系统: {system.GetType().Name}");
            }

            m_SystemCache.Clear();
            m_SwitchQueue.Clear();
            m_StateHistory.Clear();
            m_Tree = null;
            m_Running = false;
            m_SceneLoading = false;
            m_NextState = null;

            base.OnClear();

            Debug.Log("\n=== 清理完成 ===\n");
        }
    }
}
