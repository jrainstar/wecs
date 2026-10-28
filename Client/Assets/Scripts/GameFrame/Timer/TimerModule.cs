using System;
using GameCore;

namespace GameFrame
{
    /// <summary>
    /// 计时器
    /// </summary>
    public class TimerModule : GameModule<TimerModule>
    {
        private readonly TimerPool m_TimerPool = new();

        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// 运行一次的定时器
        /// </summary>
        public int AddOnceTimer(float time, Action fun, bool isIgnoreTimeScale = false)
        {
            return m_TimerPool.AddTimer(time, 1, fun, isIgnoreTimeScale);
        }

        /// <summary>
        /// 执行多次的定时器
        /// </summary>
        public int AddRepeatTimer(float time, int runCount, Action fun, bool isIgnoreTimeScale = false)
        {
            return m_TimerPool.AddTimer(time, runCount, fun, isIgnoreTimeScale);
        }

        /// <summary>
        /// 永远执行的计时器
        /// </summary>
        public int AddForeverTimer(float time, Action fun, bool isIgnoreTimeScale = false)
        {
            return m_TimerPool.AddTimer(time, -1, fun, isIgnoreTimeScale);
        }

        /// <summary>
        /// 获取定时器剩余时间
        /// </summary>
        public float GetTimeLeft(int timerId)
        {
            return m_TimerPool.GetTimerLeft(timerId);
        }

        /// <summary>
        /// 暂停定时器
        /// </summary>
        public void PauseTimer(int timerId)
        {
            m_TimerPool.PauseTimer(timerId);
        }

        /// <summary>
        /// 恢复定时器
        /// </summary>
        public void ResumeTimer(int timerId)
        {
            m_TimerPool.ResumeTimer(timerId);
        }

        /// <summary>
        /// 查找函数是否以加入计时器
        /// </summary>
        public bool ContainsTimer(int timerId)
        {
            return m_TimerPool.ContainsTimer(timerId);
        }

        /// <summary>
        /// 删除定时器
        /// </summary>
        public void RemoveTimer(int timerId)
        {
            m_TimerPool.RemoveTimer(timerId);
        }

        /// <summary>
        /// 删除所有计时器
        /// </summary>
        public void RemoveAllTimer()
        {
            m_TimerPool.RemoveAllTimer();
        }

        internal void Update()
        {
            m_TimerPool.Update();
        }
    }
}