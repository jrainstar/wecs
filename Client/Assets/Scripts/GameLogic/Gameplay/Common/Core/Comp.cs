using System;
using System.Collections.Generic;
using System.Linq;


namespace GameLogic.Gameplay.Common
{
    /// <summary>
    /// 组件
    /// </summary>
    public abstract class Comp
    {
        /// <summary>
        /// 销毁了
        /// </summary>
        public bool Destroyed { get; private set; } = false;

        /// <summary>
        /// 引擎
        /// </summary>
        public Engine Engine { get; set; }

        /// <summary>
        /// 父组件
        /// </summary>
        public Comp Parent { get; set; }

        /// <summary>
        /// 组件列表
        /// </summary>
        private List<Comp> Comps { get; set; }

        /// <summary>
        /// 组件字典，根据组件类型分类
        /// </summary>
        private Dictionary<Type, List<Comp>> CompDict { get; set; }

        /// <summary>
        /// 创建一个 Goblin 对象
        /// </summary>
        public void Create()
        {
            OnCreate();
        }

        /// <summary>
        /// 销毁一个 对象
        /// </summary>
        public void Destroy()
        {
            if (Destroyed) return;
            Destroyed = true;

            OnDestroy();
            Parent.RmvComp(this);

            if (null != Comps)
            {
                for (int i = Comps.Count - 1; i >= 0; i--)
                {
                    Comps[i].Destroy();
                }
                Comps.Clear();
                CompDict.Clear();
            }
        }

        protected virtual void OnCreate()
        {
        }

        protected virtual void OnDestroy()
        {

        }

        /// <summary>
        /// 获得一个组件，如果存在多个，将会取到最新那个
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件</returns>
        public T GetComp<T>(bool force = false) where T : Comp
        {
            if (false == CompDict.TryGetValue(typeof(T), out var comps)) return null;

            return comps.Last() as T;
        }

        /// <summary>
        /// 添加组件
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件</returns>
        public T AddComp<T>() where T : Comp, new()
        {
            if (null == Comps) Comps = new();
            if (null == CompDict) CompDict = new();

            T comp = new();
            comp.Engine = Engine;
            comp.Parent = this;
            if (false == CompDict.TryGetValue(typeof(T), out var list))
            {
                list = new();
                CompDict.Add(typeof(T), list);
            }
            list.Add(comp);
            Comps.Add(comp);

            return comp;
        }

        /// <summary>
        /// 移除组件
        /// </summary>
        /// <param name="comp">组件</param>
        public void RmvComp(Comp comp)
        {
            if (false == Comps.Contains(comp)) return;

            if (CompDict.TryGetValue(comp.GetType(), out var list)) list.Remove(comp);
            Comps.Remove(comp);
        }
    }
}
