using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameFrame
{
    public abstract class UIBase : MonoBehaviour
    {
        public GameObject GameObject { get; internal set; }
        public Transform Transform { get; internal set; }
        public virtual int Depth { get; internal set; }
        public virtual bool Visible { get; internal set; }
        protected object Data { get; set; }

        protected virtual void OnInit() { }
        protected virtual void OnShow() { }
        protected virtual void OnTick() { }
        protected virtual void OnHide() { }
        protected virtual void OnClear() { }

        protected virtual void OnDepthChange() { }

        internal Dictionary<GameObject, UIWidget> m_UIWidgetDict;

        /// <summary>
        /// 扫描并绑定子节点上已挂载的 UIWidget（预制件非动态创建的）
        /// 将它们纳入生命周期管理，与 CreateWidget 创建的行为一致
        /// </summary>
        internal void BindChildWidgets()
        {
            var widgets = Transform.GetComponentsInChildren<UIWidget>(true);
            if (widgets == null || widgets.Length == 0) return;

            for (int i = 0; i < widgets.Length; i++)
            {
                var widget = widgets[i];
                // 跳过自身（UIWindow 自身也继承 UIBase，但不应被当作子 Widget）
                if (widget.gameObject == GameObject) continue;

                RegisterWidget(widget);
            }
        }

        /// <summary>
        /// 注册一个 UIWidget 纳入生命周期管理（自动去重）
        /// 可供子类手动调用，也作为 BindChildWidgets 的统一注册入口
        /// </summary>
        protected void RegisterWidget(UIWidget widget)
        {
            m_UIWidgetDict ??= new();
            if (m_UIWidgetDict.ContainsKey(widget.gameObject)) return;

            widget.Bind(this, widget.gameObject);
            m_UIWidgetDict.Add(widget.gameObject, widget);
        }

        protected async UniTask<T> CreateWidget<T>(Transform parent, object data = null) where T : UIWidget
        {
            m_UIWidgetDict ??= new();
            var type = typeof(T);
            GameObject gameObject = await ResourceModule.Instance.InstantiateGameObjectAsync(type.Name, parent);
            UIWidget widget = gameObject.GetComponent<UIWidget>();
            if (widget == null)
            {
                throw new Exception($"{type.FullName} create fail!");
            }
            widget.Create(this, gameObject, data);
            m_UIWidgetDict.Add(gameObject, widget);
            return widget as T;
        }

        /// <summary>
        /// 根据路径查找子节点并获取组件
        /// </summary>
        protected T GetChildComponent<T>(string path) where T : UnityEngine.Component
        {
            var node = Transform.Find(path);
            if (node == null)
            {
                return null;
            }
            var component = node.GetComponent<T>();
            return component != null ? component : null;
        }

        protected void DestroyWidget(UIWidget widget)
        {
            widget.Destroy();
            m_UIWidgetDict.Remove(widget.GameObject);
        }

        /// <summary>
        /// 帧更新(仅State=SHOING)
        /// </summary>
        internal void UIUpdate()
        {
            if (m_UIWidgetDict != null && m_UIWidgetDict.Count > 0)
            {
                // 使用快照遍历，防止 OnTick 或 Widget.UIUpdate 中调用 DestroyWidget 修改字典
                var snapshot = new List<UIWidget>(m_UIWidgetDict.Values);
                foreach (var ui in snapshot)
                {
                    if (ui.GameObject != null)
                    {
                        ui.UIUpdate();
                    }
                }
            }
            OnTick();
        }
    }
}