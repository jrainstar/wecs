using GameCore;
using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 任务组
    /// </summary>
    public class TaskGroup : IMemory
    {
        /// <summary>
        /// 任务列表
        /// </summary>
        private LinkedList<Task> m_TaskList;



        private int m_TotalCount = 0;

        private int m_CurrCount = 0;

        /// <summary>
        /// 是否并发执行
        /// </summary>
        private bool m_IsParallel = false;

        /// <summary>
        /// 任务组完成
        /// </summary>
        public event Action OnComplete;

        public TaskGroup()
        {
            m_TaskList = new LinkedList<Task>();
        }

        public void AddTask(Task task)
        {
            m_TaskList.AddLast(task);
        }

        /// <summary>
        /// 清空所有任务
        /// </summary>
        public void ClearTask()
        {
            LinkedListNode<Task> task = m_TaskList.First;
            while (task != null)
            {
                var next = task.Next;
                MemoryPool.Release(task.Value);
                m_TaskList.Remove(task);
                task = next;
            }
        }

        public void OnUpdate()
        {
            LinkedListNode<Task> taskNode = m_TaskList.First;
            while (taskNode != null)
            {
                taskNode.Value.OnUpdate();
                taskNode = taskNode.Next;
            }
        }

        /// <summary>
        /// 执行任务
        /// </summary>
        public void Run(bool isConcurrency = false)
        {
            if (m_TaskList.Count == 0)
            {
                OnComplete?.Invoke();
                return;
            }

            m_IsParallel = isConcurrency;

            if (m_IsParallel)
            {
                ParallelTask();
            }
            else
            {
                SequenceTask();
            }
        }

        /// <summary>
        /// 检查任务
        /// </summary>
        private void SequenceTask()
        {
            LinkedListNode<Task> curr = m_TaskList.First;
            if (curr != null)
            {
                curr.Value.OnComplete += () =>
                {
                    m_CurrCount++;
                    m_TaskList.Remove(curr);
                    SequenceTask();
                };
                curr.Value.Start();
            }
            else
            {
                OnComplete?.Invoke();
                MemoryPool.Release(this);
                MemoryPool.Release(this);
            }
        }

        /// <summary>
        /// 并发执行任务
        /// </summary>
        private void ParallelTask()
        {
            m_TotalCount = m_TaskList.Count;
            m_CurrCount = 0;

            LinkedListNode<Task> taskNode = m_TaskList.First;
            while (taskNode != null)
            {
                LinkedListNode<Task> next = taskNode.Next;
                taskNode.Value.Start();
                taskNode.Value.OnComplete += ()=> 
                {
                    m_CurrCount++;
                    if (m_CurrCount == m_TotalCount)
                    {
                        OnComplete?.Invoke();
                        MemoryPool.Release(this);
                        MemoryPool.Release(this);
                    }
                };
                taskNode = next;
            }
        }

        public void Clear()
        {
            m_TaskList.Clear();
            OnComplete = null;
        }
    }
}