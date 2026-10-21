using System;

namespace GameCore
{
    public abstract class FsmState<T> where T : class
    {
        /// <summary>
        /// 有限状态机状态初始化时调用。
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        protected internal virtual void OnInit(Fsm<T> sm)
        {
        }

        /// <summary>
        /// 有限状态机状态进入时调用。
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        protected internal virtual void OnEnter(Fsm<T> sm)
        {
        }

        /// <summary>
        /// 有限状态机状态轮询时调用。
        /// </summary>
        /// <param name="sm">有限状态机引用。</param>
        /// <param name="time">逻辑流逝时间，以秒为单位。</param>
        protected internal virtual void OnUpdate(Fsm<T> sm, float time)
        {
        }

        /// <summary>
        /// 有限状态机状态离开时调用。
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        /// <param name="isShutdown">是否是关闭有限状态机时触发。</param>
        protected internal virtual void OnExit(Fsm<T> sm)
        {
        }

        /// <summary>
        /// 有限状态机状态销毁时调用。
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        protected internal virtual void OnDestroy(Fsm<T> sm)
        {
        }

        /// <summary>
        /// 切换当前有限状态机状态。
        /// </summary>
        /// <typeparam name="TState">要切换到的有限状态机状态类型。</typeparam>
        /// <param name="fsm">有限状态机引用。</param>
        protected void ChangeState<TState>(Fsm<T> sm) where TState : FsmState<T>
        {
            if (sm == null)
            {
                throw new Exception("状态机不应该为空.");
            }

            sm.ChangeState<TState>();
        }

        /// <summary>
        /// 切换当前有限状态机状态。
        /// </summary>
        /// <param name="fsm">有限状态机引用。</param>
        /// <param name="stateType">要切换到的有限状态机状态类型。</param>
        protected void ChangeState(Fsm<T> sm, Type stateType)
        {
            if (sm == null)
            {
                throw new Exception("状态机不应该为空.");
            }

            if (stateType == null)
            {
                throw new Exception("状态类型不应该为空.");
            }

            if (!typeof(FsmState<T>).IsAssignableFrom(stateType))
            {
                throw new Exception($"状态类 '{stateType.FullName}' 不正确. 没有继承State");
            }

            sm.ChangeState(stateType);
        }
    }
}