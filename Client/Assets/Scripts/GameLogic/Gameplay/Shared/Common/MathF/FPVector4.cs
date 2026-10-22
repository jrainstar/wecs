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

namespace GameLogic.Gameplay.Shared
{
    /// <summary>
    /// A vector structure.
    /// </summary>
    [Serializable]
    public struct FPVector4
    {

        private static FP ZeroEpsilonSq = FPMath.Epsilon;
        internal static FPVector4 InternalZero;

        /// <summary>The X component of the vector.</summary>
        public FP X;
        /// <summary>The Y component of the vector.</summary>
        public FP Y;
        /// <summary>The Z component of the vector.</summary>
        public FP Z;
        /// <summary>The W component of the vector.</summary>
        public FP w;

        #region Static readonly variables
        /// <summary>
        /// A vector with components (0,0,0,0);
        /// </summary>
        public static readonly FPVector4 zero;
        /// <summary>
        /// A vector with components (1,1,1,1);
        /// </summary>
        public static readonly FPVector4 one;
        /// <summary>
        /// A vector with components 
        /// (FP.MinValue,FP.MinValue,FP.MinValue);
        /// </summary>
        public static readonly FPVector4 MinValue;
        /// <summary>
        /// A vector with components 
        /// (FP.MaxValue,FP.MaxValue,FP.MaxValue);
        /// </summary>
        public static readonly FPVector4 MaxValue;
        #endregion

        #region Private static constructor
        static FPVector4()
        {
            one = new FPVector4(1, 1, 1, 1);
            zero = new FPVector4(0, 0, 0, 0);
            MinValue = new FPVector4(FP.MinValue);
            MaxValue = new FPVector4(FP.MaxValue);
            InternalZero = zero;
        }
        #endregion

        public static FPVector4 Abs(FPVector4 other)
        {
            return new FPVector4(FP.Abs(other.X), FP.Abs(other.Y), FP.Abs(other.Z), FP.Abs(other.Z));
        }

        /// <summary>
        /// Gets the squared length of the vector.
        /// </summary>
        /// <returns>Returns the squared length of the vector.</returns>
        public FP sqrMagnitude
        {
            get
            {
                return (((X * X) + (Y * Y)) + (Z * Z) + (w * w));
            }
        }

        /// <summary>
        /// Gets the length of the vector.
        /// </summary>
        /// <returns>Returns the length of the vector.</returns>
        public FP magnitude
        {
            get
            {
                FP num = sqrMagnitude;
                return FP.Sqrt(num);
            }
        }

        public static FPVector4 ClampMagnitude(FPVector4 vector, FP maxLength)
        {
            return Normalize(vector) * maxLength;
        }

        /// <summary>
        /// Gets a normalized version of the vector.
        /// </summary>
        /// <returns>Returns a normalized version of the vector.</returns>
        public FPVector4 normalized
        {
            get
            {
                FPVector4 result = new FPVector4(X, Y, Z, w);
                result.Normalize();

                return result;
            }
        }

        /// <summary>
        /// Constructor initializing a new instance of the structure
        /// </summary>
        /// <param name="x">The X component of the vector.</param>
        /// <param name="y">The Y component of the vector.</param>
        /// <param name="z">The Z component of the vector.</param>
        /// <param name="w">The W component of the vector.</param>
        public FPVector4(int x, int y, int z, int w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.w = w;
        }

        public FPVector4(FP x, FP y, FP z, FP w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.w = w;
        }

        /// <summary>
        /// Multiplies each component of the vector by the same components of the provided vector.
        /// </summary>
        public void Scale(FPVector4 other)
        {
            X = X * other.X;
            Y = Y * other.Y;
            Z = Z * other.Z;
            w = w * other.w;
        }

        /// <summary>
        /// Sets all vector component to specific values.
        /// </summary>
        /// <param name="x">The X component of the vector.</param>
        /// <param name="y">The Y component of the vector.</param>
        /// <param name="z">The Z component of the vector.</param>
        /// <param name="w">The W component of the vector.</param>
        public void Set(FP x, FP y, FP z, FP w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.w = w;
        }

        /// <summary>
        /// Constructor initializing a new instance of the structure
        /// </summary>
        /// <param name="xyz">All components of the vector are set to xyz</param>
        public FPVector4(FP xyzw)
        {
            X = xyzw;
            Y = xyzw;
            Z = xyzw;
            w = xyzw;
        }

        public static FPVector4 Lerp(FPVector4 from, FPVector4 to, FP percent)
        {
            return from + (to - from) * percent;
        }

