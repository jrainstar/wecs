using GameLogic.Gameplay.View;
using System;
using System.Collections.Generic;
using System.Linq;


namespace GameLogic.Gameplay.Common
{
    /// <summary>
    /// 节点, 对象树的通用基类
    /// 只负责三件事: 父子关系 / 生命周期 (Create-Destroy) / 根引用 (world)
    /// </summary>
    public abstract class Node
    {
        /// <summary>
        /// 销毁了
        /// </summary>
        public bool destroyed { get; private set; } = false;

        /// <summary>
        /// 世界/根节点
        /// </summary>
        public World world { get; set; }

        /// <summary>
        /// 父节点
        /// </summary>
        public Node parent { get; set; }

        /// <summary>
        /// 子节点列表
        /// </summary>
        private List<Node> children { get; set; }

        /// <summary>
        /// 子节点字典, 根据节点类型分类
        /// </summary>
        private Dictionary<Type, List<Node>> childdict { get; set; }

        /// <summary>
        /// 创建一个节点
        /// </summary>
        public void Create()
        {
            OnCreate();
        }

        /// <summary>
        /// 销毁一个节点
        /// </summary>
        public void Destroy()
        {
            if (destroyed) return;
            destroyed = true;

            OnDestroy();
            if (null != parent) parent.RmvNode(this);

            if (null != children)
            {
                for (int i = children.Count - 1; i >= 0; i--)
                {
                    children[i].Destroy();
                }
                children.Clear();
                childdict.Clear();
            }
        }

        protected virtual void OnCreate()
        {
        }

        protected virtual void OnDestroy()
        {

        }

        /// <summary>
        /// 获得一个子节点，如果存在多个，将会取到最新那个
        /// </summary>
        /// <typeparam name="T">节点类型</typeparam>
        /// <returns>节点</returns>
        public T GetNode<T>(bool force = false) where T : Node
        {
            if (null == childdict || false == childdict.TryGetValue(typeof(T), out var nodes)) return null;

            return nodes.Last() as T;
        }

        /// <summary>
        /// 添加子节点
        /// </summary>
        /// <typeparam name="T">节点类型</typeparam>
        /// <returns>节点</returns>
        public T AddNode<T>() where T : Node, new()
        {
            if (null == children) children = new();
            if (null == childdict) childdict = new();

            T node = new();
            node.world = world;
            node.parent = this;
            if (false == childdict.TryGetValue(typeof(T), out var list))
            {
                list = new();
                childdict.Add(typeof(T), list);
            }
            list.Add(node);
            children.Add(node);

            return node;
        }

        /// <summary>
        /// 移除子节点
        /// </summary>
        /// <param name="node">节点</param>
        public void RmvNode(Node node)
        {
            if (null == children) return;
            if (false == children.Contains(node)) return;

            if (childdict.TryGetValue(node.GetType(), out var list)) list.Remove(node);
            children.Remove(node);
        }
    }
}
