using System;

namespace GameCore
{
    public partial class TimerPool
    {
        private class Timer
        {
            public int TimerId;
            public Action Callback;
            public float Time;
            public float CurrentTime;
            public int RunCount;
            public bool IsRunning;
            public bool IsUnscaled;
            public bool NeedRemove;
        }
    }
}