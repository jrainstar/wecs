using GameLogic.Gameplay.Common;

namespace GameLogic.Gameplay.Logic
{
    /// <summary>
    /// 几何体基类。Center 为相对 Actor 原点的局部偏移，其余参数为不带缩放的局部尺寸。
    /// </summary>
    public abstract class BaseShape
    {
        /// <summary>
        /// 类型
        /// </summary>
        public abstract ShapeType Type { get; }
        /// <summary>
        /// 局部偏移
        /// </summary>
        public abstract FPVector3 Center { get; }
        /// <summary>
        /// 局部尺寸缩放
        /// </summary>
        /// <param name="scale">缩放</param>
        /// <returns>缩放后的形状副本</returns>
        public abstract BaseShape Scaled(FP scale);
    }

    /// <summary>
    /// 立方体, Size 为三个轴的全尺寸
    /// </summary>
    public sealed class BoxShape : BaseShape
    {
        private readonly FPVector3 m_Center;
        private readonly FPVector3 m_Size;

        public override ShapeType Type => ShapeType.Box;
        public override FPVector3 Center => m_Center;
        /// <summary>
        /// 全尺寸
        /// </summary>
        public FPVector3 Size => m_Size;
        /// <summary>
        /// 半尺寸
        /// </summary>
        public FPVector3 HalfSize => m_Size * FP.Half;

        public BoxShape(FPVector3 center, FPVector3 size)
        {
            m_Center = center;
            m_Size = size;
        }

        public override BaseShape Scaled(FP scale) => new BoxShape(m_Center * scale, m_Size * scale);
    }

    /// <summary>
    /// 球体
    /// </summary>
    public sealed class SphereShape : BaseShape
    {
        private readonly FPVector3 m_Center;
        private readonly FP m_Radius;

        public override ShapeType Type => ShapeType.Sphere;
        public override FPVector3 Center => m_Center;
        /// <summary>
        /// 半径
        /// </summary>
        public FP Radius => m_Radius;

        public SphereShape(FPVector3 center, FP radius)
        {
            m_Center = center;
            m_Radius = radius;
        }

        public override BaseShape Scaled(FP scale) => new SphereShape(m_Center * scale, m_Radius * scale);
    }

    /// <summary>
    /// 胶囊体, 局部 Y 轴为长轴。Height 为包含两端半球的全高, 内部圆柱段半长为 (Height - 2 * Radius) / 2。
    /// 要求 Height >= 2 * Radius, 否则视为非法尺寸。
    /// </summary>
    public sealed class CapsuleShape : BaseShape
    {
        private readonly FPVector3 m_Center;
        private readonly FP m_Radius;
        private readonly FP m_Height;

        public override ShapeType Type => ShapeType.Capsule;
        public override FPVector3 Center => m_Center;
        /// <summary>
        /// 半径
        /// </summary>
        public FP Radius => m_Radius;
        /// <summary>
        /// 胶囊全高, 包含两端半球
        /// </summary>
        public FP Height => m_Height;
        /// <summary>
        /// 两端球心距离的一半, 由 Height 与 Radius 推导
        /// </summary>
        public FP HalfLength => FP.Max(FP.Zero, (m_Height - FP.Two * m_Radius) * FP.Half);

        public CapsuleShape(FPVector3 center, FP radius, FP height)
        {
            m_Center = center;
            m_Radius = radius;
            m_Height = height;
        }

        public override BaseShape Scaled(FP scale) => new CapsuleShape(m_Center * scale, m_Radius * scale, m_Height * scale);
    }

    /// <summary>
    /// 圆柱体, 局部 Y 轴为长轴。Height 为沿轴全高, 两端为平面。
    /// </summary>
    public sealed class CylinderShape : BaseShape
    {
        private readonly FPVector3 m_Center;
        private readonly FP m_Radius;
        private readonly FP m_Height;

        public override ShapeType Type => ShapeType.Cylinder;
        public override FPVector3 Center => m_Center;
        /// <summary>
        /// 半径
        /// </summary>
        public FP Radius => m_Radius;
        /// <summary>
        /// 沿局部 Y 轴的全高
        /// </summary>
        public FP Height => m_Height;
        /// <summary>
        /// 轴向半高
        /// </summary>
        public FP HalfHeight => m_Height * FP.Half;

        public CylinderShape(FPVector3 center, FP radius, FP height)
        {
            m_Center = center;
            m_Radius = radius;
            m_Height = height;
        }

        public override BaseShape Scaled(FP scale) => new CylinderShape(m_Center * scale, m_Radius * scale, m_Height * scale);
    }
}
