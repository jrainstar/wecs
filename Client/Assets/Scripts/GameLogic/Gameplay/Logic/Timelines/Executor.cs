using GameConf.Gameplay;

namespace GameLogic.Gameplay.Logic
{
    public abstract class Executor
    {
        /// <summary>
        /// 舞台
        /// </summary>
        protected Stage stage { get; private set; }

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="stage">场景</param>
        /// <returns>指令执行器</returns>
        public Executor Load(Stage stage)
        {
            this.stage = stage;

            return this;
        }

        /// <summary>
        /// 卸载
        /// </summary>
        public void Unload()
        {
            stage = null;
        }

        public abstract void HandleEvent(TimelineEvent timelineevent);
    }

    public abstract class Executor<T> : Executor where T : TimelineEvent
    {
        public override void HandleEvent(TimelineEvent timelineevent)
        {
            var t = timelineevent as T;
            if (t == null)
                return;
            HandleEvent(t);
        }

        public abstract void HandleEvent(T timelineevent);
    }
}