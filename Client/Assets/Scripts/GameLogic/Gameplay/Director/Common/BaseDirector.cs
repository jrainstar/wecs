using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;
using GameLogic.Gameplay.View;
using System;
using System.Threading;

namespace GameLogic.Gameplay.Director
{
    /// <summary>
    /// 导演, 负责指挥游戏的运行, 包括创建游戏, 销毁游戏, 开始游戏, 暂停游戏, 恢复游戏, 停止游戏等操作
    /// 驱动渲染层 World, 渲染状态的接收以及传入 World
    /// </summary>
    public abstract class BaseDirector : Node
    {
        /// <summary>
        /// 是否渲染 (驱动 World)
        /// </summary>
        public abstract bool rendering { get; }
        /// <summary>
        /// 游戏数据
        /// </summary>
        protected BuildData data { get; private set; }
        /// <summary>
        /// 是否多线程
        /// </summary>
        public bool multithread { get; private set; }
        /// <summary>
        /// 子线程
        /// </summary>
        private Thread thread { get; set; }
        /// <summary>
        /// 逻辑 Step 耗时
        /// </summary>
        public int stepms { get; private set; }
        /// <summary>
        /// 是否已经创建了游戏
        /// </summary>
        private bool created { get; set; }

        /// <summary>
        /// 创建游戏
        /// </summary>
        /// <param name="data">游戏数据</param>
        public void CreateGame(BuildData data, bool multithread = false)
        {
            this.data = data;
            this.multithread = multithread;
            // 世界是全局唯一的根, 这里只切换本局的战斗内容
            world.Begin(data.seat);
            OnCreateGame();
            created = true;
            world.rendering = rendering;

            world.ticker.eventor.Listen<TickEvent>(OnTick);
            if (false == multithread)
            {
                world.ticker.eventor.Listen<FixedTickEvent>(OnFixedTick);
                return;
            }
            thread = new Thread(() =>
            {
                int logicms = (int)GAME_DEFINE.LOGIC_TICK_MS;
                while (true)
                {
                    var ms = DateTime.Now.Millisecond;
                    OnStep();
                    stepms = Math.Clamp(DateTime.Now.Millisecond - ms, 0, int.MaxValue);

                    if (stepms <= logicms)
                    {
                        Thread.Sleep(logicms - stepms);
                    }
                }
            });
            thread.Start();
        }

        /// <summary>
        /// 销毁游戏
        /// </summary>
        public void DestroyGame()
        {
            if (false == created) return;
            created = false;

            world.ticker.eventor.UnListen<TickEvent>(OnTick);
            if (false == multithread)
            {
                world.ticker.eventor.UnListen<FixedTickEvent>(OnFixedTick);
            }
            else if (null != thread)
            {
                try
                {
                    thread.Abort();
                }
                catch (Exception e)
                {
                    // ignored
                }

                thread = null;
            }

            OnDestroyGame();
            world.End();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            // 组件被直接销毁时也要把本局收干净
            DestroyGame();
        }

        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGame()
        {
            OnStartGame();
            world.rendering = rendering;
        }

        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            OnPauseGame();
            world.rendering = rendering;
        }

        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            OnResumeGame();
            world.rendering = rendering;
        }

        /// <summary>
        /// 停止游戏
        /// </summary>
        public void StopGame()
        {
            OnStopGame();
            world.rendering = rendering;
        }

        /// <summary>
        /// 快照
        /// </summary>
        public void Snapshot()
        {
            OnSnapshot();
        }

        /// <summary>
        /// 恢复
        /// </summary>
        public void Restore()
        {
            OnRestore();
            world.rendering = rendering;
        }

        protected void OnTick(TickEvent e)
        {
            // 导演的监听晚于 World 注册, 这里同步开关后 World 本帧就能拿到最新值
            world.rendering = rendering;
            if (false == rendering) return;
            OnTick();
        }

        protected void OnFixedTick(FixedTickEvent e)
        {
            stepms = (int)(e.tick * 1000);
            OnStep();
            // 逻辑帧结束时同步渲染开关, 本帧后续的渲染 Tick 才能拿到最新值
            world.rendering = rendering;
        }

        /// <summary>
        /// 创建游戏
        /// </summary>
        protected abstract void OnCreateGame();
        /// <summary>
        /// 销毁游戏
        /// </summary>
        protected abstract void OnDestroyGame();
        /// <summary>
        /// 开始游戏
        /// </summary>
        protected abstract void OnStartGame();
        /// <summary>
        /// 暂停游戏
        /// </summary>
        protected abstract void OnPauseGame();
        /// <summary>
        /// 恢复游戏
        /// </summary>
        protected abstract void OnResumeGame();
        /// <summary>
        /// 停止游戏
        /// </summary>
        protected abstract void OnStopGame();
        /// <summary>
        /// 快照
        /// </summary>
        protected abstract void OnSnapshot();
        /// <summary>
        /// 恢复
        /// </summary>
        protected abstract void OnRestore();
        /// <summary>
        /// 渲染层驱动 (单线程)
        /// </summary>
        protected abstract void OnTick();
        /// <summary>
        /// 逻辑层驱动 (根据配置决定单线程还是多线程)
        /// </summary>
        protected abstract void OnStep();
    }
}