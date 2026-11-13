using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

using Transform = UnityEngine.Transform;


namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 伤害跳字
    /// 性能要点:
    /// 1. 世界空间 TMP 全部池化预热, 命中时只 SetActive + 改文本, 运行期不会 Instantiate/Destroy;
    /// 2. 所有跳字由本组件的一个 Tick 循环统一推进, 不给每个字挂 MonoBehaviour/协程/Animator/DOTween;
    /// 3. 材质共用字体自带的默认材质, 不产生材质实例, 保持合批; 变化只有 Transform 和顶点色;
    /// 4. 文本只在生成时写一次, 位移/缩放/淡出不改文本内容, 不会反复触发 TMP 重建;
    /// 5. 数字走缓存字符串, 连 string 分配也省掉.
    /// </summary>
    public class DamageSalute : RILEventHandler<RIL_EVENT_DAMAGE>
    {
        /// <summary>
        /// 同时在场的跳字上限, 也是预热数量
        /// </summary>
        private const int CAPACITY = 32;
        /// <summary>
        /// 单个跳字的存在时长 (秒)
        /// </summary>
        private const float DURATION = 0.9f;
        /// <summary>
        /// 上浮总高度 (米)
        /// </summary>
        private const float RISE = 1.2f;
        /// <summary>
        /// 出生位置相对单位的竖直偏移 (米)
        /// </summary>
        private const float OFFSET_Y = 2f;
        /// <summary>
        /// 水平随机抖动半径 (米), 多段伤害同时跳时不会完全重叠
        /// </summary>
        private const float SPREAD = 0.3f;
        /// <summary>
        /// 普通伤害字号
        /// TMP 世界空间的 1 点约等于 0.1 米, 9 号字实际高度约 0.9 米
        /// </summary>
        private const float SIZE = 9f;
        /// <summary>
        /// 暴击字号
        /// </summary>
        private const float CRIT_SIZE = 12.5f;
        /// <summary>
        /// 出生弹一下的倍率与持续时间
        /// </summary>
        private const float POP = 0.35f;
        private const float POP_TIME = 0.12f;
        /// <summary>
        /// 淡出起点 (占整个时长的比例)
        /// </summary>
        private const float FADE = 0.65f;

        private static readonly Color NORMAL_COLOR = new(1f, 0.93f, 0.78f, 1f);
        private static readonly Color CRIT_COLOR = new(1f, 0.32f, 0.18f, 1f);

        /// <summary>
        /// 数字字符串缓存, 避免每次命中都 ToString 分配
        /// </summary>
        private static readonly string[] TEXT_CACHE = BuildTextCache(1024);

        /// <summary>
        /// 单个跳字
        /// </summary>
        private sealed class Item
        {
            /// <summary>
            /// 载体
            /// </summary>
            public GameObject go;
            /// <summary>
            /// 载体变换
            /// </summary>
            public Transform transform;
            /// <summary>
            /// 文本
            /// </summary>
            public TextMeshPro text;
            /// <summary>
            /// 已存活时间
            /// </summary>
            public float elapsed;
            /// <summary>
            /// 出生位置
            /// </summary>
            public Vector3 origin;
            /// <summary>
            /// 基础颜色 (暴击/普通)
            /// </summary>
            public Color color;
            /// <summary>
            /// 当前透明度, 只在变化时才写顶点色
            /// </summary>
            public float alpha;
        }

        /// <summary>
        /// 世界
        /// </summary>
        private World world => rilbucket.world;
        /// <summary>
        /// 字体, 所有跳字共用
        /// </summary>
        private TMP_FontAsset font { get; set; }
        /// <summary>
        /// 跳字根节点
        /// </summary>
        private GameObject poolroot { get; set; }
        /// <summary>
        /// 正在显示的跳字
        /// </summary>
        private List<Item> busy { get; set; }
        /// <summary>
        /// 空闲的跳字
        /// </summary>
        private List<Item> idle { get; set; }

        protected override void OnCreate()
        {
            base.OnCreate();

            poolroot = new GameObject("DamageTextRoot");
            poolroot.transform.SetParent(world.root.transform, false);

            busy = ObjectPool.Ensure<List<Item>>();
            idle = ObjectPool.Ensure<List<Item>>();

            font = TMP_Settings.defaultFontAsset;
            if (null == font) font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            // 预热: 一次性建好, 运行期不再创建对象
            for (int i = 0; i < CAPACITY; i++)
            {
                var item = Create();
                if (null == item) break;
                idle.Add(item);
            }

            world.ticker.eventor.Listen<TickEvent>(OnTick);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (null != world && null != world.ticker) world.ticker.eventor.UnListen<TickEvent>(OnTick);

            if (null != busy)
            {
                busy.Clear();
                ObjectPool.Set(busy);
                busy = null;
            }

            if (null != idle)
            {
                for (int i = 0; i < idle.Count; i++) DestroyItem(idle[i]);
                idle.Clear();
                ObjectPool.Set(idle);
                idle = null;
            }

            if (null != poolroot)
            {
                Object.Destroy(poolroot);
                poolroot = null;
            }
        }

        protected override void OnSalute(RIL_EVENT_DAMAGE e)
        {
            if (null == e) return;
            if (e.damage <= 0) return;

            var item = Take();
            if (null == item) return;

            item.origin = Position(e.to);
            item.elapsed = 0f;
            item.alpha = 1f;
            item.color = e.crit ? CRIT_COLOR : NORMAL_COLOR;

            // 这里会触发一次 TMP 重建, 每次命中只做一次
            item.text.text = Text(e.damage);
            item.text.fontSize = e.crit ? CRIT_SIZE : SIZE;
            item.text.color = item.color;

            item.transform.position = item.origin;
            item.transform.rotation = Rotation();
            item.transform.localScale = Vector3.one;
            item.go.SetActive(true);

            busy.Add(item);
        }

        /// <summary>
        /// 统一推进所有跳字
        /// </summary>
        /// <param name="e">渲染帧</param>
        private void OnTick(TickEvent e)
        {
            if (0 == busy.Count) return;

            var rotation = Rotation();

            for (int i = busy.Count - 1; i >= 0; i--)
            {
                var item = busy[i];
                item.elapsed += e.tick;

                if (item.elapsed >= DURATION)
                {
                    busy.RemoveAt(i);
                    Recycle(item);
                    continue;
                }

                var t = item.elapsed / DURATION;

                // 上浮用 1-(1-t)^2, 起步快后面慢
                var rest = 1f - t;
                item.transform.position = item.origin + Vector3.up * (RISE * (1f - rest * rest));
                item.transform.rotation = rotation;

                // 出生弹一下再回原大小
                var pop = t < POP_TIME ? 1f + POP * (1f - t / POP_TIME) : 1f;
                item.transform.localScale = Vector3.one * pop;

                // 尾段淡出, 透明度没变就不写顶点色
                var alpha = t < FADE ? 1f : 1f - (t - FADE) / (1f - FADE);
                if (alpha >= item.alpha) continue;

                item.alpha = alpha;
                var color = item.color;
                color.a = alpha;
                item.text.color = color;
            }
        }

        /// <summary>
        /// 取一个空闲跳字, 池子空了就补建, 超过上限则丢弃本次跳字
        /// </summary>
        /// <returns>跳字</returns>
        private Item Take()
        {
            if (0 < idle.Count)
            {
                var index = idle.Count - 1;
                var item = idle[index];
                idle.RemoveAt(index);

                return item;
            }

            if (busy.Count >= CAPACITY) return null;

            return Create();
        }

        /// <summary>
        /// 回收跳字
        /// </summary>
        /// <param name="item">跳字</param>
        private void Recycle(Item item)
        {
            if (null == item) return;

            item.elapsed = 0f;
            item.alpha = 0f;
            if (null != item.go) item.go.SetActive(false);

            idle.Add(item);
        }

        /// <summary>
        /// 创建一个跳字载体
        /// </summary>
        /// <returns>跳字, 字体缺失时返回 null</returns>
        private Item Create()
        {
            if (null == font) return null;
            if (null == poolroot) return null;

            var go = new GameObject("DamageText");
            go.transform.SetParent(poolroot.transform, false);

            var text = go.AddComponent<TextMeshPro>();
            // 共用字体自带的默认材质, 不产生材质实例
            text.font = font;
            text.fontSharedMaterial = font.material;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = SIZE;
            text.fontStyle = FontStyles.Bold;
            text.color = NORMAL_COLOR;
            text.renderer.sortingOrder = 100;
            go.SetActive(false);

            return new Item
            {
                go = go,
                transform = go.transform,
                text = text,
                alpha = 0f,
            };
        }

        /// <summary>
        /// 销毁跳字载体
        /// </summary>
        /// <param name="item">跳字</param>
        private static void DestroyItem(Item item)
        {
            if (null == item || null == item.go) return;

            Object.Destroy(item.go);
        }

        /// <summary>
        /// 跳字出生位置: 单位当前位置 + 竖直偏移 + 水平抖动
        /// </summary>
        /// <param name="actor">ActorID</param>
        /// <returns>世界坐标</returns>
        private Vector3 Position(ulong actor)
        {
            var node = world.GetAgent<NodeAgent>(actor);
            var origin = null != node && null != node.go ? node.go.transform.position : Vector3.zero;

            origin.y += OFFSET_Y;
            origin.x += UnityEngine.Random.Range(-SPREAD, SPREAD);
            origin.z += UnityEngine.Random.Range(-SPREAD, SPREAD);

            return origin;
        }

        /// <summary>
        /// 跳字朝向: 始终与相机同向, 保证正面朝向玩家
        /// </summary>
        /// <returns>旋转</returns>
        private Quaternion Rotation()
        {
            var eyes = null != world ? world.eyes : null;
            if (null == eyes || null == eyes.camera) return Quaternion.identity;

            return eyes.camera.transform.rotation;
        }

        /// <summary>
        /// 数字转字符串, 常见范围直接取缓存
        /// </summary>
        /// <param name="value">数值</param>
        /// <returns>字符串</returns>
        private static string Text(int value)
        {
            if (0 <= value && value < TEXT_CACHE.Length) return TEXT_CACHE[value];

            return value.ToString();
        }

        /// <summary>
        /// 构建数字字符串缓存
        /// </summary>
        /// <param name="capacity">容量</param>
        /// <returns>缓存</returns>
        private static string[] BuildTextCache(int capacity)
        {
            var cache = new string[capacity];
            for (int i = 0; i < capacity; i++) cache[i] = i.ToString();

            return cache;
        }
    }
}
