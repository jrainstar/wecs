namespace GameCore
{
    public static class Log
    {
        public static void Debug(object message)
        {
            UnityEngine.Debug.Log(message?.ToString() ?? "null");
        }

        public static void Info(object message)
        {
            UnityEngine.Debug.Log(message?.ToString() ?? "null");
        }

        public static void Warning(object message)
        {
            UnityEngine.Debug.LogWarning(message?.ToString() ?? "null");
        }

        public static void Error(object message)
        {
            UnityEngine.Debug.LogError(message.ToString() ?? "null");
        }
    }
}