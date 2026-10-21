using System;
using System.Collections.Generic;

namespace GameCore
{
    public class Fsm<T> where T : class
    {
        private T m_Owner;
        private Dictionary<Type, FsmState<T>> m_States = new();
        private FsmState<T> m_LastState;
        private FsmState<T> m_CurrentState;
        private float m_CurrentStateTime;
        private bool m_IsDestroyed;

        /// <summary>
        /// 获取有限状态机持有者。
        /// </summary>
        public T Owner
        {
            get
            {
                return m_Owner;
            }
        }

        /// <summary>
        /// 获取有限状态机持有者类型。
        /// </summary>
        public Type OwnerType
        {
            get
            {
                return typeof(T);
            }
        }

        /// <summary>
        /// 获取有限状态机中状态的数量。
        /// </summary>
        public int StateCount
        {
            get
            {
                return m_States.Count;
            }
        }

        /// <summary>
        /// 获取有限状态机是否正在运行。
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return m_CurrentState != null;
            }
        }

        /// <summary>
        /// 获取有限状态机是否被销毁。
        /// </summary>
        public bool IsDestroyed
        {
            get
            {
                return m_IsDestroyed;
            }
        }

        public FsmState<T> LastState
        {
            get
            {
                return m_LastState;
            }
        }

        /// <summary>
        /// 获取上个有限状态机状态名称。
        /// </summary>
        public string LastStateName
        {
            get
            {
                return m_LastState != null ? m_LastState.GetType().FullName : null;
            }
        }

        /// <summary>
        /// 获取上个有限状态机状态类型。
        /// </summary>
        public Type LastStateType
        {
            get
            {
                return m_LastState != null ? m_LastState.GetType() : null;
            }
        }

        /// <summary>
        /// 获取当前有限状态机状态。
        /// </summary>
        public FsmState<T> CurrentState
        {
            get
            {
                return m_CurrentState;
            }
        }

        /// <summary>
        /// 获取当前有限状态机状态名称。
        /// </summary>
        public string CurrentStateName
        {
            get
            {
                return m_CurrentState != null ? m_CurrentState.GetType().FullName : null;
            }
        }

        /// <summary>
        /// 获取上个有限状态机状态类型。
        /// </summary>
        public Type CurrentStateType
        {
            get
            {
                return m_CurrentState != null ? m_CurrentState.GetType() : null;
            }
        }

        /// <summary>
        /// 获取当前有限状态机状态持续时间。
        /// </summary>
        public float CurrentStateTime
        {
            get
            {
                return m_CurrentStateTime;
            }
        }

        public void Create(T owner, List<FsmState<T>> states)
        {
            this.m_Owner = owner;
            m_IsDestroyed = false;
            m_CurrentStateTime = 0;
            foreach (var state in states)
            {
                if (state == null)
                {
                    throw new Exception("状态类型不能为空.");
                }

                Type stateType = state.GetType();
                if (this.m_States.ContainsKey(stateType))
                {
                    throw new Exception($"状态机 '{typeof(T).FullName}'中 状态 '{stateType.FullName}' 已经存在.");
                }

                this.m_States.Add(stateType, state);
                state.OnInit(this);
            }
        }

        public void Clear()
        {
            if (m_CurrentState != null)
            {
                m_CurrentState.OnExit(this);
            }

            foreach (KeyValuePair<Type, FsmState<T>> state in m_States)
            {
                state.Value.OnDestroy(this);
            }

            m_Owner = null;
            m_States.Clear();

            m_LastState = null;
            m_CurrentState = null;
            m_CurrentStateTime = 0f;
            m_IsDestroyed = true;
        }

        /// <summary>
        /// 开始有限状态机。
        /// </summary>
        /// <typeparam name="TState">要开始的有限状态机状态类型。</typeparam>
        public void Start<TState>() where TState : FsmState<T>
        {
            if (IsRunning)
            {
                throw new Exception("状态机运行中 不能重复开启.");
            }

            FsmState<T> state = GetState<TState>();
            if (state == null)
            {
                throw new Exception($"状态机 不能从'{typeof(TState).FullName}'开始 因为它不存在.");
            }

            m_LastState = null;
            m_CurrentStateTime = 0f;
            m_CurrentState = state;
            m_CurrentState.OnEnter(this);
        }

        /// <summary>
        /// 开始有限状态机。
        /// </summary>
        /// <param name="stateType">要开始的有限状态机状态类型。</param>
        public void Start(Type stateType)
        {
            if (IsRunning)
            {
                throw new Exception("状态机运行中 不能重复开启.");
            }

            if (stateType == null)
            {
                throw new Exception("状态类型不能为空.");
            }

            if (!typeof(FsmState<T>).IsAssignableFrom(stateType))
            {
                throw new Exception($"状态类 '{stateType.FullName}' 不正确. 没有继承State<{typeof(T).FullName}>");
            }

            FsmState<T> state = GetState(stateType);
            if (state == null)
            {
                throw new Exception($"状态机 不能从'{stateType.FullName}'开始 因为它不存在.");
            }

            m_LastState = null;
            m_CurrentStateTime = 0f;
            m_CurrentState = state;
            m_CurrentState.OnEnter(this);
        }

        /// <summary>
        /// 是否存在有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要检查的有限状态机状态类型。</typeparam>
        /// <returns>是否存在有限状态机状态。</returns>
        public bool HasState<TState>() where TState : FsmState<T>
        {
            return m_States.ContainsKey(typeof(TState));
        }

        /// <summary>
        /// 是否存在有限状态机状态。
        /// </summary>
        /// <param name="stateType">要检查的有限状态机状态类型。</param>
        /// <returns>是否存在有限状态机状态。</returns>
        public bool HasState(Type stateType)
        {
            if (stateType == null)
            {
                throw new Exception("状态类不能为空.");
            }

            if (!typeof(FsmState<T>).IsAssignableFrom(stateType))
            {
                throw new Exception($"状态类 '{stateType.FullName}' 不正确. 没有继承State<{typeof(T).FullName}>");
            }

            return m_States.ContainsKey(stateType);
        }

        /// <summary>
        /// 获取有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要获取的有限状态机状态类型。</typeparam>
        /// <returns>要获取的有限状态机状态。</returns>
        public TState GetState<TState>() where TState : FsmState<T>
        {
            FsmState<T> state = null;
            if (m_States.TryGetValue(typeof(TState), out state))
            {
                return (TState)state;
            }

            return null;
        }

        /// <summary>
        /// 获取有限状态机状态。
        /// </summary>
        /// <param name="stateType">要获取的有限状态机状态类型。</param>
        /// <returns>要获取的有限状态机状态。</returns>
        internal FsmState<T> GetState(Type stateType)
        {
            if (stateType == null)
            {
                throw new Exception("状态类不能为空.");
            }

            if (!typeof(FsmState<T>).IsAssignableFrom(stateType))
            {
                throw new Exception($"状态类 '{stateType.FullName}' 不正确. 没有继承State<{typeof(T).FullName}>");
            }

            FsmState<T> state = null;
            if (m_States.TryGetValue(stateType, out state))
            {
                return state;
            }

            return null;
        }

        /// <summary>
        /// 获取有限状态机的所有状态。
        /// </summary>
        /// <returns>有限状态机的所有状态。</returns>
        public FsmState<T>[] GetAllStates()
        {
            int index = 0;
            FsmState<T>[] results = new FsmState<T>[m_States.Count];
            foreach (KeyValuePair<Type, FsmState<T>> state in m_States)
            {
                results[index++] = state.Value;
            }

            return results;
        }

        /// <summary>
        /// 获取有限状态机的所有状态。
        /// </summary>
        /// <param name="results">有限状态机的所有状态。</param>
        public void GetAllStates(List<FsmState<T>> results)
        {
            if (results == null)
            {
                throw new Exception("Results不能传空.");
            }

            results.Clear();
            foreach (KeyValuePair<Type, FsmState<T>> state in m_States)
            {
                results.Add(state.Value);
            }
        }

        /// <summary>
        /// 有限状态机轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public void Update(float time)
        {
            if (m_CurrentState == null)
            {
                return;
            }

            m_CurrentStateTime += time;
            m_CurrentState.OnUpdate(this, time);
        }

        /// <summary>
        /// 切换当前有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要切换到的有限状态机状态类型。</typeparam>
        public void ChangeState<TState>() where TState : FsmState<T>
        {
            ChangeState(typeof(TState));
        }

        /// <summary>
        /// 切换当前有限状态机状态。
        /// </summary>
        /// <param name="stateType">要切换到的有限状态机状态类型。</param>
        public void ChangeState(Type stateType)
        {
            if (m_CurrentState == null)
            {
                throw new Exception("状态类不能为空.");
            }

            FsmState<T> state = GetState(stateType);
            if (state == null)
            {
                throw new Exception($"状态机不能改变到 '{stateType.FullName}' 因为不存在.");
            }

            m_CurrentState.OnExit(this);
            m_CurrentStateTime = 0f;
            m_LastState = m_CurrentState;
            m_CurrentState = state;
            m_CurrentState.OnEnter(this);
        }
    }
}