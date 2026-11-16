using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameLogic.Gameplay.X
{
    /// <summary>
    /// WECS_0 测试场景的界面
    /// 全部在运行时用代码创建, 不依赖预制体与场景中的 Canvas / EventSystem
    /// 主界面: 直接启动 / 以房间号启动 (录像回放)
    /// 战斗界面: 结算退出
    /// </summary>
    public class WECS_0UI
    {
        /// <summary>
        /// 点击"直接启动"
        /// </summary>
        public Action onlaunch;
        /// <summary>
        /// 点击"以房间号启动", 参数为房间号
        /// </summary>
        public Action<string> onreplay;
        /// <summary>
        /// 点击"结算退出"
        /// </summary>
        public Action onsettle;
        /// <summary>
        /// 点击"快照"
        /// </summary>
        public Action onsnapshot;
        /// <summary>
        /// 点击"恢复快照"
        /// </summary>
        public Action onrestore;

        /// <summary>
        /// 参考分辨率
        /// </summary>
        private static readonly Vector2 reference = new(1920, 1080);
        /// <summary>
        /// 内置字体缓存
        /// </summary>
        private static Font cachedfont;

        private GameObject canvasgo;
        private GameObject launchpanel;
        private GameObject battlepanel;
        private Text resulttext;
        private Text tiptext;
        private InputField roominput;
        private Text battletext;
        private Button snapshotbutton;
        private Button restorebutton;

        /// <summary>
        /// 内置字体 (Unity 2022.2 之后 Arial.ttf 已更名为 LegacyRuntime.ttf)
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

        /// <summary>
        /// 创建界面
        /// </summary>
        public void Build()
        {
            if (null != canvasgo) return;

            EnsureEventSystem();

            canvasgo = new GameObject("WECS_0UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.referenceResolution = reference;
            scaler.matchWidthOrHeight = 0.5f;

            BuildLaunch(canvasgo.transform);
            BuildBattle(canvasgo.transform);
        }

        /// <summary>
        /// 显示主界面
        /// </summary>
        /// <param name="result">结算结果 (房间号 / 录像路径等)</param>
        /// <param name="tip">提示信息</param>
        public void ShowLaunch(string result = null, string tip = null)
        {
            Build();
            launchpanel.SetActive(true);
            battlepanel.SetActive(false);
            resulttext.text = result ?? string.Empty;
            tiptext.text = tip ?? string.Empty;
        }

        /// <summary>
        /// 显示战斗界面
        /// </summary>
        /// <param name="room">房间号</param>
        /// <param name="replay">是否录像回放</param>
        public void ShowBattle(string room, bool replay)
        {
            Build();
            launchpanel.SetActive(false);
            battlepanel.SetActive(true);
            battletext.text = replay ? $"录像回放  房间号: {room}" : $"战斗中  房间号: {room}";
            // 回放中不允许快照/恢复, 恢复需要先有快照
            SetSnapshotButtons(false == replay, false);
        }

        /// <summary>
        /// 设置快照按钮是否可用
        /// </summary>
        /// <param name="cansnapshot">能否快照</param>
        /// <param name="canrestore">能否恢复快照</param>
        public void SetSnapshotButtons(bool cansnapshot, bool canrestore)
        {
            if (null != snapshotbutton) snapshotbutton.interactable = cansnapshot;
            if (null != restorebutton) restorebutton.interactable = canrestore;
        }

        /// <summary>
        /// 填充房间号 (方便上一局结算后直接回放)
        /// </summary>
        public void SetRoom(string room)
        {
            if (null == roominput) return;
            roominput.text = room ?? string.Empty;
        }

        /// <summary>
        /// 刷新战斗界面的信息
        /// </summary>
        public void SetBattleInfo(string info)
        {
            if (null == battletext) return;
            battletext.text = info;
        }

        /// <summary>
        /// 创建主界面
        /// </summary>
        private void BuildLaunch(Transform parent)
        {
            var panel = Stretch(parent, "LaunchPanel");
            launchpanel = panel.gameObject;
            var bg = launchpanel.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.07f, 0.09f, 0.97f);

            var center = new Vector2(0.5f, 0.5f);
            var middle = new Vector2(0.5f, 0.5f);

            Label(panel, "Title", "WECS_0 战斗测试", 72, Color.white, center, middle, new Vector2(0, 320), new Vector2(1400, 110));

            resulttext = Label(panel, "Result", string.Empty, 34, new Color(0.5f, 0.9f, 1f), center, middle, new Vector2(0, 195), new Vector2(1700, 100));

            MakeButton(panel, "BtnLaunch", "直接启动", center, middle, new Vector2(0, 50), new Vector2(460, 110), () => onlaunch?.Invoke());

            roominput = MakeField(panel, "RoomInput", "输入房间号", center, middle, new Vector2(0, -100), new Vector2(460, 90));

            MakeButton(panel, "BtnReplay", "以房间号启动", center, middle, new Vector2(0, -240), new Vector2(460, 110), OnReplayClick);

            tiptext = Label(panel, "Tip", string.Empty, 28, new Color(1f, 0.62f, 0.45f), center, middle, new Vector2(0, -390), new Vector2(1700, 120));
        }

        /// <summary>
        /// 创建战斗界面
        /// </summary>
        private void BuildBattle(Transform parent)
        {
            var panel = Stretch(parent, "BattlePanel");
            battlepanel = panel.gameObject;

            var lefttop = new Vector2(0, 1);
            var topleft = new Vector2(0, 1);
            battletext = Label(panel, "Info", string.Empty, 30, new Color(0.85f, 0.92f, 1f), lefttop, topleft, new Vector2(30, -24), new Vector2(1200, 60), TextAnchor.MiddleLeft);

            var righttop = new Vector2(1, 1);
            var topright = new Vector2(1, 1);
            snapshotbutton = MakeButton(panel, "BtnSnapshot", "快照", righttop, topright, new Vector2(-500, -24), new Vector2(200, 80), () => onsnapshot?.Invoke());
            restorebutton = MakeButton(panel, "BtnRestore", "恢复快照", righttop, topright, new Vector2(-280, -24), new Vector2(200, 80), () => onrestore?.Invoke());
            MakeButton(panel, "BtnSettle", "结算退出", righttop, topright, new Vector2(-30, -24), new Vector2(220, 80), () => onsettle?.Invoke());
        }

        /// <summary>
        /// 以房间号启动
        /// </summary>
        private void OnReplayClick()
        {
            onreplay?.Invoke(null != roominput ? roominput.text : string.Empty);
        }

        /// <summary>
        /// 保证场景中存在 EventSystem, 否则 UI 无法点击
        /// </summary>
        private static void EnsureEventSystem()
        {
            if (null != EventSystem.current) return;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            go.transform.SetParent(null, false);
        }

        /// <summary>
        /// 创建一个铺满父节点的节点
        /// </summary>
        private static RectTransform Stretch(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return rt;
        }

        /// <summary>
        /// 创建一个按锚点定位的节点
        /// </summary>
        private static RectTransform Node(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
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
        /// 创建文本
        /// </summary>
        private static Text Label(Transform parent, string name, string content, int size, Color color, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 rectsize, TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var rt = Node(parent, name, anchor, pivot, pos, rectsize);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = uifont;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.text = content;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return text;
        }

        /// <summary>
        /// 创建按钮
        /// </summary>
        private static Button MakeButton(Transform parent, string name, string content, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Action onclick)
        {
            var rt = Node(parent, name, anchor, pivot, pos, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = new Color(0.2f, 0.45f, 0.85f, 1f);

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (null != onclick) button.onClick.AddListener(() => onclick());

            var textrt = Stretch(rt, "Text");
            var text = textrt.gameObject.AddComponent<Text>();
            text.font = uifont;
            text.fontSize = 40;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = content;
            text.raycastTarget = false;

            return button;
        }

        /// <summary>
        /// 创建输入框
        /// </summary>
        private static InputField MakeField(Transform parent, string name, string placeholder, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var rt = Node(parent, name, anchor, pivot, pos, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = new Color(0.15f, 0.16f, 0.19f, 1f);

            var field = rt.gameObject.AddComponent<InputField>();
            field.targetGraphic = image;
            field.lineType = InputField.LineType.SingleLine;
            field.characterLimit = 12;
            field.contentType = InputField.ContentType.Standard;

            var text = FieldText(rt, "Text", string.Empty, Color.white, 36);
            field.textComponent = text;
            field.text = string.Empty;

            var ph = FieldText(rt, "Placeholder", placeholder, new Color(1f, 1f, 1f, 0.35f), 36);
            field.placeholder = ph;

            return field;
        }

        /// <summary>
        /// 创建输入框内部的文本
        /// </summary>
        private static Text FieldText(Transform parent, string name, string content, Color color, int size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(16, 6);
            rt.offsetMax = new Vector2(-16, -6);

            var text = go.AddComponent<Text>();
            text.font = uifont;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            text.text = content;
            text.raycastTarget = false;
            text.supportRichText = false;

            return text;
        }
    }
}
