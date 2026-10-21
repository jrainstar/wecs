using System;
using System.Collections.Generic;
using GameCore;

namespace GameEngine
{
    /// <summary>
    /// 任务管理器
    /// </summary>
    public class TaskModule : GameModule<TaskModule>
    {
        /// <summary>
        /// 任务组列表
        /// </summary>
        private readonly LinkedList<TaskGroup> m_TaskGroupList = new();

        protected override void Awake()
        {
            base.Awake();
        }

        public void Update()
        {
            LinkedListNode<TaskGroup> taskGroup = m_TaskGroupList.First;
            while (taskGroup != null)
            {
                taskGroup.Value.OnUpdate();
                taskGroup = taskGroup.Next;
            }
        }

        /// <summary>
        /// 创建一个任务组
        /// </summary>
        /// <returns></returns>
        public TaskGroup CreateTaskGroup()
        {
            TaskGroup taskGroup = MemoryPool.Acquire<TaskGroup>();
            m_TaskGroupList.AddLast(taskGroup);
            return taskGroup;
        }

        /// <summary>
        /// 移除任务组
        /// </summary>
        /// <param name="taskGroup"></param>
        public void RemoveTaskGroup(TaskGroup taskGroup)
        {
            m_TaskGroupList.Remove(taskGroup);
        }

        /// <summary>
        /// 创建任务执行器
        /// </summary>
        /// <returns></returns>
        public Task CreateTask()
        {
            return MemoryPool.Acquire<Task>();
        }
    }
}