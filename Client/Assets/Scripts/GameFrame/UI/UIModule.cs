using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameCore;

namespace GameFrame
{
    public class UIModule : GameModule<UIModule>
    {
        internal const int LAYER_DEEP = 1000;
        internal const int WINDOW_DEEP = 100;
        internal const int WINDOW_HIDE_TO_CLOSE = 10;

        [SerializeField] 
        private Transform m_Root;
        public Transform Root => m_Root;

        [SerializeField]
        private Camera m_UICamera;
        public Camera UICamera => m_UICamera;


        [SerializeField]
        private Camera m_UIRoleCamera;
        public Camera UIRoleCamera => m_UIRoleCamera;

        private Canvas m_Canvas;
        public Canvas Canvas => m_Canvas;


        private readonly List<UIWindow> m_WindowList = new();
        private readonly Dictionary<Type, UIWindow> m_WindowDict = new();

        public event Action<UIWindow> OnWindowShow;
        public event Action<UIWindow> OnWindowHide;
        public event Action<UIWindow> OnWindowClose;

        protected override void Awake()
        {
            base.Awake();
            m_Canvas = m_Root.GetComponent<Canvas>();
            // m_UICamera = m_Root.Find("UICamera").GetComponent<Camera>();
        }

        #region 设置安全区域

        /// <summary>
        /// 设置屏幕安全区域（异形屏支持）。
        /// </summary>
        /// <param name="safeRect">安全区域</param>
        public static void ApplyScreenSafeRect(Rect safeRect)
        {
            CanvasScaler scaler = UIModule.Instance.Root.GetComponentInParent<CanvasScaler>();
            if (scaler == null)
            {
                Log.Error($"Not found {nameof(CanvasScaler)} !");
                return;
            }

            // Convert safe area rectangle from absolute pixels to UGUI coordinates
            float rateX = scaler.referenceResolution.x / Screen.width;
            float rateY = scaler.referenceResolution.y / Screen.height;
            float posX = (int)(safeRect.position.x * rateX);
            float posY = (int)(safeRect.position.y * rateY);
            float width = (int)(safeRect.size.x * rateX);
            float height = (int)(safeRect.size.y * rateY);

            float offsetMaxX = scaler.referenceResolution.x - width - posX;
            float offsetMaxY = scaler.referenceResolution.y - height - posY;

            // 注意：安全区坐标系的原点为左下角	
            var rectTrans = UIModule.Instance.Root as RectTransform;
            if (rectTrans != null)
            {
                rectTrans.offsetMin = new Vector2(posX, posY); //锚框状态下的屏幕左下角偏移向量
                rectTrans.offsetMax = new Vector2(-offsetMaxX, -offsetMaxY); //锚框状态下的屏幕右上角偏移向量
            }
        }

        /// <summary>
        /// 模拟IPhoneX异形屏
        /// </summary>
        public static void SimulateIPhoneXNotchScreen()
        {
            Rect rect;
            if (Screen.height > Screen.width)
            {
                // 竖屏Portrait
                float deviceWidth = 1125;
                float deviceHeight = 2436;
                rect = new Rect(0f / deviceWidth, 102f / deviceHeight, 1125f / deviceWidth, 2202f / deviceHeight);
            }
            else
            {
                // 横屏Landscape
                float deviceWidth = 2436;
                float deviceHeight = 1125;
                rect = new Rect(132f / deviceWidth, 63f / deviceHeight, 2172f / deviceWidth, 1062f / deviceHeight);
            }

            Rect safeArea = new Rect(Screen.width * rect.x, Screen.height * rect.y, Screen.width * rect.width, Screen.height * rect.height);
            ApplyScreenSafeRect(safeArea);
        }

        #endregion


        #region Window

        /// <summary>
        /// 获取窗口
        /// </summary>
        public T GetWindow<T>() where T : UIWindow
        {
            return GetWindow(typeof(T)) as T;
        }

        public UIWindow GetWindow(Type type)
        {
            m_WindowDict.TryGetValue(type, out var window);
            return window;
        }

        public List<UIWindow> GetAllWindow()
        {
            return m_WindowList;
        }

        public T ShowWindowSync<T>(object data = null) where T : UIWindow
        {
            return ShowWindowTmpSync<T>(data);
        }

