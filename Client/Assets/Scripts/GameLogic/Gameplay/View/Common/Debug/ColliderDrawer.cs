using System.Collections.Generic;
using GameLogic.Gameplay.Common;
using GameLogic.Gameplay.Logic;
using UnityEngine;
// Logic 命名空间下也有一个用于检测的 Transform, 这里统一指向 Unity 的组件
using Transform = UnityEngine.Transform;

namespace GameLogic.Gameplay.View
{
    /// <summary>
    /// 碰撞盒可视化。池化图元, 每个逻辑帧只刷新一次, 运行期不产生 GC。
    /// 只负责显示: 图元自带的 Collider 在创建时即禁用并销毁, 不参与物理模拟。
    /// 独立组件, 不侵入 ColliderInfo / Detection / Movement 等既有结构。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ColliderDrawer : MonoBehaviour
    {
        /// <summary>是否显示</summary>
        public bool showing = true;
        /// <summary>是否只在逻辑帧推进时刷新, 关闭后每渲染帧刷新</summary>
        public bool followTick = true;

        [Header("Colors")]
        public Color boxColor = new Color(0.15f, 0.9f, 1f, 0.35f);
        public Color sphereColor = new Color(1f, 0.85f, 0.2f, 0.35f);
        public Color capsuleColor = new Color(0.4f, 1f, 0.5f, 0.35f);
        public Color cylinderColor = new Color(1f, 0.5f, 0.2f, 0.35f);

        /// <summary>跟随速度, 与 NodeAgent 一致; 越大越贴近逻辑帧, 越小越平滑但延迟越大</summary>
        private const float FOLLOW_SPEED = 12f;
        /// <summary>贴合阈值(米), 小于该值直接对齐, 避免无限逼近抖动</summary>
        private const float SNAP = 0.001f;

        /// <summary>1..4 对应 COLLISION_DEFINE.COLLIDER_*, 0 位不使用</summary>
        private const int SHAPE_COUNT = 5;
        private const int SHAPE_BOX = COLLISION_DEFINE.COLLIDER_BOX;
        private const int SHAPE_SPHERE = COLLISION_DEFINE.COLLIDER_SPHERE;
        private const int SHAPE_CAPSULE = COLLISION_DEFINE.COLLIDER_CAPSULE;
        private const int SHAPE_CYLINDER = COLLISION_DEFINE.COLLIDER_CYLINDER;

        private Stage m_Stage;
        private Transform m_Root;
        private uint m_Frame = uint.MaxValue;

        private readonly List<Visual>[] m_Pools = new List<Visual>[SHAPE_COUNT];
        private readonly int[] m_Used = new int[SHAPE_COUNT];
        private readonly Material[] m_Materials = new Material[SHAPE_COUNT];

        /// <summary>单个可视化实例, 胶囊需要三个图元, 其余只用到 body</summary>
        private sealed class Visual
        {
            public GameObject gameObject;
            public Transform body;
            public Transform top;
            public Transform bottom;
            /// <summary>本逻辑帧的目标位置 (含 offset)</summary>
            public Vector3 targetPosition;
            /// <summary>本逻辑帧的目标旋转</summary>
            public Quaternion targetRotation;
            /// <summary>是否已完成首次定位, 未经定位的实例直接贴合, 避免从上一个位置飞过来</summary>
            public bool snapped;
        }

        /// <summary>
        /// 绑定逻辑场景, 之后每个逻辑帧同步一次
        /// </summary>
        /// <param name="stage">逻辑场景</param>
        public void Bind(Stage stage)
        {
            m_Stage = stage;
            m_Frame = uint.MaxValue;
            if (null == m_Root)
            {
                var root = new GameObject("ColliderDrawer");
                root.hideFlags = HideFlags.DontSave;
                root.transform.SetParent(transform, false);
                m_Root = root.transform;
            }
        }

        /// <summary>
        /// 解绑, 隐藏所有图元
        /// </summary>
        public void Unbind()
        {
            m_Stage = null;
            m_Frame = uint.MaxValue;
            HideAll();
        }

        /// <summary>
        /// 创建并绑定
        /// </summary>
        /// <param name="stage">逻辑场景</param>
        /// <param name="parent">挂载父节点</param>
        /// <returns>绘制器</returns>
        public static ColliderDrawer Attach(Stage stage, Transform parent = null)
        {
            var go = new GameObject("ColliderDrawer");
            if (null != parent) go.transform.SetParent(parent, false);
            var drawer = go.AddComponent<ColliderDrawer>();
            drawer.Bind(stage);
            return drawer;
        }

        private void LateUpdate()
        {
            if (false == showing || null == m_Stage) return;

            // 逻辑帧只负责采样目标值; 逻辑帧 25Hz, 直接贴合会一卡一卡
            if (false == followTick || m_Frame != m_Stage.frame)
            {
                m_Frame = m_Stage.frame;
                Rebuild();
            }

            // 每渲染帧阻尼插值, 与 NodeAgent 用同一套规则, 保证显示跟模型同步
            Smooth(Time.deltaTime);
        }

        /// <summary>
        /// 渲染帧插值。逻辑帧与渲染帧不对等, 这里把 25fps 的位移摊平到每个渲染帧,
        /// 规则与 NodeAgent 一致 (指数阻尼 + 贴合阈值)。
        /// </summary>
        /// <param name="tick">渲染帧时长</param>
        private void Smooth(float tick)
        {
            float t = tick > 0f ? 1f - Mathf.Exp(-FOLLOW_SPEED * tick) : 1f;

            for (int shape = 1; shape < SHAPE_COUNT; shape++)
            {
                List<Visual> pool = m_Pools[shape];
                if (null == pool) continue;

                int used = m_Used[shape];
                for (int i = 0; i < used; i++)
                {
                    Visual visual = pool[i];
                    Transform visualTransform = visual.gameObject.transform;

                    if (false == visual.snapped)
                    {
                        // 刚出现或刚复用, 直接贴合
                        visual.snapped = true;
                        visualTransform.SetPositionAndRotation(visual.targetPosition, visual.targetRotation);
                        continue;
                    }

                    Vector3 position = Vector3.Lerp(visualTransform.position, visual.targetPosition, t);
                    Quaternion rotation = Quaternion.Slerp(visualTransform.rotation, visual.targetRotation, t);

                    if ((position - visual.targetPosition).sqrMagnitude < SNAP * SNAP)
                    {
                        position = visual.targetPosition;
                        rotation = visual.targetRotation;
                    }

                    visualTransform.SetPositionAndRotation(position, rotation);
                }
            }
        }

        /// <summary>
        /// 从逻辑层取一次数据并刷新所有图元
        /// </summary>
        private void Rebuild()
        {
            for (int i = 0; i < SHAPE_COUNT; i++) m_Used[i] = 0;

            if (false == m_Stage.SeekBehaviorInfos(out List<ColliderInfo> colliders))
            {
                HideUnused();
                return;
            }

            for (int i = 0; i < colliders.Count; i++)
            {
                ColliderInfo collider = colliders[i];
                if (null == collider) continue;

                int shape = collider.shape;
                if (shape <= 0 || shape >= SHAPE_COUNT) continue;

                if (false == m_Stage.SeekBehaviorInfo(collider.actor, out SpatialInfo spatial)) continue;

                Visual visual = Rent(shape);
                Apply(visual, collider, spatial);
            }

            HideUnused();
        }

        /// <summary>
        /// 按形状应用位置与尺寸
        /// </summary>
        private void Apply(Visual visual, ColliderInfo collider, SpatialInfo spatial)
        {
            Vector3 position = ToVector3(spatial.position);
            Quaternion rotation = ToQuaternion(spatial.euler);
            float scale = spatial.scale.AsFloat();

            switch (collider.shape)
            {
                case SHAPE_BOX:
                {
                    BoxShape box = collider.box;
                    Vector3 size = ToVector3(box.size) * scale;
                    Vector3 offset = ToVector3(box.offset) * scale;
                    visual.targetPosition = position + rotation * offset;
                    visual.targetRotation = rotation;
                    visual.gameObject.transform.localScale = new Vector3(
                        Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
                    break;
                }
                case SHAPE_SPHERE:
                {
                    SphereShape sphere = collider.sphere;
                    float diameter = 2f * Mathf.Max(0f, sphere.radius.AsFloat() * scale);
                    Vector3 offset = ToVector3(sphere.offset) * scale;
                    visual.targetPosition = position + rotation * offset;
                    visual.targetRotation = rotation;
                    visual.gameObject.transform.localScale = Vector3.one * diameter;
                    break;
                }
                case SHAPE_CAPSULE:
                {
                    CapsuleShape capsule = collider.capsule;
                    float radius = Mathf.Max(0f, capsule.radius.AsFloat() * scale);
                    float height = Mathf.Max(0f, capsule.height.AsFloat() * scale);
                    // 圆柱段半长, 两端用等比球体补齐, 避免拉成椭球
                    float halfLength = Mathf.Max(0f, 0.5f * height - radius);
                    float diameter = 2f * radius;
                    Vector3 offset = ToVector3(capsule.offset) * scale;

                    visual.targetPosition = position + rotation * offset;
                    visual.targetRotation = rotation;

                    bool hasBody = halfLength > 0f;
                    if (visual.body.gameObject.activeSelf != hasBody) visual.body.gameObject.SetActive(hasBody);
                    visual.body.localScale = new Vector3(diameter, halfLength, diameter);

                    visual.top.localPosition = new Vector3(0f, halfLength, 0f);
                    visual.top.localScale = Vector3.one * diameter;
                    visual.bottom.localPosition = new Vector3(0f, -halfLength, 0f);
                    visual.bottom.localScale = Vector3.one * diameter;
                    break;
                }
                case SHAPE_CYLINDER:
                {
                    CylinderShape cylinder = collider.cylinder;
                    float radius = Mathf.Max(0f, cylinder.radius.AsFloat() * scale);
                    float height = Mathf.Max(0f, cylinder.height.AsFloat() * scale);
                    Vector3 offset = ToVector3(cylinder.offset) * scale;
                    // Unity Cylinder 原型高 2 直径 1
                    visual.targetPosition = position + rotation * offset;
                    visual.targetRotation = rotation;
                    visual.gameObject.transform.localScale = new Vector3(2f * radius, 0.5f * height, 2f * radius);
                    break;
                }
            }
        }

        /// <summary>
        /// 取一个可视实例, 优先复用, 数量不足时才新建
        /// </summary>
        private Visual Rent(int shape)
        {
            List<Visual> pool = m_Pools[shape];
            if (null == pool)
            {
                pool = new List<Visual>(8);
                m_Pools[shape] = pool;
            }

            int used = m_Used[shape];
            if (used < pool.Count)
            {
                Visual reused = pool[used];
                m_Used[shape] = used + 1;
                if (false == reused.gameObject.activeSelf)
                {
                    // 重新启用时位置可能已经变远, 直接贴合而不是插值飞过去
                    reused.snapped = false;
                    reused.gameObject.SetActive(true);
                }
                return reused;
            }

            Visual created = Create(shape);
            pool.Add(created);
            m_Used[shape] = used + 1;
            return created;
        }

        /// <summary>
        /// 新建可视实例
        /// </summary>
        private Visual Create(int shape)
        {
            var visual = new Visual();
            Material material = MaterialOf(shape);

            switch (shape)
            {
                case SHAPE_BOX:
                    visual.gameObject = CreateVisualPrimitive(PrimitiveType.Cube, "Box", material, m_Root);
                    break;
                case SHAPE_SPHERE:
                    visual.gameObject = CreateVisualPrimitive(PrimitiveType.Sphere, "Sphere", material, m_Root);
                    break;
                case SHAPE_CYLINDER:
                    visual.gameObject = CreateVisualPrimitive(PrimitiveType.Cylinder, "Cylinder", material, m_Root);
                    break;
                case SHAPE_CAPSULE:
                {
                    visual.gameObject = new GameObject("Capsule");
                    visual.gameObject.hideFlags = HideFlags.DontSave;
                    visual.gameObject.transform.SetParent(m_Root, false);
                    visual.body = CreateVisualPrimitive(PrimitiveType.Cylinder, "Body", material, visual.gameObject.transform).transform;
                    visual.top = CreateVisualPrimitive(PrimitiveType.Sphere, "Top", material, visual.gameObject.transform).transform;
                    visual.bottom = CreateVisualPrimitive(PrimitiveType.Sphere, "Bottom", material, visual.gameObject.transform).transform;
                    break;
                }
            }

            return visual;
        }

        /// <summary>
        /// 创建图元, 并立即禁用/销毁自带 Collider
        /// </summary>
        private static GameObject CreateVisualPrimitive(PrimitiveType type, string name, Material material, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one;

            Collider primitiveCollider = go.GetComponent<Collider>();
            if (null != primitiveCollider)
            {
                primitiveCollider.enabled = false;
                Destroy(primitiveCollider);
            }

            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (null != meshRenderer)
            {
                meshRenderer.sharedMaterial = material;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            }

            return go;
        }

        /// <summary>
        /// 每类形状一份共享材质, 只在首次创建时生成
        /// </summary>
        private Material MaterialOf(int shape)
        {
            Material material = m_Materials[shape];
            if (null != material) return material;

            material = new Material(FindShader());
            material.name = "ColliderDrawer " + shape;
            material.hideFlags = HideFlags.DontSave;
            ApplyColor(material, ColorOf(shape));
            m_Materials[shape] = material;
            return material;
        }

        private Color ColorOf(int shape) => shape switch
        {
            SHAPE_BOX => boxColor,
            SHAPE_SPHERE => sphereColor,
            SHAPE_CAPSULE => capsuleColor,
            SHAPE_CYLINDER => cylinderColor,
            _ => Color.magenta,
        };

        /// <summary>
        /// 半透明混合参数, 兼容内置管线与 URP
        /// </summary>
        private static void ApplyColor(Material material, Color color)
        {
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);

            bool transparent = color.a < 0.999f;
            if (material.HasProperty("_Mode")) material.SetFloat("_Mode", transparent ? 2f : 0f);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", transparent ? 1f : 0f);
            if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)(transparent ? UnityEngine.Rendering.BlendMode.SrcAlpha : UnityEngine.Rendering.BlendMode.One));
            if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)(transparent ? UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : UnityEngine.Rendering.BlendMode.Zero));
            if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", transparent ? 0 : 1);

            material.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            if (transparent)
            {
                material.EnableKeyword("_ALPHABLEND_ON");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            material.renderQueue = transparent
                ? (int)UnityEngine.Rendering.RenderQueue.Transparent
                : (int)UnityEngine.Rendering.RenderQueue.Geometry;
        }

        private static Shader FindShader()
        {
            bool scriptable = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            if (scriptable)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (null != shader) return shader;
                shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                if (null != shader) return shader;
            }

            Shader fallback = Shader.Find("Unlit/Color");
            if (null != fallback) return fallback;
            fallback = Shader.Find("Sprites/Default");
            if (null != fallback) return fallback;

            return Shader.Find("Hidden/InternalErrorShader");
        }

        /// <summary>
        /// 隐藏本帧没用到的实例
        /// </summary>
        private void HideUnused()
        {
            for (int shape = 1; shape < SHAPE_COUNT; shape++)
            {
                List<Visual> pool = m_Pools[shape];
                if (null == pool) continue;

                for (int i = m_Used[shape]; i < pool.Count; i++)
                {
                    GameObject go = pool[i].gameObject;
                    if (null != go && go.activeSelf) go.SetActive(false);
                }
            }
        }

        private void HideAll()
        {
            for (int shape = 1; shape < SHAPE_COUNT; shape++)
            {
                List<Visual> pool = m_Pools[shape];
                if (null == pool) continue;
                for (int i = 0; i < pool.Count; i++)
                {
                    GameObject go = pool[i].gameObject;
                    if (null != go && go.activeSelf) go.SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            // 图元都是本节点的子物体, 随节点一起销毁, 这里只需回收材质
            for (int shape = 1; shape < SHAPE_COUNT; shape++)
            {
                Material material = m_Materials[shape];
                if (null != material) Destroy(material);
                m_Materials[shape] = null;

                List<Visual> pool = m_Pools[shape];
                if (null != pool) pool.Clear();
            }
            m_Root = null;
            m_Stage = null;
        }

        private static Vector3 ToVector3(FPVector3 value) =>
            new Vector3(value.X.AsFloat(), value.Y.AsFloat(), value.Z.AsFloat());

        /// <summary>逻辑层欧拉角单位为度</summary>
        private static Quaternion ToQuaternion(FPVector3 euler) =>
            Quaternion.Euler(euler.X.AsFloat(), euler.Y.AsFloat(), euler.Z.AsFloat());
    }
}
