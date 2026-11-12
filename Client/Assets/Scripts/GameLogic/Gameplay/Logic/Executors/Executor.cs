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
        /// 事件上下文: 施法者
        /// </summary>
        protected ulong caster { get; private set; }
        /// <summary>
        /// 事件上下文: 目标, 0 表示没有目标
        /// </summary>
        protected ulong target { get; private set; }

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
            caster = 0;
            target = 0;
        }

        /// <summary>
        /// 带上下文派发
        /// 事件本身只描述"做什么", 作用于谁由上下文给出;
        /// 执行器是单例被所有派发方共享, 上下文每次派发覆盖, 不能跨事件保存
        /// </summary>
        /// <param name="timelineevent">配置事件</param>
        /// <param name="caster">施法者</param>
        /// <param name="target">目标, 0 表示没有目标</param>
        public void HandleEvent(GameEvent timelineevent, ulong caster, ulong target)
        {
            this.caster = caster;
            this.target = target;
            HandleEvent(timelineevent);
        }

        public abstract void HandleEvent(GameEvent timelineevent);
    }

    public abstract class Executor<T> : Executor where T : GameEvent
    {
        public override void HandleEvent(GameEvent timelineevent)
        {
            var t = timelineevent as T;
            if (t == null)
                return;
            HandleEvent(t);
        }

        public abstract void HandleEvent(T timelineevent);
    }
}
