using System;
using UnityEngine;

namespace GameEngine
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
                            GameObject singletonObject = new GameObject();
                            m_Instance = singletonObject.AddComponent<T>();
                            m_Instance.DynamicAwake();
                            singletonObject.name = typeof(T).Name;
                            // 可选：让单例对象在场景切换时不被销毁
                            //DontDestroyOnLoad(singletonObject);
                        }
                    }

                    return m_Instance;
                }
            }
        }

        protected virtual void DynamicAwake()
        {
            var module = new GameObject(typeof(T).Name);
            module.transform.parent = GameModules.Instance.transform;
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