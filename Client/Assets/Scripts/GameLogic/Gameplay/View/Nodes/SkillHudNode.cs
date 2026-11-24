using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 技能面板 HUD
    /// 显示本地角色的技能冷却 / 连招窗口进度 / 时间轴进度;
    /// 数据只来自 RIL_SKILL, 不直接读逻辑层, 没有本地角色或没有数据时自动隐藏;
    /// 监听 LateTickEvent, 保证读到的是本帧渲染刚派发进桶的数据
    /// </summary>
    public class SkillHudNode : Node
    {
        /// <summary>
        /// 参考分辨率
        /// </summary>
        private static readonly Vector2 REFERENCE = new(1920, 1080);
        /// <summary>
        /// 槽位按键显示名, 与 SKILL_DEFINE.SLOT_INPUTS 一一对应
        /// </summary>
        private static readonly string[] SLOT_NAMES = { "左键", "Q", "E", "R" };

        private static readonly Color PANEL_COLOR = new(0f, 0f, 0f, 0.45f);
        private static readonly Color TRACK_COLOR = new(1f, 1f, 1f, 0.16f);
        private static readonly Color READY_COLOR = new(0.3f, 0.75f, 1f, 0.9f);
        private static readonly Color COOLDOWN_COLOR = new(0.85f, 0.55f, 0.2f, 0.9f);
        private static readonly Color WINDOW_COLOR = new(0.45f, 0.95f, 0.55f, 0.9f);
        private static readonly Color TIMELINE_COLOR = new(0.95f, 0.45f, 0.75f, 0.9f);

        /// <summary>
        /// 内置字体缓存
        /// </summary>
        private static Font cachedfont { get; set; }
        /// <summary>
        /// 1x1 白色精灵
        /// </summary>
        private static Sprite cachedsprite { get; set; }

        /// <summary>
        /// 画布
        /// </summary>
        private GameObject canvasgo { get; set; }
        /// <summary>
        /// 面板
        /// </summary>
        private GameObject panel { get; set; }
        /// <summary>
        /// 槽位进度填充
        /// </summary>
        private Image[] fills { get; set; }
        /// <summary>
        /// 槽位文本
        /// </summary>
        private Text[] texts { get; set; }
        /// <summary>
        /// 连招窗口填充
        /// </summary>
        private Image windowfill { get; set; }
        /// <summary>
        /// 连招窗口文本
        /// </summary>
        private Text windowtext { get; set; }
        /// <summary>
        /// 时间轴填充
        /// </summary>
        private Image timelinefill { get; set; }
        /// <summary>
        /// 时间轴文本
        /// </summary>
        private Text timelinetext { get; set; }

        /// <summary>
        /// 内置字体
        /// </summary>
        private static Font uifont
        {
            get
            {
                if (null != cachedfont) return cachedfont;

                cachedfont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return cachedfont;
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            Build();
            Hide();

            world.ticker.eventor.Listen<LateTickEvent>(OnLateTick);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (null != world && null != world.ticker) world.ticker.eventor.UnListen<LateTickEvent>(OnLateTick);

            if (null != canvasgo) GameObject.Destroy(canvasgo);
            canvasgo = null;
            panel = null;
            fills = null;
            texts = null;
            windowfill = null;
            windowtext = null;
            timelinefill = null;
            timelinetext = null;
        }

        /// <summary>
        /// 渲染帧末尾刷新 (此时导演已把本帧的 RIL 灌进桶)
        /// </summary>
        /// <param name="e">事件</param>
        private void OnLateTick(LateTickEvent e)
        {
            if (null == world) return;

            var self = world.self;
            if (0 == self || false == world.rilbucket.SeekRIL<RIL_SKILL>(self, out var ril))
            {
                Hide();
                return;
            }

            Show();
            Refresh(ril);
        }

        /// <summary>
        /// 按状态刷新界面
        /// </summary>
        /// <param name="ril">技能面板指令</param>
        private void Refresh(RIL_SKILL ril)
        {
            for (int i = 0; i < SKILL_DEFINE.SLOT_COUNT; i++)
            {
                var name = i < SLOT_NAMES.Length ? SLOT_NAMES[i] : i.ToString();
                var skill = null != ril.slots && i < ril.slots.Length ? ril.slots[i] : 0u;
                var cooldown = null != ril.cooldowns && i < ril.cooldowns.Length ? ril.cooldowns[i].AsFloat() : 0f;
                var duration = null != ril.durations && i < ril.durations.Length ? ril.durations[i].AsFloat() : 0f;

                if (0 == skill)
                {
                    SetBar(fills[i], texts[i], 0f, $"{name}  --", TRACK_COLOR);
                    continue;
                }

                var ready = cooldown <= 0f;
                var progress = ready ? 1f : (duration > 0f ? 1f - cooldown / duration : 0f);
                var label = ready
                    ? $"{name}  {skill}  就绪"
                    : $"{name}  {skill}  CD {cooldown:0.0}s / {duration:0.0}s";

                SetBar(fills[i], texts[i], progress, label, ready ? READY_COLOR : COOLDOWN_COLOR);
            }

            // 连招窗口
            if (0 == ril.window)
            {
                SetBar(windowfill, windowtext, 0f, "连招窗口  --", TRACK_COLOR);
            }
            else
            {
                var remaining = ril.windowremaining.AsFloat();
                var duration = ril.windowduration.AsFloat();
                var progress = duration > 0f ? remaining / duration : 0f;

                SetBar(windowfill, windowtext, progress, $"连招窗口  {ril.window}  {remaining:0.00}s / {duration:0.00}s", WINDOW_COLOR);
            }

            // 时间轴
            if (0 == ril.timeline)
            {
                SetBar(timelinefill, timelinetext, 0f, "时间轴  --", TRACK_COLOR);
            }
            else
            {
                var elapsed = ril.timelineelapsed.AsFloat();
                var max = ril.timelinemax.AsFloat();
                var progress = max > 0f ? elapsed / max : 0f;

                SetBar(timelinefill, timelinetext, progress, $"时间轴  {ril.timeline}  {elapsed:0.00}s / {max:0.00}s", TIMELINE_COLOR);
            }
        }

        /// <summary>
        /// 设置一条进度条
        /// </summary>
        /// <param name="fill">填充图</param>
        /// <param name="text">文本</param>
        /// <param name="progress">进度 0~1</param>
        /// <param name="label">文本内容</param>
        /// <param name="color">填充颜色</param>
        private static void SetBar(Image fill, Text text, float progress, string label, Color color)
        {
            if (null != fill)
            {
                fill.fillAmount = Mathf.Clamp01(progress);
                fill.color = color;
            }

            // 文本没变就不写, 避免每帧触发 UGUI 重建
            if (null != text && text.text != label) text.text = label;
        }

        /// <summary>
        /// 显示面板
        /// </summary>
        private void Show()
        {
            if (null != panel && false == panel.activeSelf) panel.SetActive(true);
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        private void Hide()
        {
            if (null != panel && panel.activeSelf) panel.SetActive(false);
        }

        /// <summary>
        /// 创建界面
        /// </summary>
        private void Build()
        {
            if (null != canvasgo) return;

            canvasgo = new GameObject("SkillHud", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;

            var scaler = canvasgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.referenceResolution = REFERENCE;
            scaler.matchWidthOrHeight = 0.5f;

            var bottomleft = Vector2.zero;
            var panelrt = MakeRect(canvasgo.transform, "Panel", bottomleft, bottomleft, new Vector2(24f, 24f), new Vector2(560f, 330f));
            panel = panelrt.gameObject;

            var bg = panel.AddComponent<Image>();
            bg.sprite = White();
            bg.color = PANEL_COLOR;
            bg.raycastTarget = false;

            fills = new Image[SKILL_DEFINE.SLOT_COUNT];
            texts = new Text[SKILL_DEFINE.SLOT_COUNT];

            const float height = 46f;
            const float gap = 8f;
            var top = new Vector2(0f, 1f);

            for (int i = 0; i < SKILL_DEFINE.SLOT_COUNT; i++)
            {
                var y = -(12f + (height + gap) * i);
                fills[i] = Row(panelrt, $"Slot{i}", top, new Vector2(12f, y), new Vector2(536f, height), out texts[i]);
            }

            var windowy = -(12f + (height + gap) * SKILL_DEFINE.SLOT_COUNT);
            windowfill = Row(panelrt, "Window", top, new Vector2(12f, windowy), new Vector2(536f, height), out var windowlabel);
            windowtext = windowlabel;

            var timeliney = windowy - height - gap;
            timelinefill = Row(panelrt, "Timeline", top, new Vector2(12f, timeliney), new Vector2(536f, height), out var timelinelabel);
            timelinetext = timelinelabel;
        }

        /// <summary>
        /// 创建一条进度条 (底 + 填充 + 文本)
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="name">名称</param>
        /// <param name="anchor">锚点</param>
        /// <param name="pos">位置</param>
        /// <param name="size">尺寸</param>
        /// <param name="text">文本</param>
        /// <returns>填充图</returns>
        private static Image Row(RectTransform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, out Text text)
        {
            var rt = MakeRect(parent, name, anchor, anchor, pos, size);

            var track = rt.gameObject.AddComponent<Image>();
            track.sprite = White();
            track.color = TRACK_COLOR;
            track.raycastTarget = false;

            var fill = CreateImage(rt, "Fill");
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0f;

            text = Label(rt, "Text", string.Empty);

            return fill;
        }

        /// <summary>
        /// 创建一个铺满父节点的纯色图片
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="name">名称</param>
        /// <returns>图片</returns>
        private static Image CreateImage(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.sprite = White();
            image.color = READY_COLOR;
            image.raycastTarget = false;

            return image;
        }

        /// <summary>
        /// 创建一个铺满父节点并留出边距的文本
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="name">名称</param>
        /// <param name="content">内容</param>
        /// <returns>文本</returns>
        private static Text Label(RectTransform parent, string name, string content)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12f, 0f);
            rt.offsetMax = new Vector2(-12f, 0f);

            var text = go.GetComponent<Text>();
            text.font = uifont;
            text.fontSize = 26;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.text = content;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return text;
        }

        /// <summary>
        /// 创建一个按锚点定位的节点
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="name">名称</param>
        /// <param name="anchor">锚点</param>
        /// <param name="pivot">轴心</param>
        /// <param name="pos">位置</param>
        /// <param name="size">尺寸</param>
        /// <returns>矩形</returns>
        private static RectTransform MakeRect(UnityEngine.Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            return rt;
        }

        /// <summary>
        /// 1x1 白色精灵
        /// </summary>
        /// <returns>精灵</returns>
        private static Sprite White()
        {
            if (null != cachedsprite) return cachedsprite;

            var texture = Texture2D.whiteTexture;
            cachedsprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            cachedsprite.name = "SkillHudWhite";

            return cachedsprite;
        }
    }
}
