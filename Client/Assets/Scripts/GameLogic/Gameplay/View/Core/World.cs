using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 世界/根组件, 全局唯一
    /// 持有全局服务 (配置/资源/输入/时钟) 以及渲染层的所有单位
    /// 全局服务只在创建时初始化一次, 每局战斗通过 Begin/End 切换战斗内容
    /// </summary>
    public sealed class World : Node
    {
        /// <summary>
        /// 世界根节点
        /// </summary>
        public GameObject root { get; private set; }

        /// <summary>
        /// Stage.Actor, Stage 的数据走的也是 Actor/Behavior/BehaviorInfo 那一套
        /// 通过包装 Actor 的形式使用
        /// 所以 Stage 也是 Actor, 但它是一个特殊的 Actor, 它的 ID 是 ulong.MaxValue
        /// </summary>
        public ulong sa => ulong.MaxValue;

        /// <summary>
        /// 座位 ID
        /// </summary>
        public ulong selfseat { get; private set; } = 0;

        /// <summary>
        /// 自我
        /// </summary>
        public ulong self
        {
            get
            {
                if (false == rilbucket.SeekRIL<RIL_SEAT>(sa, out var ril)) return 0;
                if (false == ril.seatdict.TryGetValue(selfseat, out var actor)) return 0;

                return actor;
            }
        }

        /// <summary>
        /// 配置
        /// </summary>
        public ConfigKit config { get; private set; }

        /// <summary>
        /// 资源
        /// </summary>
        public GameResKit gameres { get; private set; }

        /// <summary>
        /// 随机器
        /// </summary>
        public RandomKit random { get; private set; }

        /// <summary>
        /// Ticker/时间驱动器, 同时是全局事件派发者 (ticker.eventor)
        /// </summary>
        public TickerKit ticker { get; private set; }

        /// <summary>
        /// U3D/输入
        /// </summary>
        public U3DKit u3dkit { get; private set; }

        /// <summary>
        /// 桶
        /// </summary>
        public RILBucket rilbucket { get; private set; }

        /// <summary>
        /// 眼睛/摄像机
        /// </summary>
        public Eyes eyes { get; private set; }

        /// <summary>
        /// 是否驱动渲染, 由导演在每个逻辑帧同步
        /// 未驱动时 Agent 不做追帧 (暂停/回滚期间画面保持不动)
        /// </summary>
        public bool rendering { get; set; }

        /// <summary>
        /// Agent 集合
        /// </summary>
        private Dictionary<ulong, Dictionary<Type, Agent>> agentdict { get; set; }

        /// <summary>
        /// 快照之后产生的 Agent
        /// </summary>
        private List<Agent> snapshotagents { get; set; }

        /// <summary>
        /// 创建世界, 全局唯一, 只需要创建一次
        /// </summary>
        /// <returns>世界</returns>
        public static World CreateWorld()
        {
            World world = new();
            world.world = world;
            world.parent = world;
            world.Create();

            return world;
        }

        /// <summary>
        /// 绑定节点
        /// </summary>
        private void BindRoot()
        {
            root = new GameObject("World");
            root.transform.localPosition = Vector3.zero;
            root.transform.localScale = Vector3.one;
        }

        protected override void OnCreate()
        {
            BindRoot();

            base.OnCreate();

            // 资源要先于配置创建 (WebGL 下配置需要走资源异步加载)
            gameres = AddNode<GameResKit>();
            gameres.Create();

            config = AddNode<ConfigKit>();
            config.Create();

            random = AddNode<RandomKit>();
            random.Initial(19491001);
            random.Create();

            ticker = AddNode<TickerKit>();
            ticker.Create();

            u3dkit = AddNode<U3DKit>();
            u3dkit.Create();

            rilbucket = AddNode<RILBucket>();
            rilbucket.Create();

            eyes = AddNode<Eyes>();
            eyes.Create();

            Batches();

            agentdict = ObjectPool.Ensure<Dictionary<ulong, Dictionary<Type, Agent>>>();
            snapshotagents = ObjectPool.Ensure<List<Agent>>();

            ticker.eventor.Listen<TickEvent>(OnTick);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ticker.eventor.UnListen<TickEvent>(OnTick);
            ClearAgents();
        }

        /// <summary>
        /// 创建批处理
        /// </summary>
        private void Batches()
        {

        }

        /// <summary>
        /// 开始一局, 先清理上一局残留的渲染单位与状态
        /// </summary>
        /// <param name="selfseat">我的座位号</param>
        /// <returns>世界</returns>
        public World Begin(ulong selfseat)
        {
            End();
            this.selfseat = selfseat;

            return this;
        }

        /// <summary>
        /// 结束一局, 回收本局的所有渲染单位与状态
        /// </summary>
        public void End()
        {
            rilbucket.LossAllRIL();
            ClearAgents();
            rendering = false;
        }

        /// <summary>
        /// 回收所有的 Agent
        /// </summary>
        private void ClearAgents()
        {
            foreach (var kv in agentdict)
            {
                foreach (var agent in kv.Value)
                {
                    agent.Value.Reset();
                    ObjectPool.Set(agent.Value);
                }

                kv.Value.Clear();
                ObjectPool.Set(kv.Value);
            }
            agentdict.Clear();

            snapshotagents.Clear();
        }

        /// <summary>
        /// 拍摄
        /// </summary>
        public void Snapshot()
        {
            snapshotagents.Clear();
        }

        /// <summary>
        /// 恢复
        /// </summary>
        public void Restore()
        {
            var agents = ObjectPool.Ensure<List<Agent>>();
            agents.AddRange(snapshotagents);
            foreach (var agent in agents) RmvAgent(agent);
            agents.Clear();
            ObjectPool.Set(agents);
            snapshotagents.Clear();
            foreach (var kv in agentdict)
            {
                foreach (var kv2 in kv.Value)
                {
                    kv2.Value.Flash();
                }
            }
        }

        /// <summary>
        /// 切换座位
        /// </summary>
        /// <param name="seat">座位</param>
        public void SwitchSeat(ulong seat)
        {
            selfseat = seat;
        }

        /// <summary>
        /// 获取 Agent, 如果不存在则创建
        /// </summary>
        /// <param name="actor">ActorID</param>
        /// <typeparam name="T">Agent 类型</typeparam>
        /// <returns>Agent</returns>
        public T EnsureAgent<T>(ulong actor) where T : Agent, new()
        {
            var agent = GetAgent<T>(actor);
            if (null == agent) agent = AddAgent<T>(actor);

            return agent;
        }

        /// <summary>
        /// 获取 Agent, 如果不存在则返回默认值
        /// </summary>
        /// <param name="actor">ActorID</param>
        /// <typeparam name="T">Agent 类型</typeparam>
        /// <returns>Agent</returns>
        public T GetAgent<T>(ulong actor) where T : Agent
        {
            if (false == agentdict.TryGetValue(actor, out var agents)) return default;
            if (false == agents.TryGetValue(typeof(T), out var agent)) return default;

            return agent as T;
        }

        /// <summary>
        /// 获取 Agent 集合
        /// </summary>
        /// <param name="actor">ActorID</param>
        /// <returns>Agent 集合</returns>
        public Dictionary<Type, Agent> GetAgents(ulong actor)
        {
            if (false == agentdict.TryGetValue(actor, out var agents)) return default;

            return agents;
        }

        /// <summary>
        /// 移除 Agents
        /// </summary>
        /// <param name="actor">ActorID</param>
        public void RmvAgent(ulong actor)
        {
            var dict = GetAgents(actor);
            if (null == dict) return;
            var agents = ObjectPool.Ensure<List<Agent>>();
            foreach (var kv in dict) agents.Add(kv.Value);
            foreach (var agent in agents) RmvAgent(agent);
            agents.Clear();
            ObjectPool.Set(agents);
        }

        /// <summary>
        /// 移除 Agent
        /// </summary>
        /// <param name="agent">Agent</param>
        public void RmvAgent(Agent agent)
        {
            var actor = agent.actor;
            if (false == agentdict.TryGetValue(actor, out var dict)) return;

            dict.Remove(agent.GetType());
            agent.Reset();
            ObjectPool.Set(agent);
            if (0 == dict.Count)
            {
                ObjectPool.Set(dict);
                agentdict.Remove(actor);
            }

            snapshotagents.Remove(agent);
        }

        /// <summary>
        /// 添加 Agent
        /// </summary>
        /// <param name="actor">ActorID</param>
        /// <typeparam name="T">Agent 类型</typeparam>
        /// <returns>Agent</returns>
        /// <exception cref="Exception">Agent 已存在</exception>
        private T AddAgent<T>(ulong actor) where T : Agent, new()
        {
            if (false == agentdict.TryGetValue(actor, out var dict))
            {
                agentdict.Add(actor, dict = ObjectPool.Ensure<Dictionary<Type, Agent>>());
            }

            if (dict.TryGetValue(typeof(T), out var agent)) throw new Exception($"agent {typeof(T)} already exists");

            agent = ObjectPool.Ensure<T>();
            agent.Ready(actor, this);
            dict.Add(typeof(T), agent);
            snapshotagents.Add(agent);

            return agent as T;
        }

        private void OnTick(TickEvent e)
        {
            // 未驱动渲染时 (暂停/回滚) 不做追帧
            if (false == rendering) return;

            // 执行过程中, 可能会触发修改 agentdict, 导致错误
            var agents = ObjectPool.Ensure<List<(Agent agents, float timescale)>>();
            foreach (var kv in agentdict)
            {
                float timescale = 1f;
                if (rilbucket.SeekRIL<RIL_TICKER>(kv.Key, out var ril)) timescale = ril.timescale * ConfigKit.Int2Float;
                foreach (var agent in kv.Value.Values)
                {
                    if (ChaseStatus.Arrived == agent.status) continue;
                    agents.Add((agent, timescale));
                }
            }

            // 收集后再处理
            foreach ((Agent agent, float timescale) in agents) agent.Chase(e.tick, timescale);
            agents.Clear();
            ObjectPool.Set(agents);
        }
    }
}