        /// <summary>
        /// Builds a string from the JVector.
        /// </summary>
        /// <returns>A string containing all three components.</returns>
        #region public override string ToString()
        public override string ToString()
        {
            return string.Format("({0:f5}, {1:f5}, {2:f5}, {3:f5})", X.AsFloat(), Y.AsFloat(), Z.AsFloat(), w.AsFloat());
        }
        #endregion

        /// <summary>
        /// Tests if an object is equal to this vector.
        /// </summary>
        /// <param name="obj">The object to test.</param>
        /// <returns>Returns true if they are euqal, otherwise false.</returns>
        #region public override bool Equals(object obj)
        public override bool Equals(object obj)
        {
            if (!(obj is FPVector4)) return false;
            FPVector4 other = (FPVector4)obj;

            return (((X == other.X) && (Y == other.Y)) && (Z == other.Z) && (w == other.w));
        }
        #endregion

        /// <summary>
        /// Multiplies each component of the vector by the same components of the provided vector.
        /// </summary>
        public static FPVector4 Scale(FPVector4 vecA, FPVector4 vecB)
        {
            FPVector4 result;
            result.X = vecA.X * vecB.X;
            result.Y = vecA.Y * vecB.Y;
            result.Z = vecA.Z * vecB.Z;
            result.w = vecA.w * vecB.w;

            return result;
        }

        public static FPVector4 operator -(FPVector4 value)
        {
            value.X = -value.X;
            value.Y = -value.Y;
            value.Z = -value.Z;
            value.w = -value.w;

            return value;
        }

        /// <summary>
        /// Tests if two JVector are equal.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>Returns true if both values are equal, otherwise false.</returns>
        #region public static bool operator ==(JVector value1, JVector value2)
        public static bool operator ==(FPVector4 value1, FPVector4 value2)
        {
            return (((value1.X == value2.X) && (value1.Y == value2.Y)) && (value1.Z == value2.Z) && (value1.w == value2.w));
        }
        #endregion

        /// <summary>
        /// Tests if two JVector are not equal.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>Returns false if both values are equal, otherwise true.</returns>
        #region public static bool operator !=(JVector value1, JVector value2)
        public static bool operator !=(FPVector4 value1, FPVector4 value2)
        {
            if ((value1.X == value2.X) && (value1.Y == value2.Y) && (value1.Z == value2.Z))
            {
                return (value1.w != value2.w);
            }
            return true;
        }
        #endregion

        /// <summary>
        /// Gets a vector with the minimum x,y and z values of both vectors.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>A vector with the minimum x,y and z values of both vectors.</returns>
        #region public static JVector Min(JVector value1, JVector value2)

        public static FPVector4 Min(FPVector4 value1, FPVector4 value2)
        {
            FPVector4 result;
            Min(ref value1, ref value2, out result);
            return result;
        }

        /// <summary>
        /// Gets a vector with the minimum x,y and z values of both vectors.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <param name="result">A vector with the minimum x,y and z values of both vectors.</param>
        public static void Min(ref FPVector4 value1, ref FPVector4 value2, out FPVector4 result)
        {
            result.X = (value1.X < value2.X) ? value1.X : value2.X;
            result.Y = (value1.Y < value2.Y) ? value1.Y : value2.Y;
            result.Z = (value1.Z < value2.Z) ? value1.Z : value2.Z;
            result.w = (value1.w < value2.w) ? value1.w : value2.w;
        }
        #endregion

        /// <summary>
        /// Gets a vector with the maximum x,y and z values of both vectors.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>A vector with the maximum x,y and z values of both vectors.</returns>
        #region public static JVector Max(JVector value1, JVector value2)
        public static FPVector4 Max(FPVector4 value1, FPVector4 value2)
        {
            FPVector4 result;
            Max(ref value1, ref value2, out result);
            return result;
        }

        public static FP Distance(FPVector4 v1, FPVector4 v2)
        {
            return FP.Sqrt((v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y) + (v1.Z - v2.Z) * (v1.Z - v2.Z) + (v1.w - v2.w) * (v1.w - v2.w));
        }

        /// <summary>
        /// Gets a vector with the maximum x,y and z values of both vectors.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <param name="result">A vector with the maximum x,y and z values of both vectors.</param>
        public static void Max(ref FPVector4 value1, ref FPVector4 value2, out FPVector4 result)
        {
            result.X = (value1.X > value2.X) ? value1.X : value2.X;
            result.Y = (value1.Y > value2.Y) ? value1.Y : value2.Y;
            result.Z = (value1.Z > value2.Z) ? value1.Z : value2.Z;
            result.w = (value1.w > value2.w) ? value1.w : value2.w;
        }
        #endregion

