using System;

namespace GameLogic.Gameplay.Common
{
    public struct FPMatrix2x2
    {
        public FP M11, M12;
        public FP M21, M22;

        // 单位矩阵
        public static readonly FPMatrix2x2 Identity = new FPMatrix2x2(1, 0, 0, 1);

        public FPMatrix2x2(FP m11, FP m12, FP m21, FP m22)
        {
            this.M11 = m11;
            this.M12 = m12;
            this.M21 = m21;
            this.M22 = m22;
        }

        // 创建旋转矩阵
        public static FPMatrix2x2 CreateRotation(FP radians)
        {
            FP cos = FP.Cos(radians);
            FP sin = FP.Sin(radians);
            return new FPMatrix2x2(cos, -sin, sin, cos);
        }

        // 创建缩放矩阵
        public static FPMatrix2x2 CreateScale(FPVector2 scale)
        {
            return new FPMatrix2x2(scale.X, FP.Zero, FP.Zero, scale.Y);
        }

        // 矩阵乘法
        public static FPMatrix2x2 operator *(FPMatrix2x2 a, FPMatrix2x2 b)
        {
            return new FPMatrix2x2
            (
                a.M11 * b.M11 + a.M12 * b.M21,
                a.M11 * b.M12 + a.M12 * b.M22,
                a.M21 * b.M11 + a.M22 * b.M21,
                a.M21 * b.M12 + a.M22 * b.M22
            );
        }

        // 计算行列式
        public FP Determinant()
        {
            return M11 * M22 - M12 * M21;
        }

        // 计算逆矩阵
        public FPMatrix2x2 Inverse()
        {
            FP det = Determinant();
            if (FP.Abs(det) < FP.Epsilon)
                throw new InvalidOperationException("Matrix is not invertible");

            FP invDet = FP.One / det;
            return new FPMatrix2x2
            (
                M22 * invDet, -M12 * invDet,
                -M21 * invDet, M11 * invDet
            );
        }

        // 变换向量
        public FPVector2 Transform(FPVector2 v)
        {
            return new FPVector2
            (
                M11 * v.X + M12 * v.Y,
                M21 * v.X + M22 * v.Y
            );
        }
    }
}
