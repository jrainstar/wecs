/* Copyright (C) <2009-2011> <Thorben Linneweber, Jitter Physics>
 *
 *  This software is provided 'as-is', without any express or implied
 *  warranty.  In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *
 *  1. The origin of this software must not be misrepresented; you must not
 *      claim that you wrote the original software. If you use this software
 *      in a product, an acknowledgment in the product documentation would be
 *      appreciated but is not required.
 *  2. Altered source versions must be plainly marked as such, and must not be
 *      misrepresented as being the original software.
 *  3. This notice may not be removed or altered from any source distribution.
 */

using System;

namespace GameLogic.Gameplay.Common
{

    /// <summary>
    /// A Quaternion representing an orientation.
    /// </summary>
    [Serializable]
    public struct FPQuaternion
    {

        /// <summary>The X component of the quaternion.</summary>
        public FP x;
        /// <summary>The Y component of the quaternion.</summary>
        public FP y;
        /// <summary>The Z component of the quaternion.</summary>
        public FP z;
        /// <summary>The W component of the quaternion.</summary>
        public FP w;

        public static readonly FPQuaternion identity;

        static FPQuaternion()
        {
            identity = new FPQuaternion(0, 0, 0, 1);
        }

        /// <summary>
        /// Initializes a new instance of the JQuaternion structure.
        /// </summary>
        /// <param name="x">The X component of the quaternion.</param>
        /// <param name="y">The Y component of the quaternion.</param>
        /// <param name="z">The Z component of the quaternion.</param>
        /// <param name="w">The W component of the quaternion.</param>
        public FPQuaternion(FP x, FP y, FP z, FP w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public void Set(FP new_x, FP new_y, FP new_z, FP new_w)
        {
            x = new_x;
            y = new_y;
            z = new_z;
            w = new_w;
        }

        public void SetFromToRotation(FPVector3 fromDirection, FPVector3 toDirection)
        {
            FPQuaternion targetRotation = FromToRotation(fromDirection, toDirection);
            Set(targetRotation.x, targetRotation.y, targetRotation.z, targetRotation.w);
        }

        public FPVector3 EulerAngles
        {
            get
            {
                return ToEulerAngles();
            }
        }
        public FPVector3 ToEulerAngles()
        {
            FPVector3 eulerAngles = new FPVector3();

            // 计算 Pitch (X)
            FP sinP = FP.Two * (w * x - z * y);

            if (FP.Abs(sinP) >= FP.One)
            {
                eulerAngles.X = FP.PI / FP.Two * FP.Sign(sinP); // 处理万向锁情况
            }
            else
            {
                eulerAngles.X = FP.Asin(sinP);
            }

            // 计算 Yaw (Y)
            FP sinY_cosp = FP.Two * (w * z + y * x);
            FP cosY_cosp = FP.One - FP.Two * (x * x + z * z);
            eulerAngles.Z = FP.Atan2(sinY_cosp, cosY_cosp);

            // 计算 Roll (Z)
            FP sinR_cosp = FP.Two * (w * y + x * z);
            FP cosR_cosp = FP.One - FP.Two * (y * y + x * x);
            eulerAngles.Y = FP.Atan2(sinR_cosp, cosR_cosp);

            // 弧度转角度
            eulerAngles *= FP.Rad2Deg;

            return eulerAngles;
        }

        public static FP Angle(FPQuaternion a, FPQuaternion b)
        {
            FPQuaternion aInv = Inverse(a);
            FPQuaternion f = b * aInv;

            FP angle = FP.Acos(f.w) * 2 * FP.Rad2Deg;

            if (angle > 180)
            {
                angle = 360 - angle;
            }

            return angle;
        }

        /// <summary>
        /// Quaternions are added.
        /// </summary>
        /// <param name="quaternion1">The first quaternion.</param>
        /// <param name="quaternion2">The second quaternion.</param>
        /// <returns>The sum of both quaternions.</returns>
        #region public static JQuaternion Add(JQuaternion quaternion1, JQuaternion quaternion2)
        public static FPQuaternion Add(FPQuaternion quaternion1, FPQuaternion quaternion2)
        {
            FPQuaternion result;
            Add(ref quaternion1, ref quaternion2, out result);
            return result;
        }

        public static FPQuaternion LookRotation(FPVector3 forward)
        {
            return CreateFromMatrix(FPMatrix.LookAt(forward, FPVector3.Up));
        }

        public static FPQuaternion LookRotation(FPVector3 forward, FPVector3 upwards)
        {
            return CreateFromMatrix(FPMatrix.LookAt(forward, upwards));
        }

        public static FPQuaternion Slerp(FPQuaternion from, FPQuaternion to, FP t)
        {
            t = FPMath.Clamp(t, 0, 1);

            FP dot = Dot(from, to);

            if (dot < FP.Zero)
            {
                to = Multiply(to, -1);
                dot = -dot;
            }

            FP halfTheta = FP.Acos(dot);

            return Multiply(Multiply(from, FP.Sin((1 - t) * halfTheta)) + Multiply(to, FP.Sin(t * halfTheta)), 1 / FP.Sin(halfTheta));
        }

        public static FPQuaternion RotateTowards(FPQuaternion from, FPQuaternion to, FP maxDegreesDelta)
        {
            FP dot = Dot(from, to);

            if (dot < FP.Zero)
            {
                to = Multiply(to, -1);
                dot = -dot;
            }

            FP halfTheta = FP.Acos(dot);
            FP theta = halfTheta * 2;

            maxDegreesDelta *= FP.Deg2Rad;

            if (maxDegreesDelta >= theta)
            {
                return to;
            }

            maxDegreesDelta /= theta;

            return Multiply(Multiply(from, FP.Sin((1 - maxDegreesDelta) * halfTheta)) + Multiply(to, FP.Sin(maxDegreesDelta * halfTheta)), 1 / FP.Sin(halfTheta));
        }

        public static FPQuaternion Euler(FP x, FP y, FP z)
        {
            x *= FP.Deg2Rad;
            y *= FP.Deg2Rad;
            z *= FP.Deg2Rad;

            FPQuaternion rotation;
            CreateFromYawPitchRoll(y, x, z, out rotation);

            return rotation;
        }

        public static FPQuaternion Euler(FPVector3 eulerAngles)
        {
            return Euler(eulerAngles.X, eulerAngles.Y, eulerAngles.Z);
        }

        public static FPQuaternion AngleAxis(FP angle, FPVector3 axis)
        {
            axis = axis * FP.Deg2Rad;
            axis.Normalize();

            FP halfAngle = angle * FP.Deg2Rad * FP.Half;

            FPQuaternion rotation;
            FP sin = FP.Sin(halfAngle);

            rotation.x = axis.X * sin;
            rotation.y = axis.Y * sin;
            rotation.z = axis.Z * sin;
            rotation.w = FP.Cos(halfAngle);

            return rotation;
        }

        public static void CreateFromYawPitchRoll(FP yaw, FP pitch, FP roll, out FPQuaternion result)
        {
            FP halfRoll = roll * FP.Half;
            FP sinRoll = FP.Sin(halfRoll);
            FP cosRoll = FP.Cos(halfRoll);
            FP halfPitch = pitch * FP.Half;
            FP sinPitch = FP.Sin(halfPitch);
            FP cosPitch = FP.Cos(halfPitch);
            FP halfYaw = yaw * FP.Half;
            FP sinYaw = FP.Sin(halfYaw);
            FP cosYaw = FP.Cos(halfYaw);
            result.x = ((cosYaw * sinPitch) * cosRoll) + ((sinYaw * cosPitch) * sinRoll);
            result.y = ((sinYaw * cosPitch) * cosRoll) - ((cosYaw * sinPitch) * sinRoll);
            result.z = ((cosYaw * cosPitch) * sinRoll) - ((sinYaw * sinPitch) * cosRoll);
            result.w = ((cosYaw * cosPitch) * cosRoll) + ((sinYaw * sinPitch) * sinRoll);
        }

        /// <summary>
        /// Quaternions are added.
        /// </summary>
        /// <param name="quaternion1">The first quaternion.</param>
        /// <param name="quaternion2">The second quaternion.</param>
        /// <param name="result">The sum of both quaternions.</param>
        public static void Add(ref FPQuaternion quaternion1, ref FPQuaternion quaternion2, out FPQuaternion result)
        {
            result.x = quaternion1.x + quaternion2.x;
            result.y = quaternion1.y + quaternion2.y;
            result.z = quaternion1.z + quaternion2.z;
            result.w = quaternion1.w + quaternion2.w;
        }
        #endregion

        public static FPQuaternion Conjugate(FPQuaternion value)
        {
            FPQuaternion quaternion;
            quaternion.x = -value.x;
            quaternion.y = -value.y;
            quaternion.z = -value.z;
            quaternion.w = value.w;
            return quaternion;
        }

        public static FP Dot(FPQuaternion a, FPQuaternion b)
        {
            return a.w * b.w + a.x * b.x + a.y * b.y + a.z * b.z;
        }

        public static FPQuaternion Inverse(FPQuaternion rotation)
        {
            FP invNorm = FP.One / ((rotation.x * rotation.x) + (rotation.y * rotation.y) + (rotation.z * rotation.z) + (rotation.w * rotation.w));
            return Multiply(Conjugate(rotation), invNorm);
        }

        public static FPQuaternion FromToRotation(FPVector3 fromVector3, FPVector3 toVector3)
        {
            FPVector3 w = FPVector3.Cross(fromVector3, toVector3);
            FPQuaternion q = new FPQuaternion(w.X, w.Y, w.Z, FPVector3.Dot(fromVector3, toVector3));
            q.w += FP.Sqrt(fromVector3.sqrmagnitude * toVector3.sqrmagnitude);
            q.Normalize();

            return q;
        }

        public static FPQuaternion Lerp(FPQuaternion a, FPQuaternion b, FP t)
        {
            t = FPMath.Clamp(t, FP.Zero, FP.One);

            return LerpUnclamped(a, b, t);
        }

        public static FPQuaternion LerpUnclamped(FPQuaternion a, FPQuaternion b, FP t)
        {
            FPQuaternion result = Multiply(a, (1 - t)) + Multiply(b, t);
            result.Normalize();

            return result;
        }

        /// <summary>
        /// Quaternions are subtracted.
        /// </summary>
        /// <param name="quaternion1">The first quaternion.</param>
        /// <param name="quaternion2">The second quaternion.</param>
        /// <returns>The difference of both quaternions.</returns>
        #region public static JQuaternion Subtract(JQuaternion quaternion1, JQuaternion quaternion2)
        public static FPQuaternion Subtract(FPQuaternion quaternion1, FPQuaternion quaternion2)
        {
            FPQuaternion result;
            Subtract(ref quaternion1, ref quaternion2, out result);
            return result;
        }

        /// <summary>
        /// Quaternions are subtracted.
        /// </summary>
        /// <param name="quaternion1">The first quaternion.</param>
        /// <param name="quaternion2">The second quaternion.</param>
        /// <param name="result">The difference of both quaternions.</param>
        public static void Subtract(ref FPQuaternion quaternion1, ref FPQuaternion quaternion2, out FPQuaternion result)
        {
            result.x = quaternion1.x - quaternion2.x;
            result.y = quaternion1.y - quaternion2.y;
            result.z = quaternion1.z - quaternion2.z;
            result.w = quaternion1.w - quaternion2.w;
        }
        #endregion

        /// <summary>
        /// Multiply two quaternions.
        /// </summary>
        /// <param name="quaternion1">The first quaternion.</param>
        /// <param name="quaternion2">The second quaternion.</param>
        /// <returns>The product of both quaternions.</returns>
        #region public static JQuaternion Multiply(JQuaternion quaternion1, JQuaternion quaternion2)
        public static FPQuaternion Multiply(FPQuaternion quaternion1, FPQuaternion quaternion2)
        {
            FPQuaternion result;
            Multiply(ref quaternion1, ref quaternion2, out result);
            return result;
        }

        /// <summary>
        /// Multiply two quaternions.
        /// </summary>
        /// <param name="quaternion1">The first quaternion.</param>
        /// <param name="quaternion2">The second quaternion.</param>
        /// <param name="result">The product of both quaternions.</param>
        public static void Multiply(ref FPQuaternion quaternion1, ref FPQuaternion quaternion2, out FPQuaternion result)
        {
            FP x = quaternion1.x;
            FP y = quaternion1.y;
            FP z = quaternion1.z;
            FP w = quaternion1.w;
            FP num4 = quaternion2.x;
            FP num3 = quaternion2.y;
            FP num2 = quaternion2.z;
            FP num = quaternion2.w;
            FP num12 = (y * num2) - (z * num3);
            FP num11 = (z * num4) - (x * num2);
            FP num10 = (x * num3) - (y * num4);
            FP num9 = ((x * num4) + (y * num3)) + (z * num2);
            result.x = ((x * num) + (num4 * w)) + num12;
            result.y = ((y * num) + (num3 * w)) + num11;
            result.z = ((z * num) + (num2 * w)) + num10;
            result.w = (w * num) - num9;
        }
        #endregion

        /// <summary>
        /// Scale a quaternion
        /// </summary>
        /// <param name="quaternion1">The quaternion to scale.</param>
        /// <param name="scaleFactor">Scale factor.</param>
        /// <returns>The scaled quaternion.</returns>
        #region public static JQuaternion Multiply(JQuaternion quaternion1, FP scaleFactor)
        public static FPQuaternion Multiply(FPQuaternion quaternion1, FP scaleFactor)
        {
            FPQuaternion result;
            Multiply(ref quaternion1, scaleFactor, out result);
            return result;
        }

        /// <summary>
        /// Scale a quaternion
        /// </summary>
        /// <param name="quaternion1">The quaternion to scale.</param>
        /// <param name="scaleFactor">Scale factor.</param>
        /// <param name="result">The scaled quaternion.</param>
        public static void Multiply(ref FPQuaternion quaternion1, FP scaleFactor, out FPQuaternion result)
        {
            result.x = quaternion1.x * scaleFactor;
            result.y = quaternion1.y * scaleFactor;
            result.z = quaternion1.z * scaleFactor;
            result.w = quaternion1.w * scaleFactor;
        }
        #endregion

        /// <summary>
        /// Sets the length of the quaternion to one.
        /// </summary>
        #region public void Normalize()
        public void Normalize()
        {
            FP num2 = (((x * x) + (y * y)) + (z * z)) + (w * w);
            FP num = 1 / (FP.Sqrt(num2));
            x *= num;
            y *= num;
            z *= num;
            w *= num;
        }
        #endregion

        public static FPQuaternion CreateFromAxisAngle(FPVector3 axis, FP angle)
        {
            // 确保旋转轴是单位向量
            axis = axis.Normalized;

            // 将角度减半并计算正弦和余弦值
            FP halfAngle = angle * FP.Half;
            FP sinHalfAngle = FPMath.Sin(halfAngle);
            FP cosHalfAngle = FPMath.Cos(halfAngle);

            // 使用公式生成四元数
            return new FPQuaternion(
                axis.X * sinHalfAngle,
                axis.Y * sinHalfAngle,
                axis.Z * sinHalfAngle,
                cosHalfAngle
            );
        }

        /// <summary>
        /// Creates a quaternion from a matrix.
        /// </summary>
        /// <param name="matrix">A matrix representing an orientation.</param>
        /// <returns>JQuaternion representing an orientation.</returns>
        #region public static JQuaternion CreateFromMatrix(JMatrix matrix)
        public static FPQuaternion CreateFromMatrix(FPMatrix matrix)
        {
            FPQuaternion result;
            CreateFromMatrix(ref matrix, out result);
            return result;
        }

        /// <summary>
        /// Creates a quaternion from a matrix.
        /// </summary>
        /// <param name="matrix">A matrix representing an orientation.</param>
        /// <param name="result">JQuaternion representing an orientation.</param>
        public static void CreateFromMatrix(ref FPMatrix matrix, out FPQuaternion result)
        {
            FP num8 = (matrix.M11 + matrix.M22) + matrix.M33;
            if (num8 > FP.Zero)
            {
                FP num = FP.Sqrt((num8 + FP.One));
                result.w = num * FP.Half;
                num = FP.Half / num;
                result.x = (matrix.M23 - matrix.M32) * num;
                result.y = (matrix.M31 - matrix.M13) * num;
                result.z = (matrix.M12 - matrix.M21) * num;
            }
            else if ((matrix.M11 >= matrix.M22) && (matrix.M11 >= matrix.M33))
            {
                FP num7 = FP.Sqrt((((FP.One + matrix.M11) - matrix.M22) - matrix.M33));
                FP num4 = FP.Half / num7;
                result.x = FP.Half * num7;
                result.y = (matrix.M12 + matrix.M21) * num4;
                result.z = (matrix.M13 + matrix.M31) * num4;
                result.w = (matrix.M23 - matrix.M32) * num4;
            }
            else if (matrix.M22 > matrix.M33)
            {
                FP num6 = FP.Sqrt((((FP.One + matrix.M22) - matrix.M11) - matrix.M33));
                FP num3 = FP.Half / num6;
                result.x = (matrix.M21 + matrix.M12) * num3;
                result.y = FP.Half * num6;
                result.z = (matrix.M32 + matrix.M23) * num3;
                result.w = (matrix.M31 - matrix.M13) * num3;
            }
            else
            {
                FP num5 = FP.Sqrt((((FP.One + matrix.M33) - matrix.M11) - matrix.M22));
                FP num2 = FP.Half / num5;
                result.x = (matrix.M31 + matrix.M13) * num2;
                result.y = (matrix.M32 + matrix.M23) * num2;
                result.z = FP.Half * num5;
                result.w = (matrix.M12 - matrix.M21) * num2;
            }
        }
        #endregion

        /// <summary>
        /// Multiply two quaternions.
        /// </summary>
        /// <param name="value1">The first quaternion.</param>
        /// <param name="value2">The second quaternion.</param>
        /// <returns>The product of both quaternions.</returns>
        #region public static FP operator *(JQuaternion value1, JQuaternion value2)
        public static FPQuaternion operator *(FPQuaternion value1, FPQuaternion value2)
        {
            FPQuaternion result;
            Multiply(ref value1, ref value2, out result);
            return result;
        }
        #endregion

        /// <summary>
        /// Add two quaternions.
        /// </summary>
        /// <param name="value1">The first quaternion.</param>
        /// <param name="value2">The second quaternion.</param>
        /// <returns>The sum of both quaternions.</returns>
        #region public static FP operator +(JQuaternion value1, JQuaternion value2)
        public static FPQuaternion operator +(FPQuaternion value1, FPQuaternion value2)
        {
            FPQuaternion result;
            Add(ref value1, ref value2, out result);
            return result;
        }
        #endregion

        /// <summary>
        /// Subtract two quaternions.
        /// </summary>
        /// <param name="value1">The first quaternion.</param>
        /// <param name="value2">The second quaternion.</param>
        /// <returns>The difference of both quaternions.</returns>
        #region public static FP operator -(JQuaternion value1, JQuaternion value2)
        public static FPQuaternion operator -(FPQuaternion value1, FPQuaternion value2)
        {
            FPQuaternion result;
            Subtract(ref value1, ref value2, out result);
            return result;
        }
        #endregion

        /**
         *  @brief Rotates a {@link TSVector} by the {@link TSQuanternion}.
         **/
        public static FPVector3 operator *(FPQuaternion quat, FPVector3 vec)
        {
            FP num = quat.x * 2 * FP.One;
            FP num2 = quat.y * 2 * FP.One;
            FP num3 = quat.z * 2 * FP.One;
            FP num4 = quat.x * num;
            FP num5 = quat.y * num2;
            FP num6 = quat.z * num3;
            FP num7 = quat.x * num2;
            FP num8 = quat.x * num3;
            FP num9 = quat.y * num3;
            FP num10 = quat.w * num;
            FP num11 = quat.w * num2;
            FP num12 = quat.w * num3;

            FPVector3 result;
            result.X = (FP.One - (num5 + num6)) * vec.X + (num7 - num12) * vec.Y + (num8 + num11) * vec.Z;
            result.Y = (num7 + num12) * vec.X + (FP.One - (num4 + num6)) * vec.Y + (num9 - num10) * vec.Z;
            result.Z = (num8 - num11) * vec.X + (num9 + num10) * vec.Y + (FP.One - (num4 + num5)) * vec.Z;

            return result;
        }

        public override string ToString()
        {
            return string.Format("({0:f5}, {1:f5}, {2:f5}, {3:f5})", x.AsFloat(), y.AsFloat(), z.AsFloat(), w.AsFloat());
        }

        public static bool operator ==(FPQuaternion value1, FPQuaternion value2)
        {
            return (((value1.x == value2.x) && (value1.y == value2.y)) && (value1.z == value2.z) && (value1.w == value2.w));
        }

        public static bool operator !=(FPQuaternion value1, FPQuaternion value2)
        {
            if ((value1.x == value2.x) && (value1.y == value2.y))
            {
                return (value1.z != value2.z) && (value1.w != value2.w);
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FPQuaternion)) return false;
            FPQuaternion other = (FPQuaternion)obj;

            return (((x == other.x) && (y == other.y)) && (z == other.z) && (w == other.w));
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode() ^ w.GetHashCode();
        }
    }
}
