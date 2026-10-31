using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;
using GameLogic.Gameplay.View;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Gameplay.Director
{
    /// <summary>
    /// 本地单机, 本地导演
    /// </summary>
    public class LocalDirector : BaseDirector
    {
        /// <summary>
        /// 是否渲染 (驱动 World)
        /// </summary>
        public override bool rendering
        {
            get
            {
                if (restoreing) return false;
                if (null == stage || StageState.Ticking != stage.state) return false;

                return true;
            }
        }

        /// <summary>
        /// 时间缩放
        /// </summary>
        public float timescale => stage.timescale.AsFloat();
        /// <summary>
        /// 逻辑场景
        /// </summary>
        private Stage stage { get; set; }
        /// <summary>
        /// 是否正在恢复
        /// </summary>
        private bool restoreing { get; set; } = false;
        /// <summary>
        /// 同步锁对象
        /// </summary>
        private readonly object @lock = new();
        /// <summary>
        /// RIL 队列
        /// </summary>
        private readonly Queue<IRIL> rilqueue = new();
        /// <summary>
        /// RIL_DIFF 队列
        /// </summary>
        private readonly Queue<IRIL_DIFF> diffqueue = new();
        /// <summary>
        /// RIL 事件队列
        /// </summary>
        private readonly Queue<IRIL_EVENT> eventqueue = new();

        protected override void OnCreateGame()
        {
            // 初始化逻辑层
            stage = new Stage().Initialize(data.sdata);
            // 监听 RIL 渲染状态
            stage.onril += OnRIL;
            // 监听 RIL_DIFF 渲染状态
            stage.ondiff += OnDiff;
            // 监听 RIL 事件
            stage.onevent += OnEvent;
        }

        protected override void OnDestroyGame()
        {
            // 销毁逻辑层
            stage.Dispose();
            // 取消监听 RIL 渲染状态
            stage.onril -= OnRIL;
            // 取消监听 RIL_DIFF 渲染状态
            stage.ondiff -= OnDiff;
            // 取消监听 RIL 事件
            stage.onevent -= OnEvent;
        }

        protected override void OnStartGame() => stage.Start();
        protected override void OnPauseGame() => stage.Pause();
        protected override void OnResumeGame() => stage.Resume();
        protected override void OnStopGame() => stage.Stop();

        protected override void OnSnapshot()
        {
            world.Snapshot();
            stage.Snapshot();
        }

        protected override void OnRestore()
        {
            restoreing = true;
            world.rilbucket.LossAllRIL();
            lock (@lock)
            {
                while (rilqueue.TryDequeue(out var ril))
                {
                    ril.Reset();
                    RILCache.Set(ril);
                }

                while (diffqueue.TryDequeue(out var diff))
                {
                    diff.Reset();
                    RILCache.Set(diff);
                }

                while (eventqueue.TryDequeue(out var e))
                {
                    e.Reset();
                    RILCache.Set(e);
                }
            }

            world.Restore();
        }

        protected override void OnTick()
        {
            if (restoreing) return;

            lock (@lock)
            {
                while (rilqueue.TryDequeue(out var ril)) world.rilbucket.SetRIL(ril);
                while (diffqueue.TryDequeue(out var diff)) world.rilbucket.SetDiff(diff);
                while (eventqueue.TryDequeue(out var e)) world.rilbucket.SetEvent(e);
            }
        }
 
        protected override void OnStep()
        {
            // 如果正在恢复, 则直接恢复
            if (restoreing)
            {
                stage.Restore();
                restoreing = false;

                return;
            }

            if (null == stage || StageState.Ticking != stage.state) return;

            //while (world.input.TryDequeueCommand(out var command))
            //{
            //    stage.SetCommand(command);
            //    command.Reset();
            //    ObjectPool.Set(command);
            //}

            var joystick = engine.u3dkit.input.GetInput(INPUT_DEFINE.JOYSTICK);
            stage.SetInput(world.selfseat, INPUT_DEFINE.JOYSTICK, joystick.press, ToWorldDire(joystick.dire));

            //var ba = world.input.GetInput(INPUT_DEFINE.BA);
            //stage.SetInput(world.selfseat, INPUT_DEFINE.BA, ba.press, ba.dire);

            stage.Step();

        }

        /// <summary>
        /// 把摇杆的相机相对方向转换为世界方向
        /// 摇杆的 x 为相机右方, y 为相机前方, 绕相机偏航角旋转后即为世界方向
        /// </summary>
        /// <param name="dire">摇杆方向 (相机相对)</param>
        /// <returns>世界方向</returns>
        private IntVector2 ToWorldDire(IntVector2 dire)
        {
            if (0 == dire.x && 0 == dire.y) return dire;
            if (false == rendering) return dire;

            var eyes = world.eyes;
            if (null == eyes) return dire;

            var rad = eyes.yaw * Mathf.Deg2Rad;
            var sin = Mathf.Sin(rad);
            var cos = Mathf.Cos(rad);
            var x = dire.x * cos + dire.y * sin;
            var y = dire.y * cos - dire.x * sin;

            return new IntVector2(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
        }

        /// <summary>
        /// 处理 RIL 渲染状态
        /// </summary>
        /// <param name="ril">RIL 渲染状态</param>
        private void OnRIL(IRIL ril)
        {
            lock (@lock)
            {
                rilqueue.Enqueue(ril);
            }
        }

        /// <summary>
        /// 处理 RIL_DIFF 渲染状态
        /// </summary>
        /// <param name="diff">差异状态</param>
        private void OnDiff(IRIL_DIFF diff)
        {
            lock (@lock)
            {
                diffqueue.Enqueue(diff);
            }
        }

        /// <summary>
        /// 处理 RIL 事件
        /// </summary>
        /// <param name="e">RIL 事件</param>
        private void OnEvent(IRIL_EVENT e)
        {
            lock (@lock)
            {
                eventqueue.Enqueue(e);
            }
        }
    }
}