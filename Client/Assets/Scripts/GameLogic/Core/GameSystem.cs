using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 游戏系统基类。
    /// 生命周期：
    ///   OnInit → OnEnter → OnEnterAsync(可选异步) → OnUpdate/OnFixedUpdate ...
    ///        → OnLeave → OnStateChangeFinish(切换收尾) → OnClear
    /// </summary>
    public abstract class GameSystem : MonoBehaviour
    {
        public virtual void OnInit() { }
        public virtual void OnClear() { }

        /// <summary>进入状态（同步）</summary>
        public virtual void OnEnter() { }

        /// <summary>进入状态的异步流程（可选，默认立即完成）</summary>
        public virtual UniTask OnEnterAsync() => UniTask.CompletedTask;

        /// <summary>离开状态（同步）</summary>
        public virtual void OnLeave() { }

        /// <summary>状态切换收尾（切换完成后通知本次新进入的系统）</summary>
        public virtual void OnStateChangeFinish() { }

        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
    }


    /// <summary>
    /// 游戏框架组件抽象类，提供单例属性供外部使用。
    /// </summary>
    public abstract class GameSystem<T> : GameSystem where T : GameSystem<T>
    {
        private static T instance;
        private static bool applicationIsQuitting = false;
        private static readonly object @lock = new object();

        public static T Instance
        {
            get
            {
                if (applicationIsQuitting)
                {
                    // Debug.LogWarning($"[MonoSingleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again.");
                    return null;
                }

                lock (@lock)
                {
                    if (instance == null)
                    {
                        instance = FindAnyObjectByType<T>();

                        if (instance == null)
                        {
                            GameObject singletonObject = new GameObject();
                            instance = singletonObject.AddComponent<T>();
                            instance.DynamicAwake();
                            singletonObject.name = typeof(T).Name;
                            // 可选：让单例对象在场景切换时不被销毁
                            //DontDestroyOnLoad(singletonObject);
                        }
                    }

                    return instance;
                }
            }
        }


        protected virtual void DynamicAwake()
        {

        }

        public override void OnInit()
        {
            base.OnInit();

            if (instance == null)
            {
                instance = this as T;
                // DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Debug.LogWarning($"Another instance of {typeof(T)} already exists. Destroying this one.");
                Destroy(gameObject);
            }
        }

        public override void OnClear()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            applicationIsQuitting = true;

        }
    }
}
