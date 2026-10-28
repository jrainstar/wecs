using System;
using GameFrame;

namespace GameLogic
{
    /// <summary>
    /// EventModule 扩展 —— 支持 EventDefine 枚举直接作为事件 ID。
    /// </summary>
    public static class EventModuleExtensions
    {
        public static void Subscribe(this EventModule module, EventDefine id, Action<object> handler)
        {
            module.Subscribe((uint)id, handler);
        }

        public static void Unsubscribe(this EventModule module, EventDefine id, Action<object> handler)
        {
            module?.Unsubscribe((uint)id, handler);
        }

        public static void Dispatch(this EventModule module, EventDefine id, object data = null)
        {
            module.Dispatch((uint)id, data);
        }
    }
}
