
namespace GameLogic.Gameplay.Common
{
    /// <summary>
    /// 游戏引擎组件
    /// </summary>
    public class Engine : Comp
    {
        /// <summary>
        /// 配置
        /// </summary>
        public ConfigKit config { get; private set; }
        /// <summary>
        /// 资源
        /// </summary>
        public GameResKit gameres { get; private set; }
        /// <summary>
        /// 事件
        /// </summary>
        public EventorKit eventor { get; private set; }
        /// <summary>
        /// 随机器
        /// </summary>
        public RandomKit random { get; private set; }
        /// <summary>
        /// 引擎 Tick
        /// </summary>
        public TickerKit ticker { get; private set; }
        /// <summary>
        /// U3D
        /// </summary>
        public U3DKit u3dkit { get; private set; }


        protected override void OnCreate()
        {
            base.OnCreate();

            config = AddComp<ConfigKit>();
            config.Create();
            
            gameres = AddComp<GameResKit>();
            gameres.Create();

            eventor = AddComp<EventorKit>();
            eventor.Create();

            random = AddComp<RandomKit>();
            random.Initial(19491001);
            random.Create();

            ticker = AddComp<TickerKit>();
            ticker.Create();


            u3dkit = AddComp<U3DKit>();
            u3dkit.Create();
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
