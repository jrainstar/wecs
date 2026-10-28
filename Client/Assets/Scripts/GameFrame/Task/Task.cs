using GameCore;
using System;

namespace GameFrame
{
    /// <summary>
    /// 任务执行器
    /// </summary>
    public class Task : IMemory
    {
        /// <summary>
        /// 具体的任务
        /// </summary>
        public event Action CurrTask;

        /// <summary>
        /// 任务完成
        /// </summary>
        public event Action OnComplete;

        /// <summary>
        /// 是否完成
        /// </summary>
        public bool IsComplete { get; private set; }

        /// <summary>
        /// 进入任务
        /// </summary>
        public void Start()
        {
            IsComplete = false;

            if (CurrTask != null)
            {
                CurrTask.Invoke();
            }
            else
            {
                Complete();
            }
        }

        public void OnUpdate()
        {
            if (IsComplete)
            {
                OnComplete?.Invoke();
                CurrTask = null;
                OnComplete = null;
                IsComplete = false;
                MemoryPool.Release(this);
            }
        }

        /// <summary>
        /// 离开任务
        /// </summary>
        public void Complete()
        {
            IsComplete = true;
        }

        public void Clear()
        {

        }
    }
}