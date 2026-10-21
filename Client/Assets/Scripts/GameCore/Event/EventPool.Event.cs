namespace GameCore
{
    public sealed partial class EventPool
    {
        /// <summary>
        /// 事件结点。
        /// </summary>
        private sealed class Event : IMemory
        {
            private uint m_Id;
            private object m_Data;

            public Event()
            {
                m_Id = 0;
                m_Data = null;
            }

            public uint Id
            {
                get
                {
                    return m_Id;
                }
            }

            public object Data
            {
                get
                {
                    return m_Data;
                }
            }

            public static Event Create(uint id, object data)
            {
                Event eventNode = MemoryPool.Acquire<Event>();
                eventNode.m_Id = id;
                eventNode.m_Data = data;
                return eventNode;
            }

            public void Clear()
            {
                m_Id = 0;
                m_Data = null;
            }
        }
    }
}
