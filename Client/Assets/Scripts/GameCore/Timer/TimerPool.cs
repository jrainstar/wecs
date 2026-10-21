using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCore
{
    public partial class TimerPool
    {
        private int m_CurTimerId;
        private readonly List<Timer> m_Timers = new List<Timer>();
        private readonly List<int> m_NeedRemoveTimerIds = new List<int>();

        /// <summary>
        /// 添加计时器
        /// </summary>
        /// <param name="time">间隔时间</param>
        /// <param name="times">运行次数，填-1为永远重复执行</param>
        /// <param name="callback">回调函数</param>
        /// <param name="isUnscaled">是否忽略游戏速度</param>
        /// <returns>计时器id</returns>
        public int AddTimer(float time, int times, Action callback, bool isUnscaled = false)
        {
            if (times == 0)
            {
                return 0;
            }
            Timer timer = new Timer();
            timer.TimerId = ++m_CurTimerId;
            timer.Callback = callback;
            timer.Time = time;
            timer.CurrentTime = 0;
            timer.RunCount = times;
            timer.IsRunning = true;
            timer.IsUnscaled = isUnscaled;
            timer.NeedRemove = false;
            m_Timers.Add(timer);
            return timer.TimerId;
        }

        /// <summary>
        /// 移除计时器
        /// </summary>
        /// <param name="timerId">计时器id</param>
        public void RemoveTimer(int timerId)
        {
            var timer = GetTimer(timerId);
            if (timer != null)
            {
                timer.NeedRemove = true;
            }
        }

        /// <summary>
        /// 移除所有计时器
        /// </summary>
        public void RemoveAllTimer()
        {
            m_Timers.Clear();
        }

        /// <summary>
        /// 暂停计时器
        /// </summary>
        /// <param name="timerId">计时器id</param>
        public void PauseTimer(int timerId)
        {
            var timer = GetTimer(timerId);
            if (timer != null)
            {
                timer.IsRunning = false;
            }
        }

        /// <summary>
        /// 恢复计时器
        /// </summary>
        /// <param name="timerId">计时器id</param>
        public void ResumeTimer(int timerId)
        {
            var timer = GetTimer(timerId);
            if (timer != null)
            {
                timer.IsRunning = true;
            }
        }

        /// <summary>
        /// 是否存在计时器
        /// </summary>
        /// <param name="timerId">计时器id</param>
        public bool ContainsTimer(int timerId)
        {
            return GetTimer(timerId) != null;
        }

        /// <summary>
        /// 获取计时器剩余时间
        /// </summary>
        /// <param name="timerId">计时器id</param>
        public float GetTimerLeft(int timerId)
        {
            var timer = GetTimer(timerId);
            if (timer != null)
            {
                return timer.Time - timer.CurrentTime;
            }
            return 0;
        }

        private Timer GetTimer(int timerId)
        {
            if (timerId == 0)
            {
                return null;
            }
            foreach (var timer in m_Timers)
            {
                if (timer.TimerId == timerId)
                {
                    return timer;
                }
            }

            return null;
        }

        public void Update()
        {
            if (this.m_Timers.Count == 0) return;

            for (var i = 0; i < m_Timers.Count; i++)
            {
                var tempTimer = m_Timers[i];

                if (tempTimer.NeedRemove)
                {
                    m_NeedRemoveTimerIds.Add(i);
                    continue;
                }
                if (!tempTimer.IsRunning) continue;

                tempTimer.CurrentTime += tempTimer.IsUnscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                if (tempTimer.CurrentTime >= tempTimer.Time)
                {
                    tempTimer.Callback?.Invoke();
                    tempTimer.RunCount--;
                    if (tempTimer.RunCount == 0)
                    {
                        m_NeedRemoveTimerIds.Add(i);
                    }
                    else
                    {
                        tempTimer.CurrentTime = 0;
                    }
                }
            }

            foreach (var index in m_NeedRemoveTimerIds)
            {
                m_Timers.RemoveAt(index);
            }
            m_NeedRemoveTimerIds.Clear();
        }

    }
}