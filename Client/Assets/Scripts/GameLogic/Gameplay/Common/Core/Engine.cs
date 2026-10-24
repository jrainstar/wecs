
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
        public EventBus Eventor { get; private set; }
        /// <summary>
        /// 随机器
        /// </summary>
        public Random Random { get; private set; }
        /// <summary>
        /// 引擎 Tick
        /// </summary>
        public Ticker Ticker { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            Eventor = AddComp<EventBus>();
            Eventor.Create();

            Random = AddComp<Random>();
            Random.Initial(19491001);
            Random.Create();

            Ticker = AddComp<Ticker>();
            Ticker.Create();

        }

        /// <summary>
        /// 创建一个游戏引擎
        /// </summary>
        /// <returns>游戏引擎组件</returns>   
        public static Engine CreateEngine()
        {
            Engine engine = new();
            engine.Engine = engine;
            engine.Parent = engine;
            engine.Create();

            return engine;
        }
    }
}
