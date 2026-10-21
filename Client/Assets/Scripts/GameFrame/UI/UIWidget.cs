using UnityEngine;
using Object = UnityEngine.Object;

namespace GameEngine
{
    public class UIWidget : UIBase
    {
        /// <summary>
        /// 父窗口
        /// </summary>
        protected UIBase ParentUI { get; private set; }

        /// <summary>
        /// 是否为 Bind 方式绑定（预制件已有，非动态创建）
        /// Bind 的 Widget 销毁时不 Destroy GameObject
        /// </summary>
        private bool m_IsBound;

        public sealed override int Depth
        {
            get => ParentUI.Depth;
            internal set => OnDepthChange();
        }

        public sealed override bool Visible
        {
            get => GameObject.activeSelf;
            internal set
            {
                if (GameObject.activeSelf && value == true)
                {
                    return;
                }
                if (!GameObject.activeSelf && value == false)
                {
                    return;
                }
                GameObject.SetActive(value);
            }
        }

        internal void Create(UIBase uiBase, GameObject gameObject, object data = null)
        {
            ParentUI = uiBase;
            base.GameObject = gameObject;
            base.Transform = gameObject.transform;
            base.Data = data;
            Visible = true;
            OnInit();
            OnShow();
        }

        /// <summary>
        /// 绑定已存在于预制件上的 UIWidget 到父 UI 的生命周期
        /// 与 Create 的区别：不销毁 GameObject（因为它是预制件的一部分）
        /// </summary>
        internal void Bind(UIBase uiBase, GameObject gameObject)
        {
            if (ParentUI != null) return; // 防止重复绑定
            ParentUI = uiBase;
            base.GameObject = gameObject;
            base.Transform = gameObject.transform;
            m_IsBound = true;
            Visible = gameObject.activeSelf;
            OnInit();
            OnShow();
        }

        internal void Destroy()
        {
            // 递归销毁子 Widget
            if (m_UIWidgetDict != null)
            {
                foreach (var widget in m_UIWidgetDict.Values)
                {
                    widget.Destroy();
                }
                m_UIWidgetDict.Clear();
            }
            // Bind 方式绑定的 Widget 不销毁 GameObject（随预制件一起销毁）
            // Create 方式创建的 Widget 需要手动销毁
            if (!m_IsBound && GameObject != null)
            {
                Object.Destroy(GameObject);
                GameObject = null;
            }
            OnHide();
            OnClear();
        }
    }
}