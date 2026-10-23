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
    /// A vector structure.
    /// </summary>
    [Serializable]
    public struct FPVector3
    {

        private static FP ZeroEpsilonSq = FPMath.Epsilon;
        private static FPVector3 InternalZero = new FPVector3(0, 0, 0);
        private static FPVector3 Arbitrary = new FPVector3(1, 1, 1);

        #region Private Static Readonly Fields
        public static readonly FPVector3 Zero = new FPVector3(0, 0, 0);
        public static readonly FPVector3 One = new FPVector3(1, 1, 1);
        public static readonly FPVector3 Left = new FPVector3(-1, 0, 0);
        public static readonly FPVector3 Right = new FPVector3(1, 0, 0);
        public static readonly FPVector3 Up = new FPVector3(0, 1, 0);
        public static readonly FPVector3 Down = new FPVector3(0, -1, 0);
        public static readonly FPVector3 Back = new FPVector3(0, 0, -1);
        public static readonly FPVector3 Forward = new FPVector3(0, 0, 1);
        public static readonly FPVector3 MinValue = new FPVector3(FP.MinValue);
        public static readonly FPVector3 MaxValue = new FPVector3(FP.MaxValue);
        #endregion

        //#region public Static Properties
        //public static FPVector3 Zero => zero;
        //public static FPVector3 One => one;
        //public static FPVector3 Left => left;
        //public static FPVector3 Right => right;
        //public static FPVector3 Up => up;
        //public static FPVector3 Down => down;
        //public static FPVector3 Back => back;
        //public static FPVector3 Forward => forward;
        //public static FPVector3 MinValue => minValue;
        //public static FPVector3 MaxValue => maxValue;
        //#endregion

        #region Public Fields
        /// <summary>The X component of the vector.</summary>
        public FP X;
        /// <summary>The Y component of the vector.</summary>
        public FP Y;
        /// <summary>The Z component of the vector.</summary>
        public FP Z;
        #endregion

        public FP this[int index]
        {
            get
            {
                return index switch
                {
                    0 => X,
                    1 => Y,
                    2 => Z,
                    _ => throw new IndexOutOfRangeException("Invalid Vector2 index!"),
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        X = value;
                        break;
                    case 1:
                        Y = value;
                        break;
                    case 2:
                        Z = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector2 index!");
                }
            }
        }

        public static FPVector3 Abs(FPVector3 other)
        {
            return new FPVector3(FP.FastAbs(other.X), FP.FastAbs(other.Y), FP.FastAbs(other.Z));
        }

        /// <summary>
        /// Gets the squared length of the vector.
        /// </summary>
        /// <returns>Returns the squared length of the vector.</returns>
        public FP SqrMagnitude
        {
            get
            {
                return (((X * X) + (Y * Y)) + (Z * Z));
            }
        }

        /// <summary>
        /// Gets the length of the vector.
        /// </summary>
        /// <returns>Returns the length of the vector.</returns>
        public FP Magnitude
        {
            get
            {
                FP num = ((X * X) + (Y * Y)) + (Z * Z);
                return FP.Sqrt(num);
            }
        }

        public static FPVector3 ClampMagnitude(FPVector3 vector3, FP maxLength)
        {
            return Normalize(vector3) * maxLength;
        }

        /// <summary>
        /// Gets a normalized version of the vector.
        /// </summary>
        /// <returns>Returns a normalized version of the vector.</returns>
        public FPVector3 Normalized
        {
            get
            {
                FPVector3 result = new FPVector3(X, Y, Z);
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
        public FPVector3(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public FPVector3(FP x, FP y, FP z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        /// <summary>
        /// Multiplies each component of the vector by the same components of the provided vector.
        /// </summary>
        public void Scale(FPVector3 other)
        {
            X = X * other.X;
            Y = Y * other.Y;
            Z = Z * other.Z;
        }

        /// <summary>
        /// Sets all vector component to specific values.
        /// </summary>
        /// <param name="x">The X component of the vector.</param>
        /// <param name="y">The Y component of the vector.</param>
        /// <param name="z">The Z component of the vector.</param>
        public void Set(FP x, FP y, FP z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        /// <summary>
        /// Constructor initializing a new instance of the structure
        /// </summary>
        /// <param name="xyz">All components of the vector are set to xyz</param>
        public FPVector3(FP xyz)
        {
            X = xyz;
            Y = xyz;
            Z = xyz;
        }

        public static FPVector3 Lerp(FPVector3 from, FPVector3 to, FP percent)
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
            return string.Format("({0:f2}, {1:f2}, {2:f2})", X.AsFloat(), Y.AsFloat(), Z.AsFloat());
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
            if (!(obj is FPVector3)) return false;
            FPVector3 other = (FPVector3)obj;

            return (((X == other.X) && (Y == other.Y)) && (Z == other.Z));
        }
        #endregion

        /// <summary>
        /// Multiplies each component of the vector by the same components of the provided vector.
        /// </summary>
        public static FPVector3 Scale(FPVector3 vecA, FPVector3 vecB)
        {
            FPVector3 result;
            result.X = vecA.X * vecB.X;
            result.Y = vecA.Y * vecB.Y;
            result.Z = vecA.Z * vecB.Z;

            return result;
        }

        public static FPVector3 operator -(FPVector3 value)
        {
            value.X = -value.X;
            value.Y = -value.Y;
            value.Z = -value.Z;
            return value;
        }

        /// <summary>
        /// Tests if two JVector are equal.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>Returns true if both values are equal, otherwise false.</returns>
        #region public static bool operator ==(JVector value1, JVector value2)
        public static bool operator ==(FPVector3 value1, FPVector3 value2)
        {
            return (((value1.X == value2.X) && (value1.Y == value2.Y)) && (value1.Z == value2.Z));
        }
        #endregion

        /// <summary>
        /// Tests if two JVector are not equal.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>Returns false if both values are equal, otherwise true.</returns>
        #region public static bool operator !=(JVector value1, JVector value2)
        public static bool operator !=(FPVector3 value1, FPVector3 value2)
        {
            if ((value1.X == value2.X) && (value1.Y == value2.Y))
            {
                return (value1.Z != value2.Z);
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
        public static FPVector3 Min(FPVector3 value1, FPVector3 value2)
        {
            FPVector3 result;
            Min(ref value1, ref value2, out result);
            return result;
        }

        /// <summary>
        /// Gets a vector with the minimum x,y and z values of both vectors.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <param name="result">A vector with the minimum x,y and z values of both vectors.</param>
        public static void Min(ref FPVector3 value1, ref FPVector3 value2, out FPVector3 result)
        {
            result.X = (value1.X < value2.X) ? value1.X : value2.X;
            result.Y = (value1.Y < value2.Y) ? value1.Y : value2.Y;
            result.Z = (value1.Z < value2.Z) ? value1.Z : value2.Z;
        }
        #endregion

        /// <summary>
        /// Gets a vector with the maximum x,y and z values of both vectors.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <returns>A vector with the maximum x,y and z values of both vectors.</returns>
        #region public static JVector Max(JVector value1, JVector value2)
        public static FPVector3 Max(FPVector3 value1, FPVector3 value2)
        {
            FPVector3 result;
            Max(ref value1, ref value2, out result);
            return result;
        }

        public static FP Distance(FPVector3 v1, FPVector3 v2)
        {
            return FP.Sqrt((v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y) + (v1.Z - v2.Z) * (v1.Z - v2.Z));
        }

        /// <summary>
        /// Gets a vector with the maximum x,y and z values of both vectors.
        /// </summary>
        /// <param name="value1">The first value.</param>
        /// <param name="value2">The second value.</param>
        /// <param name="result">A vector with the maximum x,y and z values of both vectors.</param>
        public static void Max(ref FPVector3 value1, ref FPVector3 value2, out FPVector3 result)
        {
            result.X = (value1.X > value2.X) ? value1.X : value2.X;
            result.Y = (value1.Y > value2.Y) ? value1.Y : value2.Y;
            result.Z = (value1.Z > value2.Z) ? value1.Z : value2.Z;
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
        }
        #endregion

        /// <summary>
        /// Checks if the length of the vector is zero.
        /// </summary>
        /// <returns>Returns true if the vector is zero, otherwise false.</returns>
        #region public bool IsZero()
        public bool IsZero()
        {
            return (SqrMagnitude == FP.Zero);
        }

        /// <summary>
        /// Checks if the length of the vector is nearly zero.
        /// </summary>
        /// <returns>Returns true if the vector is nearly zero, otherwise false.</returns>
        public bool IsNearlyZero()
        {
            return (SqrMagnitude < ZeroEpsilonSq);
        }
        #endregion

        /// <summary>
        /// Transforms a vector by the given matrix.
        /// </summary>
        /// <param name="position">The vector to transform.</param>
        /// <param name="matrix">The transform matrix.</param>
        /// <returns>The transformed vector.</returns>
        #region public static JVector Transform(JVector position, JMatrix matrix)
        public static FPVector3 Transform(FPVector3 position, FPMatrix matrix)
        {
            FPVector3 result;
            Transform(ref position, ref matrix, out result);
            return result;
        }

        /// <summary>
        /// Transforms a vector by the given matrix.
        /// </summary>
        /// <param name="position">The vector to transform.</param>
        /// <param name="matrix">The transform matrix.</param>
        /// <param name="result">The transformed vector.</param>
        public static void Transform(ref FPVector3 position, ref FPMatrix matrix, out FPVector3 result)
        {
            FP num0 = ((position.X * matrix.M11) + (position.Y * matrix.M21)) + (position.Z * matrix.M31);
            FP num1 = ((position.X * matrix.M12) + (position.Y * matrix.M22)) + (position.Z * matrix.M32);
            FP num2 = ((position.X * matrix.M13) + (position.Y * matrix.M23)) + (position.Z * matrix.M33);

            result.X = num0;
            result.Y = num1;
            result.Z = num2;
        }

        /// <summary>
        /// Transforms a vector by the transposed of the given Matrix.
        /// </summary>
        /// <param name="position">The vector to transform.</param>
        /// <param name="matrix">The transform matrix.</param>
        /// <param name="result">The transformed vector.</param>
        public static void TransposedTransform(ref FPVector3 position, ref FPMatrix matrix, out FPVector3 result)
        {
            FP num0 = ((position.X * matrix.M11) + (position.Y * matrix.M12)) + (position.Z * matrix.M13);
            FP num1 = ((position.X * matrix.M21) + (position.Y * matrix.M22)) + (position.Z * matrix.M23);
            FP num2 = ((position.X * matrix.M31) + (position.Y * matrix.M32)) + (position.Z * matrix.M33);

            result.X = num0;
            result.Y = num1;
            result.Z = num2;
        }
        #endregion

        /// <summary>
        /// Calculates the dot product of two vectors.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="FPVector2">The second vector.</param>
        /// <returns>Returns the dot product of both vectors.</returns>
        #region public static FP Dot(JVector vector1, JVector FPVector2)
        public static FP Dot(FPVector3 vector1, FPVector3 FPVector2)
        {
            return Dot(ref vector1, ref FPVector2);
        }


        /// <summary>
        /// Calculates the dot product of both vectors.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="FPVector2">The second vector.</param>
        /// <returns>Returns the dot product of both vectors.</returns>
        public static FP Dot(ref FPVector3 vector1, ref FPVector3 FPVector2)
        {
            return ((vector1.X * FPVector2.X) + (vector1.Y * FPVector2.Y)) + (vector1.Z * FPVector2.Z);
        }
        #endregion

        // Projects a vector onto another vector.
        public static FPVector3 Project(FPVector3 vector3, FPVector3 onNormal)
        {
            FP sqrtMag = Dot(onNormal, onNormal);
            if (sqrtMag < FPMath.Epsilon)
                return Zero;
            return onNormal * Dot(vector3, onNormal) / sqrtMag;
        }

        // Projects a vector onto a plane defined by a normal orthogonal to the plane.
        public static FPVector3 ProjectOnPlane(FPVector3 vector3, FPVector3 planeNormal)
        {
            return vector3 - Project(vector3, planeNormal);
        }

        // Returns the angle in degrees between /from/ and /to/. This is always the smallest
        public static FP Angle(FPVector3 from, FPVector3 to)
        {
            return FPMath.Acos(FPMath.Clamp(Dot(from.Normalized, to.Normalized), -FP.One, FP.One)) * FPMath.Rad2Deg;
        }

        // The smaller of the two possible angles between the two vectors is returned, therefore the result will never be greater than 180 degrees or smaller than -180 degrees.
        // If you imagine the from and to vectors as lines on a piece of paper, both originating from the same point, then the /axis/ vector would point up out of the paper.
        // The measured angle between the two vectors would be positive in a clockwise direction and negative in an anti-clockwise direction.
        public static FP SignedAngle(FPVector3 from, FPVector3 to, FPVector3 axis)
        {
            FPVector3 fromNorm = from.Normalized, toNorm = to.Normalized;
            FP unsignedAngle = FPMath.Acos(FPMath.Clamp(Dot(fromNorm, toNorm), -FP.One, FP.One)) * FPMath.Rad2Deg;
            FP sign = FPMath.Sign(Dot(axis, Cross(fromNorm, toNorm)));
            return unsignedAngle * sign;
        }

        /// <summary>
        /// Adds two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The sum of both vectors.</returns>
        #region public static void Add(JVector value1, JVector value2)
        public static FPVector3 Add(FPVector3 value1, FPVector3 value2)
        {
            FPVector3 result;
            Add(ref value1, ref value2, out result);
            return result;
        }

        /// <summary>
        /// Adds to vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="result">The sum of both vectors.</param>
        public static void Add(ref FPVector3 value1, ref FPVector3 value2, out FPVector3 result)
        {
            FP num0 = value1.X + value2.X;
            FP num1 = value1.Y + value2.Y;
            FP num2 = value1.Z + value2.Z;

            result.X = num0;
            result.Y = num1;
            result.Z = num2;
        }
        #endregion

        /// <summary>
        /// Divides a vector by a factor.
        /// </summary>
        /// <param name="value1">The vector to divide.</param>
        /// <param name="scaleFactor">The scale factor.</param>
        /// <returns>Returns the scaled vector.</returns>
        public static FPVector3 Divide(FPVector3 value1, FP scaleFactor)
        {
            FPVector3 result;
            Divide(ref value1, scaleFactor, out result);
            return result;
        }

        /// <summary>
        /// Divides a vector by a factor.
        /// </summary>
        /// <param name="value1">The vector to divide.</param>
        /// <param name="scaleFactor">The scale factor.</param>
        /// <param name="result">Returns the scaled vector.</param>
        public static void Divide(ref FPVector3 value1, FP scaleFactor, out FPVector3 result)
        {
            result.X = value1.X / scaleFactor;
            result.Y = value1.Y / scaleFactor;
            result.Z = value1.Z / scaleFactor;
        }

        /// <summary>
        /// Subtracts two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The difference of both vectors.</returns>
        #region public static JVector Subtract(JVector value1, JVector value2)
        public static FPVector3 Subtract(FPVector3 value1, FPVector3 value2)
        {
            FPVector3 result;
            Subtract(ref value1, ref value2, out result);
            return result;
        }

        /// <summary>
        /// Subtracts to vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="result">The difference of both vectors.</param>
        public static void Subtract(ref FPVector3 value1, ref FPVector3 value2, out FPVector3 result)
        {
            FP num0 = value1.X - value2.X;
            FP num1 = value1.Y - value2.Y;
            FP num2 = value1.Z - value2.Z;

            result.X = num0;
            result.Y = num1;
            result.Z = num2;
        }
        #endregion

        /// <summary>
        /// The cross product of two vectors.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="FPVector2">The second vector.</param>
        /// <returns>The cross product of both vectors.</returns>
        #region public static JVector Cross(JVector vector1, JVector FPVector2)
        public static FPVector3 Cross(FPVector3 vector1, FPVector3 FPVector2)
        {
            FPVector3 result;
            Cross(ref vector1, ref FPVector2, out result);
            return result;
        }

        /// <summary>
        /// The cross product of two vectors.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="FPVector2">The second vector.</param>
        /// <param name="result">The cross product of both vectors.</param>
        public static void Cross(ref FPVector3 vector1, ref FPVector3 FPVector2, out FPVector3 result)
        {
            FP num3 = (vector1.Y * FPVector2.Z) - (vector1.Z * FPVector2.Y);
            FP num2 = (vector1.Z * FPVector2.X) - (vector1.X * FPVector2.Z);
            FP num = (vector1.X * FPVector2.Y) - (vector1.Y * FPVector2.X);
            result.X = num3;
            result.Y = num2;
            result.Z = num;
        }
        #endregion

        /// <summary>
        /// Gets the hashcode of the vector.
        /// </summary>
        /// <returns>Returns the hashcode of the vector.</returns>
        #region public override int GetHashCode()
        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
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
        }

        /// <summary>
        /// Inverses the direction of a vector.
        /// </summary>
        /// <param name="value">The vector to inverse.</param>
        /// <returns>The negated vector.</returns>
        public static FPVector3 Negate(FPVector3 value)
        {
            FPVector3 result;
            Negate(ref value, out result);
            return result;
        }

        /// <summary>
        /// Inverses the direction of a vector.
        /// </summary>
        /// <param name="value">The vector to inverse.</param>
        /// <param name="result">The negated vector.</param>
        public static void Negate(ref FPVector3 value, out FPVector3 result)
        {
            FP num0 = -value.X;
            FP num1 = -value.Y;
            FP num2 = -value.Z;

            result.X = num0;
            result.Y = num1;
            result.Z = num2;
        }
        #endregion

        /// <summary>
        /// Normalizes the given vector.
        /// </summary>
        /// <param name="value">The vector which should be normalized.</param>
        /// <returns>A normalized vector.</returns>
        #region public static JVector Normalize(JVector value)
        public static FPVector3 Normalize(FPVector3 value)
        {
            FPVector3 result;
            Normalize(ref value, out result);
            return result;
        }

        /// <summary>
        /// Normalizes this vector.
        /// </summary>
        public void Normalize()
        {
            FP num2 = ((X * X) + (Y * Y)) + (Z * Z);
            FP num = FP.One / FP.Sqrt(num2);
            X *= num;
            Y *= num;
            Z *= num;
        }

        /// <summary>
        /// Normalizes the given vector.
        /// </summary>
        /// <param name="value">The vector which should be normalized.</param>
        /// <param name="result">A normalized vector.</param>
        public static void Normalize(ref FPVector3 value, out FPVector3 result)
        {
            FP num2 = ((value.X * value.X) + (value.Y * value.Y)) + (value.Z * value.Z);
            FP num = FP.One / FP.Sqrt(num2);
            result.X = value.X * num;
            result.Y = value.Y * num;
            result.Z = value.Z * num;
        }
        #endregion

        #region public static void Swap(ref JVector vector1, ref JVector FPVector2)
        /// <summary>
        /// Swaps the components of both vectors.
        /// </summary>
        /// <param name="vector1">The first vector to swap with the second.</param>
        /// <param name="FPVector2">The second vector to swap with the first.</param>
        public static void Swap(ref FPVector3 vector1, ref FPVector3 FPVector2)
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
        }
        #endregion

        /// <summary>
        /// Multiply a vector with a factor.
        /// </summary>
        /// <param name="value1">The vector to multiply.</param>
        /// <param name="scaleFactor">The scale factor.</param>
        /// <returns>Returns the multiplied vector.</returns>
        #region public static JVector Multiply(JVector value1, FP scaleFactor)
        public static FPVector3 Multiply(FPVector3 value1, FP scaleFactor)
        {
            FPVector3 result;
            Multiply(ref value1, scaleFactor, out result);
            return result;
        }

        /// <summary>
        /// Multiply a vector with a factor.
        /// </summary>
        /// <param name="value1">The vector to multiply.</param>
        /// <param name="scaleFactor">The scale factor.</param>
        /// <param name="result">Returns the multiplied vector.</param>
        public static void Multiply(ref FPVector3 value1, FP scaleFactor, out FPVector3 result)
        {
            result.X = value1.X * scaleFactor;
            result.Y = value1.Y * scaleFactor;
            result.Z = value1.Z * scaleFactor;
        }
        #endregion

        /// <summary>
        /// Calculates the cross product of two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>Returns the cross product of both.</returns>
        #region public static JVector operator %(JVector value1, JVector value2)
        public static FPVector3 operator %(FPVector3 value1, FPVector3 value2)
        {
            FPVector3 result;
            Cross(ref value1, ref value2, out result);
            return result;
        }
        #endregion

        /// <summary>
        /// Calculates the dot product of two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>Returns the dot product of both.</returns>
        #region public static FP operator *(JVector value1, JVector value2)
        public static FP operator *(FPVector3 value1, FPVector3 value2)
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
        public static FPVector3 operator *(FPVector3 value1, FP value2)
        {
            FPVector3 result;
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
        public static FPVector3 operator *(FP value1, FPVector3 value2)
        {
            FPVector3 result;
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
        public static FPVector3 operator -(FPVector3 value1, FPVector3 value2)
        {
            FPVector3 result;
            Subtract(ref value1, ref value2, out result);
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
        public static FPVector3 operator +(FPVector3 value1, FPVector3 value2)
        {
            FPVector3 result;
            Add(ref value1, ref value2, out result);
            return result;
        }
        #endregion

        /// <summary>
        /// Divides a vector by a factor.
        /// </summary>
        /// <param name="value1">The vector to divide.</param>
        /// <param name="scaleFactor">The scale factor.</param>
        /// <returns>Returns the scaled vector.</returns>
        public static FPVector3 operator /(FPVector3 value1, FP value2)
        {
            FPVector3 result;
            Divide(ref value1, value2, out result);
            return result;
        }

        public static FPVector3 operator ^(FPVector3 value1, FPVector3 value2)
        {
            FPVector3 v = new FPVector3();
            v.X = value1.Y * value2.Z - value1.Z * value2.Y;
            v.Y = value1.Z * value2.X - value1.X * value2.Z;
            v.Z = value1.X * value2.Y - value1.Y * value2.X;
            return v;
        }

        public FPVector2 ToFPFPVector2()
        {
            return new FPVector2(X, Y);
        }

        public FPVector4 ToFPVector4()
        {
            return new FPVector4(X, Y, Z, FP.One);
        }
    }
}