        public T ShowWindowTmpSync<T>(object data = null) where T : UIWindow
        {
            UIWindow window = null;
            try
            {
                var type = typeof(T);

                long startTicks = 0;

                var loading = CheckWindowLoading<T>();

                if (loading)
                {
                    Log.Warning($"Window {type.Name} are loading... , dont reload in loading!!!.");
                    return (T)window;
                }

                window = GetWindow<T>();

                if (window == null)
                {
                    startTicks = DateTime.Now.Ticks;

                    try
                    {
                        AddWindowLoading<T>();
                        GameObject gameObject = ResourceModule.Instance.Instantiate<GameObject>(type.Name, Root);
                        window = gameObject.GetComponent<UIWindow>();

                        if (window == null)
                        {
                            throw new Exception($"Window {type.FullName} create instance failed!");
                        }

                        Push(window);

                        window.Create(gameObject, data);
                    }
                    finally
                    {
                        RemoveWindowLoading<T>();
                    }
                }
                else
                {
                    Pop(window);
                    Push(window);
                }

                window.Show();

                SortWindowsVisible();
                SortWindowsDepth(window.Layer);

                var endTicks = DateTime.Now.Ticks;

                PrintUseTime(window, startTicks, endTicks);

                OnWindowShow?.Invoke(window);
            }

            catch (Exception e)
            {
                Log.Error(e);
                // 清理已 Push 但未完成创建的窗口，避免僵尸状态
                if (window != null && m_WindowList.Contains(window))
                {
                    Pop(window);
                    window.Destroy();
                }
            }
            finally { }
            return window as T;
        }

        /// <summary>
        /// 打开窗口(需要等待)
        /// </summary>
        public async UniTask<T> AwaitShowWindowAsync<T>(object data = null) where T : UIWindow
        {
            return await ShowWindowTmpAsync<T>(data);
        }

        /// <summary>
        /// 打开窗口(不需要等待)
        /// </summary>
        public void ShowWindowAsync<T>(object data = null) where T : UIWindow
        {
            ShowWindowTmpAsync<T>(data).Forget();
        }

        private List<Type> m_WindowLoading = new List<Type>();

        private void AddWindowLoading<T>() where T : UIWindow
        {
            AddWindowLoading(typeof(T));
        }

        private void AddWindowLoading(Type type)
        {
            m_WindowLoading.Add(type);
        }

        private void RemoveWindowLoading<T>() where T : UIWindow
        {
            RemoveWindowLoading(typeof(T));
        }

        private void RemoveWindowLoading(Type type)
        {
            m_WindowLoading.Remove(type);
        }

        /// <summary>
        /// 检测窗口加载
        /// </summary>
        public bool CheckWindowLoading<T>() where T : UIWindow
        {
            return CheckWindowLoading(typeof(T));
        }

        public bool CheckWindowLoading(Type type)
        {
            foreach (var windowType in m_WindowLoading)
            {
                if (windowType == type)
                {
                    return true;
                }
            }
            return false;
        }

        private async UniTask<T> ShowWindowTmpAsync<T>(object data = null) where T : UIWindow
        {
            UIWindow window = null;
            try
            {
                var type = typeof(T);

                long startTicks = 0;

                var loading = CheckWindowLoading<T>();

                if (loading)
                {
                    Log.Warning($"Window {type.Name} are loading... , dont reload in loading!!!.");
                    return (T)window;
                }

                window = GetWindow<T>();

                if (window == null)
                {
                    startTicks = DateTime.Now.Ticks;

                    try
                    {
                        AddWindowLoading<T>();
                        GameObject gameObject = await ResourceModule.Instance.InstantiateGameObjectAsync(type.Name, Root);
                        window = gameObject.GetComponent<UIWindow>();

                        if (window == null)
                        {
                            throw new Exception($"Window {type.FullName} create instance failed!");
                        }

                        Push(window);

                        window.Create(gameObject, data);
                    }
                    finally
                    {
                        RemoveWindowLoading<T>();
                    }
                }
                else
                {
                    Pop(window);
                    Push(window);
                }

                window.Show();

                SortWindowsVisible();
                SortWindowsDepth(window.Layer);

                var endTicks = DateTime.Now.Ticks;

                PrintUseTime(window, startTicks, endTicks);

                OnWindowShow?.Invoke(window);
            }
            catch (Exception e)
            {
                Log.Error(e);
                // 清理已 Push 但未完成创建的窗口，避免僵尸状态
                if (window != null && m_WindowList.Contains(window))
                {
                    Pop(window);
                    window.Destroy();
                }
            }
            finally { }
            return window as T;
        }

        /// <summary>
        /// 隐藏窗口
        /// </summary>
        public void HideWindow<T>() where T : UIWindow
        {
            HideWindow(typeof(T));
        }

        public void HideWindow(UIWindow window)
        {
            HideWindow(window.GetType());
        }

