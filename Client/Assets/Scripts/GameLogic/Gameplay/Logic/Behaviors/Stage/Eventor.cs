using GameLogic.Gameplay.Common;
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
    public class Eventor : Behavior<EventorInfo>
    {
        private Dictionary<Type, List<(uint index, Behavior behavior, Delegate action)>> EventDict { get; set; }

        protected override void OnAssemble()
        {
            base.OnAssemble();
            EventDict = ObjectCache.Ensure<Dictionary<Type, List<(uint index, Behavior system, Delegate)>>>();
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
        public void UnListen<T>(Behavior system, Action<T> func) where T : IEvent
        {
            if (false == EventDict.TryGetValue(typeof(T), out var funcs)) return;
            var systemHash = system.GetHashCode();
            if (false == info.indexes.TryGetValue((systemHash, system.actor), out var index)) return;
            funcs.Remove((index, system, func));
            info.indexes.Remove((systemHash, system.actor));
        }

        /// <summary>
        /// 注册事件监听
        /// </summary>
        /// <typeparam name="T">事件的结构体</typeparam>
        /// <param name="func">事件的回调</param>
        public void Listen<T>(Behavior system, Action<T> func) where T : IEvent
        {
            if (false == EventDict.TryGetValue(typeof(T), out var funcs))
            {
                funcs = ObjectCache.Ensure<List<(uint index, Behavior system, Delegate)>>();
                EventDict.Add(typeof(T), funcs);
            }

            var systemHash = system.GetHashCode();
            bool notsort = false;
            if (false == info.indexes.TryGetValue((systemHash, system.actor), out var index))
            {
                info.increment = info.increment + 1;
                index = info.increment;
                info.indexes.Add((systemHash, system.actor), index);
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
                if (false == func.behavior.active) continue;

                (func.action as Action<T>).Invoke(e);
            }
        }
    }
}