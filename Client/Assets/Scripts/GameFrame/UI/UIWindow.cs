using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace GameFrame
{
    public abstract class UIWindow : UIBase
    {
        public abstract int Layer { get; }
        public abstract bool FullScreen { get; }

        private Canvas m_Canvas;
        private GraphicRaycaster m_Raycaster;
        private GraphicRaycaster[] m_ChildRaycaster;
        private int m_HideTimerId;

        /// <summary>
        /// 是否隐藏
        /// </summary>
        public bool IsHide { get; private set; }

        /// <summary>
        /// 窗口层级
        /// </summary>
        public sealed override int Depth
        {
            get
            {
                if (m_Canvas != null)
                {
                    return m_Canvas.sortingOrder;
                }
                else
                {
                    return 0;
                }
            }

            internal set
            {
                if (m_Canvas != null)
                {
                    if (m_Canvas.sortingOrder == value)
                    {
                        return;
                    }

                    m_Canvas.sortingOrder = value;

                    if (m_UIWidgetDict != null)
                    {
                        foreach (var widget in m_UIWidgetDict.Values)
                        {
                            widget.Depth = value;
                        }
                    }
                    OnDepthChange();
                }
            }
        }

        /// <summary>
        /// 窗口是否可见(和是否隐藏不同,例如在全屏界面后面的界面就是不可见,但是它并没有隐藏)
        /// </summary>
        public sealed override bool Visible
        {
            get
            {
                if (m_Canvas != null)
                {
                    return m_Canvas.gameObject.activeInHierarchy;
                }
                else
                {
                    return false;
                }
            }

            internal set
            {
                if (m_Canvas != null)
                {
                    m_Canvas.gameObject.SetActive(value);
                    Interactable = value;
                }
            }
        }

        /// <summary>
        /// 窗口的UI点击响应
        /// </summary>
        protected bool Interactable
        {
            get
            {
                if (m_Raycaster != null)
                {
                    return m_Raycaster.enabled;
                }
                else
                {
                    return false;
                }
            }

            set
            {
                if (m_Raycaster != null)
                {
                    m_Raycaster.enabled = value;
                    foreach (var graphicRaycaster in m_ChildRaycaster)
                    {
                        graphicRaycaster.enabled = value;
                    }
                }
            }
        }

        internal void Create(GameObject gameObject, object data)
        {
            if (gameObject == null)
            {
                throw new Exception($"{gameObject} load failed!");
            }

            base.GameObject = gameObject;
            base.Transform = gameObject.transform;
            base.Data = data;
            base.GameObject.layer = LayerMask.NameToLayer("UI");

            m_Canvas = GameObject.GetComponent<Canvas>();
            if (m_Canvas == null)
            {
                m_Canvas = GameObject.AddComponent<Canvas>();
            }
            m_Canvas.overrideSorting = true;
            m_Canvas.sortingOrder = 0;
            m_Canvas.sortingLayerName = "Default";

            m_Raycaster = GameObject.GetComponent<GraphicRaycaster>() ?? GameObject.AddComponent<GraphicRaycaster>();
            // 获取子节点的 Raycaster（排除自身，避免 Interactable setter 重复设置）
            var allRaycasters = GameObject.GetComponentsInChildren<GraphicRaycaster>();
            m_ChildRaycaster = Array.FindAll(allRaycasters, r => r != m_Raycaster);
            // 扫描并绑定预制件上已有的 UIWidget
            BindChildWidgets();
            OnInit();
        }

        internal void Show()
        {
            IsHide = false;
            CancelHideTimer();
            OnShow();
        }

        internal void Hide()
        {
            IsHide = true;
            m_HideTimerId = TimerModule.Instance.AddOnceTimer(UIModule.WINDOW_HIDE_TO_CLOSE, CloseSelf);
            OnHide();
        }

        internal void Destroy()
        {
            if (GameObject != null)
            {
                Object.Destroy(GameObject);
                GameObject = null;
            }
            if (m_UIWidgetDict != null)
            {
                foreach (var widget in m_UIWidgetDict.Values)
                {
                    widget.Destroy();
                }
            }
            CancelHideTimer();
            OnClear();
        }

        private void CancelHideTimer()
        {
            if (m_HideTimerId > 0)
            {
                TimerModule.Instance.RemoveTimer(m_HideTimerId);
                m_HideTimerId = 0;
            }
        }

        /// <summary>
        /// 隐藏自己
        /// </summary>
        protected void HideSelf()
        {
            UIModule.Instance.HideWindow(this);
        }

        /// <summary>
        /// 关闭自己
        /// </summary>
        protected void CloseSelf()
        {
            UIModule.Instance.CloseWindow(this);
        }
    }
}