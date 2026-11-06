
using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 立方体
    /// </summary>
    public struct Box
    {
        /// <summary>
        /// 偏移
        /// </summary>
        public FPVector3 offset { get; set; }
        /// <summary>
        /// 尺寸
        /// </summary>
        public FPVector3 size { get; set; }
    }

    /// <summary>
    /// 球体
    /// </summary>
    public struct Sphere
    {
        /// <summary>
        /// 偏移
        /// </summary>
        public FPVector3 offset { get; set; }
        /// <summary>
        /// 半径
        /// </summary>
        public FP radius { get; set; }
    }

    /// <summary>
    /// 胶囊体
    /// </summary>
    public struct Capsule
    {
        /// <summary>
        /// 偏移
        /// </summary>
        public FPVector3 offset { get; set; }
        /// <summary>
        /// 半径
        /// </summary>
        public FP radius { get; set; }
        /// <summary>
        /// 高度 (包含两端半球的全高)
        /// </summary>
        public FP height { get; set; }
    }

    /// <summary>
    /// 圆柱体
    /// </summary>
    public struct Cylinder
    {
        /// <summary>
        /// 偏移
        /// </summary>
        public FPVector3 offset { get; set; }
        /// <summary>
        /// 半径
        /// </summary>
        public FP radius { get; set; }
        /// <summary>
        /// 高度 (沿轴全高)
        /// </summary>
        public FP height { get; set; }
    }

    public struct AABB
    {
        /// <summary>
        /// 位置
        /// </summary>
        public FPVector3 position { get; set; }
        /// <summary>
        /// 尺寸
        /// </summary>
        public FPVector3 size { get; set; }
    }

    /// <summary>
    /// 碰撞盒
    /// </summary>
    public class ColliderInfo : BehaviorInfo
    {
        /// <summary>
        /// 碰撞层
        /// </summary>
        public int layer { get; set; }
        /// <summary>
        /// 几何体类型
        /// </summary>
        public byte shape { get; set; }
        /// <summary>
        /// 立方体
        /// </summary>
        public Box box { get; set; }
        /// <summary>
        /// 球体
        /// </summary>
        public Sphere sphere { get; set; }
        /// <summary>
        /// 胶囊体
        /// </summary>
        public Capsule capsule { get; set; }
        /// <summary>
        /// 圆柱体
        /// </summary>
        public Cylinder cylinder { get; set; }

        private BaseShape m_Shape;

        /// <summary>
        /// 当前几何体 (按 shape 字段与尺寸数据惰性构造, 尺寸变化时自动重建)
        /// </summary>
        public BaseShape geometry
        {
            get
            {
                if (null != m_Shape) return m_Shape;
                m_Shape = Build();
                return m_Shape;
            }
        }

        /// <summary>
        /// 使几何体缓存失效, 尺寸数据被外部修改后调用
        /// </summary>
        public void Invalidate() => m_Shape = null;

        /// <summary>
        /// 按当前数据构造几何体
        /// </summary>
        private BaseShape Build()
        {
            switch (shape)
            {
                case COLLISION_DEFINE.COLLIDER_BOX:
                    return new BoxShape(box.offset, box.size);
                case COLLISION_DEFINE.COLLIDER_SPHERE:
                    return new SphereShape(sphere.offset, sphere.radius);
                case COLLISION_DEFINE.COLLIDER_CAPSULE:
                    return new CapsuleShape(capsule.offset, capsule.radius, capsule.height);
                case COLLISION_DEFINE.COLLIDER_CYLINDER:
                    return new CylinderShape(cylinder.offset, cylinder.radius, cylinder.height);
                default:
                    return null;
            }
        }

        protected override void OnReady()
        {
            OnReset();
        }

        protected override void OnReset()
        {
            layer = COLLISION_DEFINE.LAYER_DEFAULT;
            shape = 0;
            box = default;
            sphere = default;
            capsule = default;
            cylinder = default;
            m_Shape = null;
        }

        protected override BehaviorInfo OnClone()
        {
            var clone = ObjectCache.Ensure<ColliderInfo>();
            clone.Ready(actor);
            clone.layer = layer;
            clone.shape = shape;
            clone.box = box;
            clone.sphere = sphere;
            clone.capsule = capsule;
            clone.cylinder = cylinder;
            clone.m_Shape = null;

            return clone;
        }
    }
}
