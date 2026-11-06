
using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{

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
    /// 立方体
    /// </summary>
    public struct BoxShape
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
    public struct SphereShape
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
    public struct CapsuleShape
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
        /// <summary>
        /// 两端球心距离的一半, 由 height 与 radius 推导
        /// </summary>
        public FP halfLength => FP.Max(FP.Zero, (height - FP.Two * radius) * FP.Half);
    }

    /// <summary>
    /// 圆柱体
    /// </summary>
    public struct CylinderShape
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
        /// <summary>
        /// 轴向半高
        /// </summary>
        public FP halfHeight => height * FP.Half;
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
        public BoxShape box { get; set; }
        /// <summary>
        /// 球体
        /// </summary>
        public SphereShape sphere { get; set; }
        /// <summary>
        /// 胶囊体
        /// </summary>
        public CapsuleShape capsule { get; set; }
        /// <summary>
        /// 圆柱体
        /// </summary>
        public CylinderShape cylinder { get; set; }

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

            return clone;
        }
    }
}