        /// <summary>
        /// Sets the length of the vector to zero.
        /// </summary>
        #region public void MakeZero()
        public void MakeZero()
        {
            X = FP.Zero;
            Y = FP.Zero;
            Z = FP.Zero;
            w = FP.Zero;
        }
        #endregion

        /// <summary>
        /// Checks if the length of the vector is zero.
        /// </summary>
        /// <returns>Returns true if the vector is zero, otherwise false.</returns>
        #region public bool IsZero()
        public bool IsZero()
        {
            return (sqrMagnitude == FP.Zero);
        }

        /// <summary>
        /// Checks if the length of the vector is nearly zero.
        /// </summary>
        /// <returns>Returns true if the vector is nearly zero, otherwise false.</returns>
        public bool IsNearlyZero()
        {
            return (sqrMagnitude < ZeroEpsilonSq);
        }
        #endregion

        /// <summary>
        /// Transforms a vector by the given matrix.
        /// </summary>
        /// <param name="position">The vector to transform.</param>
        /// <param name="matrix">The transform matrix.</param>
        /// <returns>The transformed vector.</returns>
        #region public static JVector Transform(JVector position, JMatrix matrix)
        public static FPVector4 Transform(FPVector4 position, FPMatrix4x4 matrix)
        {
            FPVector4 result;
            Transform(ref position, ref matrix, out result);
            return result;
        }

        public static FPVector4 Transform(FPVector3 position, FPMatrix4x4 matrix)
        {
            FPVector4 result;
            Transform(ref position, ref matrix, out result);
            return result;
        }

        /// <summary>
        /// Transforms a vector by the given matrix.
        /// </summary>
        /// <param name="vector3">The vector to transform.</param>
        /// <param name="matrix">The transform matrix.</param>
        /// <param name="result">The transformed vector.</param>
        public static void Transform(ref FPVector3 vector3, ref FPMatrix4x4 matrix, out FPVector4 result)
        {
            result.X = vector3.X * matrix.M11 + vector3.Y * matrix.M12 + vector3.Z * matrix.M13 + matrix.M14;
            result.Y = vector3.X * matrix.M21 + vector3.Y * matrix.M22 + vector3.Z * matrix.M23 + matrix.M24;
            result.Z = vector3.X * matrix.M31 + vector3.Y * matrix.M32 + vector3.Z * matrix.M33 + matrix.M34;
            result.w = vector3.X * matrix.M41 + vector3.Y * matrix.M42 + vector3.Z * matrix.M43 + matrix.M44;
        }

        public static void Transform(ref FPVector4 vector, ref FPMatrix4x4 matrix, out FPVector4 result)
        {
            result.X = vector.X * matrix.M11 + vector.Y * matrix.M12 + vector.Z * matrix.M13 + vector.w * matrix.M14;
            result.Y = vector.X * matrix.M21 + vector.Y * matrix.M22 + vector.Z * matrix.M23 + vector.w * matrix.M24;
            result.Z = vector.X * matrix.M31 + vector.Y * matrix.M32 + vector.Z * matrix.M33 + vector.w * matrix.M34;
            result.w = vector.X * matrix.M41 + vector.Y * matrix.M42 + vector.Z * matrix.M43 + vector.w * matrix.M44;
        }
        #endregion

        /// <summary>
        /// Calculates the dot product of two vectors.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="FPVector2">The second vector.</param>
        /// <returns>Returns the dot product of both vectors.</returns>
        #region public static FP Dot(JVector vector1, JVector FPVector2)
        public static FP Dot(FPVector4 vector1, FPVector4 FPVector2)
        {
            return Dot(ref vector1, ref FPVector2);
        }


        /// <summary>
        /// Calculates the dot product of both vectors.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="FPVector2">The second vector.</param>
        /// <returns>Returns the dot product of both vectors.</returns>
        public static FP Dot(ref FPVector4 vector1, ref FPVector4 FPVector2)
        {
            return ((vector1.X * FPVector2.X) + (vector1.Y * FPVector2.Y)) + (vector1.Z * FPVector2.Z) + (vector1.w * FPVector2.w);
        }
        #endregion

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The sum of both vectors.</returns>
        #region public static void Add(JVector value1, JVector value2)
        public static FPVector4 Add(FPVector4 value1, FPVector4 value2)
        {
            FPVector4 result;
            Add(ref value1, ref value2, out result);
            return result;
        }

