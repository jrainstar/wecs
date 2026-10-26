using UnityEngine;
using UnityEngine.InputSystem;

namespace GameLogic.Gameplay.Common
{
    /// <summary>
    /// 键盘/鼠标设备, 只负责向 InputKit 提供设备的原始状态, 自身不做任何缓冲与派发
    /// </summary>
    public class Keyboard : Comp
    {
        /// <summary>
        /// 键盘设备
        /// </summary>
        public UnityEngine.InputSystem.Keyboard keyboard => UnityEngine.InputSystem.Keyboard.current;
        /// <summary>
        /// 鼠标设备
        /// </summary>
        public Mouse mouse => Mouse.current;

        /// <summary>
        /// 是否有键盘设备
        /// </summary>
        public bool connected => null != keyboard;

        /// <summary>
        /// 移动方向 (WASD/方向键), 未输入时为 zero, 长度不超过 1
        /// </summary>
        public Vector2 joystick
        {
            get
            {
                Vector2 dire = Vector2.zero;

                var kb = keyboard;
                if (null != kb)
                {
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) dire.x -= 1f;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dire.x += 1f;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed) dire.y -= 1f;
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) dire.y += 1f;
                }

                return Vector2.ClampMagnitude(dire, 1f);
            }
        }

        /// <summary>
        /// 视角方向 (鼠标增量)
        /// </summary>
        public Vector2 look
        {
            get
            {
                var m = mouse;
                if (null == m) return Vector2.zero;

                return m.delta.ReadValue();
            }
        }

        /// <summary>
        /// 滚轮增量
        /// </summary>
        public Vector2 wheel => null == mouse ? Vector2.zero : mouse.scroll.ReadValue();

        /// <summary>
        /// 左键按住
        /// </summary>
        public bool left => null != mouse && mouse.leftButton.isPressed;
        /// <summary>
        /// 右键按住
        /// </summary>
        public bool right => null != mouse && mouse.rightButton.isPressed;
        /// <summary>
        /// 中键按住
        /// </summary>
        public bool middle => null != mouse && mouse.middleButton.isPressed;
        /// <summary>
        /// 左键本帧按下
        /// </summary>
        public bool leftdown => null != mouse && mouse.leftButton.wasPressedThisFrame;
        /// <summary>
        /// 左键本帧抬起
        /// </summary>
        public bool leftup => null != mouse && mouse.leftButton.wasReleasedThisFrame;

        /// <summary>
        /// 按键按住
        /// </summary>
        /// <param name="key">按键</param>
        /// <returns>YES/NO</returns>
        public bool GetKey(UnityEngine.InputSystem.Key key)
        {
            var control = SeekKey(key);
            if (null == control) return false;

            return control.isPressed;
        }

        /// <summary>
        /// 按键本帧按下
        /// </summary>
        /// <param name="key">按键</param>
        /// <returns>YES/NO</returns>
        public bool GetKeyDown(UnityEngine.InputSystem.Key key)
        {
            var control = SeekKey(key);
            if (null == control) return false;

            return control.wasPressedThisFrame;
        }

        /// <summary>
        /// 按键本帧抬起
        /// </summary>
        /// <param name="key">按键</param>
        /// <returns>YES/NO</returns>
        public bool GetKeyUp(UnityEngine.InputSystem.Key key)
        {
            var control = SeekKey(key);
            if (null == control) return false;

            return control.wasReleasedThisFrame;
        }

        /// <summary>
        /// 查找按键控件
        /// </summary>
        /// <param name="key">按键</param>
        /// <returns>按键控件</returns>
        private UnityEngine.InputSystem.Controls.KeyControl SeekKey(UnityEngine.InputSystem.Key key)
        {
            var kb = keyboard;
            if (null == kb) return null;

            return kb[key];
        }
    }
}
