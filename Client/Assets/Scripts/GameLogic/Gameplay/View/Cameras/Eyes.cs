using Cinemachine;
using GameLogic.Gameplay.Common;
using UnityEngine;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 眼睛/镜头
    /// </summary>
    public class Eyes : Comp
    {
        /// <summary>
        /// 视角灵敏度 (鼠标像素增量 -> CM 轴值, 太小会觉得转不动)
        /// </summary>
        private const float LOOK_SENSITIVITY = 0.2f;
        /// <summary>
        /// 视野缩放速度 (滚轮每格 -> 米/秒)
        /// </summary>
        private const float ZOOM_SPEED = 20f;
        /// <summary>
        /// 视野缩放阻尼 (越大越跟手)
        /// </summary>
        private const float ZOOM_DAMPING = 15f;
        /// <summary>
        /// 视野最近距离
        /// </summary>
        private const float ZOOM_MIN = -7.5f;
        /// <summary>
        /// 视野最远距离 (0 为原位置)
        /// </summary>
        private const float ZOOM_MAX = 0f;

        /// <summary>
        /// 世界
        /// </summary>
        private World world { get; set; }
        /// <summary>
        /// 相机
        /// </summary>
        public Camera camera { get; private set; }
        /// <summary>
        /// CM 的 GameObject
        /// </summary>
        private GameObject cmgo { get; set; }
        /// <summary>
        /// CM 轴输入适配器
        /// </summary>
        private AxisInput input { get; set; }
        /// <summary>
        /// CM 组件
        /// </summary>
        private CinemachineFreeLook cm { get; set; }
        /// <summary>
        /// CM 偏移组件
        /// </summary>
        private CinemachineCameraOffset cmoff { get; set; }

        /// <summary>
        /// 滚轮累计值 (逐帧消耗, 消耗完即静止)
        /// </summary>
        private float wheel { get; set; }
        /// <summary>
        /// 目标偏移
        /// </summary>
        private Vector3 offset { get; set; }

        /// <summary>
        /// 视角偏航角(度), 绕 Y 轴, 与 RIL_SPATIAL.euler 的 y 一致
        /// 用于把摇杆输入从"相机朝向"转换到"世界朝向"
        /// 直接取相机的实际朝向: CM 的 m_XAxis.Value 只在 Brain 的 LateUpdate 之后才有最新值,
        /// 逻辑 Step 在 FixedUpdate 中执行, 读到的会是过期数据, 因此不能用
        /// </summary>
        public float yaw
        {
            get
            {
                if (null == camera) return 0f;

                return camera.transform.eulerAngles.y;
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            world.ticker.eventor.Listen<TickEvent>(OnTick);
            camera = Camera.main;
            cmgo = new GameObject("CMFreeLock");

            cm = cmgo.AddComponent<CinemachineFreeLook>();
            cmoff = cmgo.AddComponent<CinemachineCameraOffset>();

            // CM 输入: 用纯代码实现的轴输入适配器替换 InputAction 配置方案
            input = new AxisInput(engine.u3dkit.input.keyboard);
            if (null != input) input.enabled = true;
            cm.m_XAxis.SetInputAxisProvider(0, input);
            cm.m_YAxis.SetInputAxisProvider(1, input);

            // 设置 CM 相机参数
            cm.m_Lens.FieldOfView = 45f;
            cm.m_YAxis.m_InvertInput = true;
            cm.m_XAxis.m_InvertInput = false;
            cm.m_Orbits[0].m_Height = 4.5f;
            cm.m_Orbits[1].m_Height = 2.5f;
            cm.m_Orbits[2].m_Height = 0f;
            cm.m_Orbits[0].m_Radius = 4f;
            cm.m_Orbits[1].m_Radius = 4.5f;
            cm.m_Orbits[2].m_Radius = 4f;
            // 设置 CM 相机的 DeadZone (避免屏幕抖动)
            ModifyDeadZone(cm, 2, 2);

            // 设置 CM 相机偏移
            offset = new Vector3(0, 1.7f, -2);
            cmoff.m_Offset = offset;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            world.ticker.eventor.UnListen<TickEvent>(OnTick);
            input = null;
            GameObject.Destroy(cmgo);
        }

        public Eyes Initialize(World world)
        {
            this.world = world;

            return this;
        }

        private void OnTick(TickEvent e)
        {
            // 根据鼠标锁定状态来锁定镜头输入 (未锁定时仍允许旋转, 方便调试)
            // input.enabled = CursorLockMode.Locked == Cursor.lockState;

            // LookAt 自己
            var node = world.GetAgent<NodeAgent>(world.self);
            if (null == node) return;
            cm.Follow = node.go.transform;
            cm.LookAt = node.go.transform;

            // 镜头推远推近
            Zoom(e.tick);
        }

        /// <summary>
        /// 镜头推远推近
        /// </summary>
        /// <param name="tick">渲染帧时长 s/秒</param>
        private void Zoom(float tick)
        {
            // 直接读设备的滚轮增量, 每帧累加后立刻消耗, 保证不依赖是否锁定鼠标
            wheel += engine.u3dkit.input.keyboard.wheel.y;
            if (0 == wheel) return;

            // 属性返回的是值副本, 不能直接改字段, 取出后再写回
            var target = offset;
            target.z = Mathf.Clamp(target.z + wheel * ZOOM_SPEED * tick, ZOOM_MIN, ZOOM_MAX);
            offset = target;
            wheel = 0;

            // 与帧率无关的阻尼
            cmoff.m_Offset = Vector3.Lerp(cmoff.m_Offset, offset, 1f - Mathf.Exp(-ZOOM_DAMPING * tick));
        }

        private void ModifyDeadZone(CinemachineFreeLook freeLookCamera, float deadZoneWidth, float deadZoneHeight)
        {
            // 遍历 CM FreeLook 的每个 Rig
            for (int i = 0; i < freeLookCamera.m_Orbits.Length; i++)
            {
                var rig = freeLookCamera.GetRig(i);
                if (null == rig) continue;

                var composer = rig.GetCinemachineComponent<CinemachineComposer>();
                if (null == composer) continue;

                // 设置 DeadZone 宽度和高度
                composer.m_SoftZoneWidth = deadZoneWidth;
                composer.m_SoftZoneHeight = deadZoneHeight;
            }
        }

        /// <summary>
        /// CM 轴输入适配器, 直接把键盘/鼠标设备状态喂给 CM, 不依赖 InputAction/InputActionReference
        /// 轴: 0 = X(左右), 1 = Y(上下), 2 = Z(推拉)
        /// </summary>
        private class AxisInput : AxisState.IInputAxisProvider
        {
            /// <summary>
            /// 是否启用 (鼠标未锁定时关闭)
            /// </summary>
            public bool enabled { get; set; } = true;
            /// <summary>
            /// 键盘/鼠标设备
            /// </summary>
            private readonly Keyboard keyboard;

            public AxisInput(Keyboard keyboard)
            {
                this.keyboard = keyboard;
            }

            public float GetAxisValue(int axis)
            {
                if (false == enabled) return 0f;
                if (null == keyboard) return 0f;

                var look = keyboard.look * LOOK_SENSITIVITY;
                switch (axis)
                {
                    case 0: return look.x;
                    case 1: return look.y;
                    case 2: return keyboard.wheel.y;
                }

                return 0f;
            }
        }
    }
}
