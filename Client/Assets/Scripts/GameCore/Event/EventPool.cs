using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    /// <summary>
    /// 事件池
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    public sealed partial class EventPool
    {
        #region 字段与属性

        /// <summary>
        /// 事件码与对应处理方法的字典
        /// </summary>
        private Dictionary<uint, Action<object>> m_EventHandlerDict;

        /// <summary>
        /// 事件队列
        /// </summary>
        private Queue<Event> m_Events;

        #endregion

        #region 构造方法
        public EventPool()
        {
            m_EventHandlerDict = new Dictionary<uint, Action<object>>();
            m_Events = new Queue<Event>();
        }
        #endregion

        #region 事件的订阅与取消
        /// <summary>
        /// 检查订阅事件处理方法是否存在
        /// </summary>
        public bool Check(uint id, Action<object> handler)
        {
            if (handler == null)
            {
                Debug.LogError("事件处理方法为空");
                return false;
            }

            Action<object> eventHandlers = null;
            if (!m_EventHandlerDict.TryGetValue(id, out eventHandlers))
            {
                return false;
            }

            if (eventHandlers == null)
            {
                return false;
            }

            //遍历委托里的所有方法
            foreach (Action<object> i in eventHandlers.GetInvocationList())
            {
                if (i == handler)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        public void Subscribe(uint id, Action<object> handler)
        {
            if (handler == null)
            {
                Debug.LogError("事件处理方法为空，无法订阅");
                return;
            }

            Action<object> eventHandler = null;
            //检查是否获取处理方法失败或获取到的为空
            if (!m_EventHandlerDict.TryGetValue(id, out eventHandler) || eventHandler == null)
            {
                m_EventHandlerDict[id] = handler;
            }
            //不为空，就检查是否处理方法重复了
            else if (Check(id, handler))
            {
                Debug.LogError("要订阅事件的处理方法已存在");
            }
            else
            {
                eventHandler += handler;
                m_EventHandlerDict[id] = eventHandler;
            }
        }

        /// <summary>
        /// 取消订阅事件
        /// </summary>
        public void Unsubscribe(uint id, Action<object> handler)
        {
            if (handler == null)
            {
                Debug.LogError("事件处理方法为空，无法取消订阅");
                return;
            }

            if (m_EventHandlerDict.ContainsKey(id))
            {
                m_EventHandlerDict[id] -= handler;
            }
        }

        #endregion

        #region 事件抛出
        /// <summary>
        /// 抛出事件（线程安全）
        /// </summary>
        /// <param name="sender">事件源</param>
        /// <param name="e">事件参数</param>
        public void DispatchNextFrame(uint id, object data = null)
        {
            //将事件源和事件参数封装为Event加入队列
            Event eventNode = Event.Create(id, data);
            lock (m_Events)
            {
                m_Events.Enqueue(eventNode);
            }
        }

        /// <summary>
        /// 抛出事件（线程不安全）
        /// </summary>
        /// <param name="id">事件源</param>
        /// <param name="data">事件参数</param>
        public void Dispatch(uint id, object data = null)
        {
            HandleEvent(id, data);
        }
        #endregion

        #region 事件处理
        /// <summary>
        /// 事件池轮询（用于处理线程安全的事件）
        /// </summary>
        public void Tick()
        {
            lock (m_Events)
            {
                while (m_Events.Count > 0)
                {
                    Event eventNode = m_Events.Dequeue();
                    HandleEvent(eventNode.Id, eventNode.Data);
                }
            }
        }

        /// <summary>
        /// 处理事件
        /// </summary>
        /// <param name="sender">事件来源</param>
        /// <param name="e">事件参数</param>
        private void HandleEvent(uint id, object data = null)
        {
            //尝试获取事件的处理方法
            Action<object> handlers = null;
            if (m_EventHandlerDict.TryGetValue(id, out handlers))
            {
                if (handlers != null)
                {
                    handlers(data);
                }
                else
                {
                    Debug.LogError("事件没有对应处理方法：" + id);
                }
            }
        }
        #endregion

        #region 清理与关闭
        /// <summary>
        /// 清理事件。
        /// </summary>
        public void Clear()
        {
            lock (m_Events)
            {
                m_Events.Clear();
                m_EventHandlerDict.Clear();
            }
        }
        #endregion
    }

}