        public void HideWindow(Type type)
        {
            UIWindow window = GetWindow(type);
            if (window != null)
            {
                if (window.IsHide)
                {
                    return;
                }

                window.Visible = false;
                window.Hide();

                if (window.FullScreen)
                {
                    SortWindowsVisible();
                }

                OnWindowHide?.Invoke(window);
            }
        }

        /// <summary>
        /// 关闭窗口
        /// </summary>
        public void CloseWindow<T>() where T : UIWindow
        {
            CloseWindow(typeof(T));
        }

        public void CloseWindow(UIWindow window)
        {
            CloseWindow(window.GetType());
        }

        public void CloseWindow(Type type)
        {
            UIWindow window = GetWindow(type);
            if (window != null)
            {
                window.Destroy();
                Pop(window);
                if (window.FullScreen)
                {
                    SortWindowsVisible();
                }

                OnWindowClose?.Invoke(window);
            }
        }

        /// <summary>
        /// 关闭除了withoutList意外的所有窗口
        /// </summary>
        /// <param name="withoutList"></param>
        public void CloseAll(List<Type> withoutList = null)
        {
            for (int i = m_WindowList.Count - 1; i >= 0; i--)
            {
                UIWindow window = m_WindowList[i];
                if (withoutList != null && withoutList.Contains(window.GetType()))
                {
                    continue;
                }
                window.Destroy();
                Pop(window);
                OnWindowClose?.Invoke(window);
            }
            SortWindowsVisible();
        }

        /// <summary>
        /// 关闭除了type意外的所有窗口
        /// </summary>
        /// <param name="type"></param>
        public void CloseAll(Type type)
        {
            CloseAll(new List<Type>() { type });
        }

        private void SortWindowsDepth(int layer)
        {
            int depth = layer * LAYER_DEEP;
            var index = 0;
            foreach (var window in m_WindowList)
            {
                if (window.IsHide)
                {
                    continue;
                }
                if (window.Layer == layer)
                {
                    window.Depth = depth;
                    depth += WINDOW_DEEP;
                }
                //#if UNITY_EDITOR
                window.Transform.SetSiblingIndex(index++);
#if UNITY_EDITOR
                window.GameObject.name = $"{window.GetType().Name}_[{!window.Visible}]";
#endif
            }
        }

        private void SortWindowsVisible()
        {
            bool isHideNext = false;
            for (int i = m_WindowList.Count - 1; i >= 0; i--)
            {
                UIWindow window = m_WindowList[i];

                if (window.IsHide)
                {
                    continue;
                }

                if (isHideNext == false)
                {
                    window.Visible = true;
                    if (window.FullScreen)
                    {
                        isHideNext = true;
                    }
                }
                else
                {
                    window.Visible = false;
                }
            }
        }


        private void Push(UIWindow window)
        {
            if (m_WindowList.Contains(window))
                throw new System.Exception($"Window {window.GetType().FullName} is exist.");

            int insertIndex = -1;
            for (int i = 0; i < m_WindowList.Count; i++)
            {
                if (window.Layer == m_WindowList[i].Layer)
                {
                    insertIndex = i + 1;
                }
            }

            if (insertIndex == -1)
            {
                for (int i = 0; i < m_WindowList.Count; i++)
                {
                    if (window.Layer > m_WindowList[i].Layer)
                    {
                        insertIndex = i + 1;
                    }
                }
            }

            if (insertIndex == -1)
            {
                insertIndex = 0;
            }

            m_WindowList.Insert(insertIndex, window);
            m_WindowDict[window.GetType()] = window;
        }

        private void Pop(UIWindow window)
        {
            if (window != null)
            {
                m_WindowList.Remove(window);
                m_WindowDict.Remove(window.GetType());
            }
        }


        private void PrintUseTime(UIWindow window, long startTicks, long endTicks)
        {
            if (startTicks == 0)
            {
                return;
            }
            var useTime = (endTicks - startTicks) / 10000f / 1000f;
            var str = $"Show {window.GetType().Name} UseTime {useTime:0.###}s";
            if (useTime >= 0.2f)
            {
                Log.Warning(str + " is too long!");
            }
            else
            {
                Log.Debug(str);
            }
        }

        #endregion

        internal void Update()
        {
            for (int i = 0; i < m_WindowList.Count; i++)
            {
                if (!m_WindowList[i].IsHide)
                {
                    m_WindowList[i].UIUpdate();
                }
            }
        }

        internal void Clear()
        {
            OnWindowShow = null;
            OnWindowHide = null;
            OnWindowClose = null;
            CloseAll();
        }
    }
}