        /// <summary>
        /// Adds to vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="result">The sum of both vectors.</param>
        public static void Add(ref FPVector4 value1, ref FPVector4 value2, out FPVector4 result)
        {
            result.X = value1.X + value2.X;
            result.Y = value1.Y + value2.Y;
            result.Z = value1.Z + value2.Z;
            result.w = value1.w + value2.w;
        }
        #endregion

        /// <summary>
        /// Divides a vector by a factor.
        /// </summary>
        /// <param name="value1">The vector to divide.</param>
        /// <param name="scaleFactor">The scale factor.</param>
        /// <returns>Returns the scaled vector.</returns>
        public static FPVector4 Divide(FPVector4 value1, FP scaleFactor)
        {
            FPVector4 result;
            Divide(ref value1, scaleFactor, out result);
            return result;
        }

        /// <summary>
        /// Divides a vector by a factor.
        /// </summary>
        /// <param name="value1">The vector to divide.</param>
        /// <param name="scaleFactor">The scale factor.</param>
        /// <param name="result">Returns the scaled vector.</param>
        public static void Divide(ref FPVector4 value1, FP scaleFactor, out FPVector4 result)
        {
            result.X = value1.X / scaleFactor;
            result.Y = value1.Y / scaleFactor;
            result.Z = value1.Z / scaleFactor;
            result.w = value1.w / scaleFactor;
        }

        /// <summary>
        /// Subtracts two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The difference of both vectors.</returns>
        #region public static JVector Subtract(JVector value1, JVector value2)
        public static FPVector4 Subtract(FPVector4 value1, FPVector4 value2)
        {
            FPVector4 result;
            Subtract(ref value1, ref value2, out result);
            return result;
        }

        /// <summary>
        /// Subtracts to vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="result">The difference of both vectors.</param>
        public static void Subtract(ref FPVector4 value1, ref FPVector4 value2, out FPVector4 result)
        {
            result.X = value1.X - value2.X;
            result.Y = value1.Y - value2.Y;
            result.Z = value1.Z - value2.Z;
            result.w = value1.w - value2.w;
        }
        #endregion

        /// <summary>
        /// Gets the hashcode of the vector.
        /// </summary>
        /// <returns>Returns the hashcode of the vector.</returns>
        #region public override int GetHashCode()
        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ w.GetHashCode();
        }
        #endregion

        /// <summary>
        /// Inverses the direction of the vector.
        /// </summary>
        #region public static JVector Negate(JVector value)
        public void Negate()
        {
            X = -X;
            Y = -Y;
            Z = -Z;
            w = -w;
        }

        /// <summary>
        /// Inverses the direction of a vector.
        /// </summary>
        /// <param name="value">The vector to inverse.</param>
        /// <returns>The negated vector.</returns>
        public static FPVector4 Negate(FPVector4 value)
        {
            FPVector4 result;
            Negate(ref value, out result);
            return result;
        }

        /// <summary>
        /// Inverses the direction of a vector.
        /// </summary>
        /// <param name="value">The vector to inverse.</param>
        /// <param name="result">The negated vector.</param>
        public static void Negate(ref FPVector4 value, out FPVector4 result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
            result.Z = -value.Z;
            result.w = -value.w;
        }
        #endregion

        /// <summary>
        /// Normalizes the given vector.
        /// </summary>
        /// <param name="value">The vector which should be normalized.</param>
        /// <returns>A normalized vector.</returns>
        #region public static JVector Normalize(JVector value)
        public static FPVector4 Normalize(FPVector4 value)
        {
            FPVector4 result;
            Normalize(ref value, out result);
            return result;
        }

        /// <summary>
        /// Normalizes this vector.
        /// </summary>
        public void Normalize()
        {
            FP num2 = ((X * X) + (Y * Y)) + (Z * Z) + (w * w);
            FP num = FP.One / FP.Sqrt(num2);
            X *= num;
            Y *= num;
            Z *= num;
            w *= num;
        }

        /// <summary>
        /// Normalizes the given vector.
        /// </summary>
        /// <param name="value">The vector which should be normalized.</param>
        /// <param name="result">A normalized vector.</param>
        public static void Normalize(ref FPVector4 value, out FPVector4 result)
        {
            FP num2 = ((value.X * value.X) + (value.Y * value.Y)) + (value.Z * value.Z) + (value.w * value.w);
            FP num = FP.One / FP.Sqrt(num2);
            result.X = value.X * num;
            result.Y = value.Y * num;
            result.Z = value.Z * num;
            result.w = value.w * num;
        }
        #endregion

        #region public static void Swap(ref JVector vector1, ref JVector FPVector2)

