using GameLogic.Gameplay.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 血条代理
    /// 位置跟随节点代理插值后的位置, 始终正对相机 (与伤害跳字同一套朝向);
    /// 数据只来自 RIL_ATTRIBUTE, 不直接读逻辑层;
    /// 画布/图片只在创建时建一次, 之后每帧只改 fillAmount 与 Transform
    /// </summary>
    public class HealthBarAgent : Agent
    {
        /// <summary>
        /// 血条宽度 (世界单位, 1 = 1 米)
        /// </summary>
        private const float WIDTH = 1f;
        /// <summary>
        /// 血条高度
        /// </summary>
        private const float HEIGHT = 0.1f;
        /// <summary>
        /// 头顶偏移 (米)
        /// </summary>
        private const float OFFSET_Y = 2.2f;
        /// <summary>
        /// 血条追赶速度, 越大越贴近目标值
        /// </summary>
        private const float SPEED = 14f;
        /// <summary>
        /// 追赶的贴合阈值, 差值小于该值直接对齐
        /// </summary>
        private const float SNAP = 0.001f;

        private static readonly Color BACK_COLOR = new(0f, 0f, 0f, 0.65f);
        private static readonly Color FILL_COLOR = new(0.86f, 0.25f, 0.22f, 1f);

        /// <summary>
        /// 1x1 白色精灵, 所有血条共用, 不产生额外贴图
        /// </summary>
        private static Sprite whitesprite { get; set; }

        /// <summary>
        /// 血条根 (世界空间画布)
        /// </summary>
        private RectTransform rt { get; set; }
        /// <summary>
        /// 血条填充
        /// </summary>
        private Image fill { get; set; }
        /// <summary>
        /// 目标进度 (血/血上限)
        /// </summary>
        private float ratio { get; set; }
        /// <summary>
        /// 当前显示进度, 平滑追赶 ratio
        /// </summary>
        private float display { get; set; }

        protected override void OnReady()
        {
            ratio = 1f;
            display = 1f;

            Create();

            WatchRIL<RIL_ATTRIBUTE>(OnRILAttribute);
        }

        protected override void OnReset()
        {
            if (null != rt)
            {
                GameObject.Destroy(rt.gameObject);
            }

            rt = null;
            fill = null;
            ratio = 0f;
            display = 0f;
        }

        protected override void OnFlash()
        {
            base.OnFlash();
            // 刚创建时先定位一次, 避免血条在世界原点闪一帧;
            // 节点代理可能还没创建 (RIL 派发顺序不保证), 拿不到就先隐藏
            if (false == Follow())
            {
                if (null != rt) rt.gameObject.SetActive(false);
            }
        }

        protected override void OnChase(float tick, float timescale)
        {
            base.OnChase(tick, timescale);
            if (false == Follow()) return;

            Apply(tick);
        }

        /// <summary>
        /// 血条跟随节点
        /// 位置取节点代理的位置 (已做渲染帧插值), 不直接取逻辑层坐标
        /// </summary>
        /// <returns>YES/NO 节点是否存在</returns>
        private bool Follow()
        {
            if (null == rt) return false;

            var node = world.GetAgent<NodeAgent>(actor);
            if (null == node || null == node.go) return false;

            rt.position = node.go.transform.position + Vector3.up * OFFSET_Y;

            var camera = world.eyes.camera;
            if (null != camera) rt.rotation = camera.transform.rotation;

            if (false == rt.gameObject.activeSelf) rt.gameObject.SetActive(true);

            return true;
        }

        /// <summary>
        /// 血条长度平滑追赶目标进度, 掉血/回血都能看到一段变化过程
        /// </summary>
        /// <param name="tick">渲染帧时长</param>
        private void Apply(float tick)
        {
            if (null == fill) return;

            if (Mathf.Abs(display - ratio) <= SNAP)
            {
                if (display == ratio) return;

                display = ratio;
            }
            else
            {
                var t = tick > 0f ? 1f - Mathf.Exp(-SPEED * tick) : 1f;
                display = Mathf.Lerp(display, ratio, t);
            }

            fill.fillAmount = display;
        }

        /// <summary>
        /// 属性状态变更
        /// </summary>
        /// <param name="ril">渲染指令</param>
        private void OnRILAttribute(RIL_ATTRIBUTE ril)
        {
            if (ril.maxhp <= 0) return;

            ratio = Mathf.Clamp01((float)ril.hp / ril.maxhp);
        }

        /// <summary>
        /// 创建血条 (画布 + 背景 + 填充)
        /// 世界空间画布, 尺寸直接用米做单位, 由血条代理每帧定位/朝向
        /// </summary>
        private void Create()
        {
            if (null == world || null == world.root) return;

            var go = new GameObject("HealthBar", typeof(RectTransform), typeof(Canvas));
            rt = (RectTransform)go.transform;
            rt.SetParent(world.root.transform, false);
            rt.sizeDelta = new Vector2(WIDTH, HEIGHT);
            rt.localScale = Vector3.one;

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            if (null != world.eyes && null != world.eyes.camera) canvas.worldCamera = world.eyes.camera;

            CreateImage(rt, "Background", BACK_COLOR);

            fill = CreateImage(rt, "Fill", FILL_COLOR);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = display;
        }

        /// <summary>
        /// 创建一张铺满父节点的纯色图片
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="name">名称</param>
        /// <param name="color">颜色</param>
        /// <returns>图片</returns>
        private static Image CreateImage(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.sprite = White();
            image.color = color;
            image.raycastTarget = false;

            return image;
        }

        /// <summary>
        /// 1x1 白色精灵, 用内置白图生成, 不需要美术资源
        /// </summary>
        /// <returns>精灵</returns>
        private static Sprite White()
        {
            if (null != whitesprite) return whitesprite;

            var texture = Texture2D.whiteTexture;
            whitesprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            whitesprite.name = "HealthBarWhite";

            return whitesprite;
        }
    }
}
