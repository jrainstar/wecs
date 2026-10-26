using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 输入系统
    /// </summary>
    public class InputSystem : Comp
    {
        /// <summary>
        /// 渲染世界
        /// </summary>
        private World world { get; set; }
        /// <summary>
        /// 输入数据集合
        /// </summary>
        private Dictionary<ushort, (bool press, IntVector2 dire)> inputdict { get; set; }
        /// <summary>
        /// 输入指令队列
        /// </summary>
        private Queue<Command> cmdqueue { get; set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            world.ticker.eventor.Listen<TickEvent>(OnTick);
            inputdict = ObjectPool.Ensure<Dictionary<ushort, (bool press, IntVector2 dire)>>();
            cmdqueue = ObjectPool.Ensure<Queue<Command>>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            world.ticker.eventor.UnListen<TickEvent>(OnTick);
            inputdict.Clear();
            ObjectPool.Set(inputdict);

            foreach (var command in cmdqueue)
            {
                command.Reset();
                ObjectPool.Set(command);
            }
            cmdqueue.Clear();
            ObjectPool.Set(cmdqueue);
        }

        /// <summary>
        /// 初始化输入系统
        /// </summary>
        /// <param name="world">世界</param>
        /// <returns>输入系统</returns>
        public InputSystem Initialize(World world)
        {
            this.world = world;

            return this;
        }

        /// <summary>
        /// 获取输入
        /// </summary>
        /// <param name="type">输入类型</param>
        /// <returns>输入信息</returns>
        public (bool press, IntVector2 dire) GetInput(ushort type)
        {
            if (inputdict.TryGetValue(type, out var input))
            {
                return input;
            }

            return default;
        }

        /// <summary>
        /// 设置输入
        /// </summary>
        /// <param name="type">输入类型</param>
        /// <param name="press">长按</param>
        /// <param name="dire">方向</param>
        public void SetInput(ushort type, bool press, IntVector2 dire)
        {
            if (inputdict.ContainsKey(type)) inputdict.Remove(type);

            inputdict.Add(type, (press, dire));
        }

        /// <summary>
        /// 尝试出队指令
        /// </summary>
        /// <param name="command">输入指令</param>
        /// <returns>YES/NO</returns>
        public bool TryDequeueCommand(out Command command)
        {
            return cmdqueue.TryDequeue(out command);
        }

        /// <summary>
        /// 设置输入指令
        /// </summary>
        /// <param name="command">输入指令</param>
        public void EnqueueCommand(Command command)
        {
            if (null == command) return;

            cmdqueue.Enqueue(command);
        }

        private void OnTick(TickEvent e)
        {
            if (CursorLockMode.Locked != Cursor.lockState) return;

            var joystickdire = engine.u3dkit.keyboard.Player.Move.ReadValue<Vector2>();


            SetInput(INPUT_DEFINE.JOYSTICK, Vector2.zero != joystickdire, new IntVector2((int)(joystickdire.x * ConfigKit.Float2Int), (int)(joystickdire.y * ConfigKit.Float2Int)));
        }
    }
}
