
namespace GameLogic.Gameplay.Common
{
    /// <summary>
    /// 游戏引擎组件
    /// </summary>
    public class Engine : Comp
    {
        /// <summary>
        /// 事件
        /// </summary>
        public EventBus eventor { get; private set; }
        /// <summary>
        /// 随机器
        /// </summary>
        public Random random { get; private set; }
        /// <summary>
        /// 引擎 Tick
        /// </summary>
        public Ticker ticker { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            eventor = AddComp<EventBus>();
            eventor.Create();

            random = AddComp<Random>();
            random.Initial(19491001);
            random.Create();

            ticker = AddComp<Ticker>();
            ticker.Create();

        }

        /// <summary>
        /// 创建一个游戏引擎
        /// </summary>
        /// <returns>游戏引擎组件</returns>   
        public static Engine CreateEngine()
        {
            Engine engine = new();
            engine.engine = engine;
            engine.parent = engine;
            engine.Create();

            return engine;
        }
    }
}
