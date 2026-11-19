using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;
using GameLogic.Gameplay.Replay;
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
        /// 当前逻辑帧号
        /// </summary>
        public uint frame => null != stage ? stage.frame : 0;
        /// <summary>
        /// 是否录像回放中 (回放时不下发本地输入)
        /// </summary>
        public bool replaying => null != player;
        /// <summary>
        /// 录像录制器, 为空表示不录像
        /// </summary>
        public ReplayRecorder recorder { get; set; }
        /// <summary>
        /// 录像播放器, 不为空表示回放模式, 玩家无法操控
        /// </summary>
        public ReplayPlayer player { get; set; }
        /// <summary>
        /// 逻辑场景
        /// </summary>
        private Stage stage { get; set; }
        /// <summary>
        /// 碰撞盒可视化 (调试用, 可在运行时关闭)
        /// </summary>
        private ColliderDrawer colliderdrawer { get; set; }
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
            // 碰撞盒可视化; 多线程模式下逻辑在子线程运行, 不做跨线程读取
            if (false == multithread) colliderdrawer = ColliderDrawer.Attach(stage, world.root.transform);
        }

        protected override void OnDestroyGame()
        {
            // 销毁碰撞盒可视化
            if (null != colliderdrawer)
            {
                UnityEngine.Object.Destroy(colliderdrawer.gameObject);
                colliderdrawer = null;
            }
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
            ClearQueues();
            world.Restore();
        }

        /// <summary>
        /// 录像回拖/跳转
        /// 逻辑从开局快照(帧 0)重新推演到目标帧, 快进期间不产生渲染表现,
        /// 到最后再按当前逻辑状态全量重推一次渲染指令
        /// 只能在录像回放 (player 不为空) 且已拍摄开局快照时使用
        /// </summary>
        /// <param name="targetframe">目标帧号</param>
        /// <returns>是否成功</returns>
        public bool Seek(uint targetframe)
        {
            if (null == stage || null == player) return false;
            if (StageState.Ticking != stage.state) return false;
            if (false == stage.hassnapshot) return false;

            // 回滚到开局快照: 清渲染层状态与队列, 再恢复 World / Stage
            world.rilbucket.LossAllRIL();
            ClearQueues();
            world.Restore();
            stage.Restore();

            // 快照不在目标帧之前, 无法再往回拖
            if (stage.frame > targetframe) return false;

            player.Seek(stage.frame);

            // 快进: 只推进逻辑, 期间产生的渲染指令全部丢弃
            int drain = 0;
            while (stage.frame < targetframe)
            {
                if (false == player.Sync(stage.frame, stage)) break;
                stage.Step();

                if (++drain < 25) continue;
                drain = 0;
                ClearQueues();
            }

            // 按当前逻辑状态全量重推渲染指令
            ClearQueues();
            stage.rilsync.ClearHashCode();
            stage.rilsync.Translate();

            // 位置整体瞬移, 碰撞盒可视化直接贴合, 避免滑过去
            if (null != colliderdrawer) colliderdrawer.Flash();

            return true;
        }

        /// <summary>
        /// 清空渲染指令队列 (回滚/快进时丢弃已经过期的渲染指令)
        /// </summary>
        private void ClearQueues()
        {
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
                // 逻辑已回到快照帧, 位置整体瞬移, 碰撞盒可视化直接贴合, 避免滑过去
                if (null != colliderdrawer) colliderdrawer.Flash();

                return;
            }

            if (null == stage || StageState.Ticking != stage.state) return;

            //while (world.input.TryDequeueCommand(out var command))
            //{
            //    stage.SetCommand(command);
            //    command.Reset();
            //    ObjectPool.Set(command);
            //}

            // 录像回放: 输入来自录像, 不读取本地设备, 玩家无法操控
            if (null != player)
            {
                // 录像已播完时不推进逻辑帧, 交给上层结算
                if (false == player.Sync(stage.frame, stage)) return;
            }
            else
            {
                // 同步本地输入到逻辑层 (同时录像)
                SyncInput();
            }

            stage.Step();

        }

        /// <summary>
        /// 同步本地输入到逻辑层
        /// 按 INPUT_DEFINE.SYNC_INPUTS 登记的类型批量采集并下发, 新增按键无需改动此处
        /// 同时把下发的世界方向登记到录像, 保证回放时与录制时完全一致
        /// </summary>
        private void SyncInput()
        {
            var input = world.input;
            if (null == input) return;

            var inputs = INPUT_DEFINE.SYNC_INPUTS;
            for (int i = 0; i < inputs.Length; i++)
            {
                var value = input.GetInput(inputs[i]);
                var dire = ToWorldDire(value.dire);
                stage.SetInput(world.selfseat, inputs[i], value.press, dire);
                if (null != recorder) recorder.Record(stage.frame, inputs[i], value.press, dire);
            }
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