        /// <summary>
        /// Swaps the components of both vectors.
        /// </summary>
        /// <param name="vector1">The first vector to swap with the second.</param>
        /// <param name="FPVector2">The second vector to swap with the first.</param>
        public static void Swap(ref FPVector4 vector1, ref FPVector4 FPVector2)
        {
            FP temp;

            temp = vector1.X;
            vector1.X = FPVector2.X;
            FPVector2.X = temp;

            temp = vector1.Y;
            vector1.Y = FPVector2.Y;
            FPVector2.Y = temp;

            temp = vector1.Z;
            vector1.Z = FPVector2.Z;
            FPVector2.Z = temp;

            temp = vector1.w;
            vector1.w = FPVector2.w;
            FPVector2.w = temp;
        }
        #endregion

        /// <summary>
        /// Multiply a vector with a factor.
        /// </summary>
        /// <param name="value1">The vector to multiply.</param>
        /// <param name="scaleFactor">The scale factor.</param>
        /// <returns>Returns the multiplied vector.</returns>
        #region public static JVector Multiply(JVector value1, FP scaleFactor)
        public static FPVector4 Multiply(FPVector4 value1, FP scaleFactor)
        {
            FPVector4 result;
            Multiply(ref value1, scaleFactor, out result);
            return result;
        }

        /// <summary>
        /// Multiply a vector with a factor.
        /// </summary>
        /// <param name="value1">The vector to multiply.</param>
        /// <param name="scaleFactor">The scale factor.</param>
        /// <param name="result">Returns the multiplied vector.</param>
        public static void Multiply(ref FPVector4 value1, FP scaleFactor, out FPVector4 result)
        {
            result.X = value1.X * scaleFactor;
            result.Y = value1.Y * scaleFactor;
            result.Z = value1.Z * scaleFactor;
            result.w = value1.w * scaleFactor;
        }
        #endregion

        /// <summary>
        /// Calculates the dot product of two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>Returns the dot product of both.</returns>
        #region public static FP operator *(JVector value1, JVector value2)
        public static FP operator *(FPVector4 value1, FPVector4 value2)
        {
            return Dot(ref value1, ref value2);
        }
        #endregion

        /// <summary>
        /// Multiplies a vector by a scale factor.
        /// </summary>
        /// <param name="value1">The vector to scale.</param>
        /// <param name="value2">The scale factor.</param>
        /// <returns>Returns the scaled vector.</returns>
        #region public static JVector operator *(JVector value1, FP value2)
        public static FPVector4 operator *(FPVector4 value1, FP value2)
        {
            FPVector4 result;
            Multiply(ref value1, value2, out result);
            return result;
        }
        #endregion

        /// <summary>
        /// Multiplies a vector by a scale factor.
        /// </summary>
        /// <param name="value2">The vector to scale.</param>
        /// <param name="value1">The scale factor.</param>
        /// <returns>Returns the scaled vector.</returns>
        #region public static JVector operator *(FP value1, JVector value2)
        public static FPVector4 operator *(FP value1, FPVector4 value2)
        {
            FPVector4 result;
            Multiply(ref value2, value1, out result);
            return result;
        }
        #endregion

        /// <summary>
        /// Subtracts two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The difference of both vectors.</returns>
        #region public static JVector operator -(JVector value1, JVector value2)
        public static FPVector4 operator -(FPVector4 value1, FPVector4 value2)
        {
            FPVector4 result; Subtract(ref value1, ref value2, out result);
            return result;
        }
        #endregion

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The sum of both vectors.</returns>
        #region public static JVector operator +(JVector value1, JVector value2)
        public static FPVector4 operator +(FPVector4 value1, FPVector4 value2)
        {
            FPVector4 result; Add(ref value1, ref value2, out result);
            return result;
        }
        #endregion

        /// <summary>
        /// Divides a vector by a factor.
        /// </summary>
        /// <param name="value1">The vector to divide.</param>
        /// <param name="scaleFactor">The scale factor.</param>
        /// <returns>Returns the scaled vector.</returns>
        public static FPVector4 operator /(FPVector4 value1, FP value2)
        {
            FPVector4 result;
            Divide(ref value1, value2, out result);
            return result;
        }

        public static implicit operator FPVector4(FPVector3 value)
        {
            return new FPVector4(value.X, value.X, value.X, 0);
        }

        public static implicit operator FPVector3(FPVector4 value)
        {
            return new FPVector3(value.X, value.Y, value.Z);
        }

        public FPVector2 ToFPFPVector2()
        {
            return new FPVector2(X, Y);
        }

        public FPVector3 ToFPVector()
        {
            return new FPVector3(X, Y, Z);
        }
    }

}