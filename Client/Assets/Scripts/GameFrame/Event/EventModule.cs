using System;
using GameCore;

namespace GameFrame
{
    /// <summary>
    /// 事件中心
    /// </summary>
    public class EventModule : GameModule<EventModule>
    {
        private readonly EventPool m_EventPool = new();

        protected override void DynamicAwake()
        {
            base.DynamicAwake();
            transform.parent = GameModules.Instance.transform;
        }

        public bool Check(uint id, Action<object> handler)
        {
            return m_EventPool.Check(id, handler);
        }

        public void Subscribe(uint id, Action<object> handler)
        {
            m_EventPool.Subscribe(id, handler);
        }

        public void Unsubscribe(uint id, Action<object> handler)
        {
            m_EventPool.Unsubscribe(id, handler);
        }

        public void DispatchNextFrame(uint id, object data = null)
        {
            m_EventPool.DispatchNextFrame(id, data);
        }

        public void Dispatch(uint id, object data = null)
        {
            m_EventPool.Dispatch(id, data);
        }

        private void Update()
        {
            m_EventPool.Tick();
        }

        public void Clear()
        {
            m_EventPool.Clear();
        }
    }
}