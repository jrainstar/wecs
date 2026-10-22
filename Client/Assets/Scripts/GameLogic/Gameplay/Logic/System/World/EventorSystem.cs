using GameLogic.Gameplay.Shared;
using System;
using System.Collections.Generic;


namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 事件接口
    /// </summary>
    public interface IEvent { }

    /// <summary>
    /// 事件订阅派发者
    /// </summary>
    public class EventorSystem : System<EventorComponent>
    {
        private Dictionary<Type, List<(uint index, System system, Delegate action)>> EventDict { get; set; }

        protected override void OnAssemble()
        {
            base.OnAssemble();
            EventDict = ObjectCache.Ensure<Dictionary<Type, List<(uint index, System system, Delegate)>>>();
        }

        protected override void OnDisassemble()
        {
            base.OnDisassemble();
            if (null == EventDict) return;
            foreach (var kv in EventDict)
            {
                kv.Value.Clear();
                ObjectCache.Set(kv.Value);
            }
            EventDict.Clear();
            ObjectCache.Set(EventDict);
        }

        /// <summary>
        /// 注销事件监听
        /// </summary>
        /// <typeparam name="T">事件的结构体</typeparam>
        /// <param name="func">事件的回调</param>
        public void UnListen<T>(System system, Action<T> func) where T : IEvent
        {
            if (false == EventDict.TryGetValue(typeof(T), out var funcs)) return;
            var systemHash = system.GetHashCode();
            if (false == Component.Indexes.TryGetValue((systemHash, system.Entity), out var index)) return;
            funcs.Remove((index, system, func));
            Component.Indexes.Remove((systemHash, system.Entity));
        }

        /// <summary>
        /// 注册事件监听
        /// </summary>
        /// <typeparam name="T">事件的结构体</typeparam>
        /// <param name="func">事件的回调</param>
        public void Listen<T>(System system, Action<T> func) where T : IEvent
        {
            if (false == EventDict.TryGetValue(typeof(T), out var funcs))
            {
                funcs = ObjectCache.Ensure<List<(uint index, System system, Delegate)>>();
                EventDict.Add(typeof(T), funcs);
            }

            var systemHash = system.GetHashCode();
            bool notsort = false;
            if (false == Component.Indexes.TryGetValue((systemHash, system.Entity), out var index))
            {
                Component.Increment = Component.Increment + 1;
                index = Component.Increment;
                Component.Indexes.Add((systemHash, system.Entity), index);
                notsort = true;
            }

            if (funcs.Contains((index, system, func))) return;
            funcs.Add((index, system, func));

            if (notsort) return;
            funcs.Sort((funca, funcb) => funca.index.CompareTo(funcb.index));
        }

        /// <summary>
        /// 派发事件
        /// </summary>
        /// <typeparam name="T">事件的结构体</typeparam>
        /// <param name="e">事件的参数</param>
        public void Tell<T>(T e = default) where T : IEvent
        {
            if (null == EventDict) return;
            if (false == EventDict.TryGetValue(typeof(T), out var funcs)) return;
            for (int i = funcs.Count - 1; i >= 0; i--)
            {
                var func = funcs[i];
                if (false == func.system.Enable) continue;

                (func.action as Action<T>).Invoke(e);
            }
        }
    }
}