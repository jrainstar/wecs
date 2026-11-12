using System;
using UnityEngine;

namespace GameFrame
{
    public abstract class GameModule : MonoBehaviour
    {
        /// <summary>
        /// 游戏框架组件初始化。
        /// </summary>
        protected virtual void Awake()
        {
            GameModules.RegisterModule(this);
        }
    }

    /// <summary>
    /// 游戏框架组件抽象类，提供单例属性供外部使用。
    /// </summary>
    public abstract class GameModule<T> : GameModule where T : GameModule<T>
    {
        private static T m_Instance;
        private static bool m_ApplicationIsQuitting = false;
        private static readonly object m_Lock = new();

        public static T Instance
        {
            get
            {
                if (m_ApplicationIsQuitting)
                {
                    // Debug.LogWarning($"[MonoSingleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again.");
                    return null;
                }

                lock (m_Lock)
                {
                    if (m_Instance == null)
                    {
                        m_Instance = FindAnyObjectByType<T>();

                        if (m_Instance == null)
                        {
                            GameObject singletonObject = new GameObject(typeof(T).Name);
                            m_Instance = singletonObject.AddComponent<T>();
                            m_Instance.DynamicAwake();
                            // 可选：让单例对象在场景切换时不被销毁
                            //DontDestroyOnLoad(singletonObject);
                        }
                    }

                    return m_Instance;
                }
            }
        }

        /// <summary>
        /// 动态创建时的初始化（仅 Instance 自动创建时调用），默认挂到 GameModules 节点下。
        /// </summary>
        protected virtual void DynamicAwake()
        {
            // GameModules 自身的挂点就是自己，避免把自己 parent 给自己
            if (this is GameModules)
            {
                return;
            }

            GameModules modules = GameModules.Instance;
            if (modules != null)
            {
                transform.SetParent(modules.transform, false);
            }
        }

        protected override void Awake()
        {
            base.Awake();

            if (m_Instance == null)
            {
                m_Instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (m_Instance != this)
            {
                Debug.LogWarning($"Another instance of {typeof(T)} already exists. Destroying this one.");
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (m_Instance == this)
            {
                m_Instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            m_ApplicationIsQuitting = true;

        }
    }
}