namespace GameCore
{
    public static class TimeWitch
    {
        public static long StartWatch()
        {
            return System.DateTime.Now.Ticks;
        }
        public static float ElapseTime(long startTicks)
        {
            var endTicks = System.DateTime.Now.Ticks;
            return (endTicks - startTicks) / 10000f / 1000f;
        }
    }
}