
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
        /// 高度
        /// </summary>
        public FP height { get; set; }
    }

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
        /// 高度
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
        }

        protected override BehaviorInfo OnClone()
        {
            var clone = ObjectCache.Ensure<ColliderInfo>();
            clone.Ready(actor);
            clone.layer = layer;
            clone.shape = shape;
            clone.box = box;
            clone.sphere = sphere;

            return clone;
        }
    }
}