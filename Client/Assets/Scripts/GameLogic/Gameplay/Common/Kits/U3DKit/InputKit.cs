using GameLogic.Gameplay.Logic;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic.Gameplay.Common
{
    /// <summary>
    /// 输入系统, 负责把键盘/鼠标设备(Keyboard)的状态采集为逻辑层可用的输入数据
    /// 自身创建并持有 Keyboard, 每个 Tick 刷新一次 inputdict / cmddict
    /// </summary>
    public class InputKit : Comp
    {
        /// <summary>
        /// 键盘/鼠标设备
        /// </summary>
        public Keyboard keyboard { get; private set; }

        /// <summary>
        /// 输入数据集合
        /// </summary>
        private Dictionary<ushort, (bool press, IntVector2 dire)> inputdict { get; set; }
        /// <summary>
        /// 输入寻址映射, 按键类型 -> 设备状态读取器
        /// </summary>
        private Dictionary<ushort, System.Func<Vector2>> keydict { get; set; }
        /// <summary>
        /// 输入指令队列
        /// </summary>
        private Queue<Command> cmdqueue { get; set; }

        protected override void OnCreate()
        {
            base.OnCreate();

            keyboard = AddComp<Keyboard>();
            keyboard.Create();

            inputdict = ObjectPool.Ensure<Dictionary<ushort, (bool press, IntVector2 dire)>>();
            keydict = ObjectPool.Ensure<Dictionary<ushort, System.Func<Vector2>>>();
            cmdqueue = ObjectPool.Ensure<Queue<Command>>();

            Bind();
            engine.ticker.eventor.Listen<TickEvent>(OnTick);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            engine.ticker.eventor.UnListen<TickEvent>(OnTick);

            inputdict.Clear();
            ObjectPool.Set(inputdict);
            keydict.Clear();
            ObjectPool.Set(keydict);

            foreach (var command in cmdqueue)
            {
                command.Reset();
                ObjectPool.Set(command);
            }
            cmdqueue.Clear();
            ObjectPool.Set(cmdqueue);
        }

        /// <summary>
        /// 绑定按键类型与设备读取器
        /// 摇杆与按键一视同仁, 都是"输入类型 -> 设备读取器", 不存在特殊分支
        /// </summary>
        private void Bind()
        {
            keydict.Clear();
            BindKey(INPUT_DEFINE.JOYSTICK, new System.Func<Vector2>(() => keyboard.move));
            BindMouse(INPUT_DEFINE.BA, () => keyboard.left);
            BindKey(INPUT_DEFINE.BB, UnityEngine.InputSystem.Key.Q);
            BindKey(INPUT_DEFINE.BC, UnityEngine.InputSystem.Key.E);
            BindKey(INPUT_DEFINE.BD, UnityEngine.InputSystem.Key.R);
        }

        /// <summary>
        /// 绑定单个按键到输入类型
        /// 按键按住为一维输入 (非零即按下), 方向由上层使用
        /// </summary>
        /// <param name="type">输入类型</param>
        /// <param name="key">键盘按键</param>
        private void BindKey(ushort type, UnityEngine.InputSystem.Key key)
        {
            keydict.Add(type, () => keyboard.GetKey(key) ? Vector2.right : Vector2.zero);
        }

        /// <summary>
        /// 绑定鼠标按键到输入类型
        /// 鼠标按住按照一维输入处理 (非零即按下), 与键盘按键语义一致
        /// </summary>
        /// <param name="type">输入类型</param>
        /// <param name="button">鼠标按键按住状态读取器</param>
        private void BindMouse(ushort type, System.Func<bool> button)
        {
            keydict.Add(type, () => button.Invoke() ? Vector2.right : Vector2.zero);
        }

        /// <summary>
        /// 绑定自定义读取器到输入类型
        /// </summary>
        /// <param name="type">输入类型</param>
        /// <param name="reader">设备状态读取器</param>
        private void BindKey(ushort type, System.Func<Vector2> reader)
        {
            keydict.Add(type, reader);
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
            foreach (var kv in keydict)
            {
                var dire = kv.Value.Invoke();
                SetInput(kv.Key, Vector2.zero != dire, new IntVector2((int)(dire.x * ConfigKit.Float2Int), (int)(dire.y * ConfigKit.Float2Int)));
            }
        }
    }
}
