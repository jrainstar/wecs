using GameConf.Gameplay;

namespace GameLogic.Gameplay.Logic
{
    public class PrintExecutor : Executor<PrintEvent>
    {
        public override void HandleEvent(PrintEvent timelineevent)
        {
            var type = timelineevent.Type;
            var message = timelineevent.Print;

            switch (type)
            {
                case 0:
                    UnityEngine.Debug.Log(message);
                    break;
                case 1:
                    UnityEngine.Debug.LogWarning(message);
                    break;
                case 2:
                    UnityEngine.Debug.LogError(message);
                    break;
            }
        }
    }
}