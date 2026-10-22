#region License

/*
MIT License
Copyright © 2006 The Mono.Xna Team

All rights reserved.

Authors
 * Alan McGovern

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

#endregion License

using System;

namespace GameLogic.Gameplay.Shared
{

    [Serializable]
    public struct FPVector2 : IEquatable<FPVector2>
    {
        #region Private Static Readonly Fields

        public static FPVector2 Zero = new FPVector2(0, 0);
        public static FPVector2 One = new FPVector2(1, 1);
        public static FPVector2 Right = new FPVector2(1, 0);
        public static FPVector2 Left = new FPVector2(-1, 0);
        public static FPVector2 Up = new FPVector2(0, 1);
        public static FPVector2 Down = new FPVector2(0, -1);
        public static FPVector2 MinValue = new FPVector2(FP.MinValue);
        public static FPVector2 MaxValue = new FPVector2(FP.MaxValue);

        #endregion

        #region Public Fields

        public FP X;
        public FP Y;

        #endregion

        #region Private static constructor
        static FPVector2()
        {
            Zero = new FPVector2(0, 0);
            One = new FPVector2(1, 1);

            Right = new FPVector2(1, 0);
            Left = new FPVector2(-1, 0);

            Up = new FPVector2(0, 1);
            Down = new FPVector2(0, -1);
        }
        #endregion

        #region Constructors

        /// <summary>
        /// Constructor foe standard 2D vector.
        /// </summary>
        /// <param name="x">
        /// A <see cref="System.Single"/>
        /// </param>
        /// <param name="y">
        /// A <see cref="System.Single"/>
        /// </param>
        public FPVector2(FP x, FP y)
        {
            this.X = x;
            this.Y = y;
        }

        /// <summary>
        /// Constructor for "square" vector.
        /// </summary>
        /// <param name="value">
        /// A <see cref="System.Single"/>
        /// </param>
        public FPVector2(FP value)
        {
            X = value;
            Y = value;
        }

        public void Set(FP x, FP y)
        {
            this.X = x;
            this.Y = y;
        }

        #endregion Constructors

        public FP this[int index]
        {
            get
            {
                return index switch
                {
                    0 => X,
                    1 => Y,
                    _ => throw new IndexOutOfRangeException("Invalid FPVector2 index!"),
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
                    default:
                        throw new IndexOutOfRangeException("Invalid FPVector2 index!");
                }
            }
        }

        #region Public Methods

        public static void Reflect(ref FPVector2 vector, ref FPVector2 normal, out FPVector2 result)
        {
            FP dot = Dot(vector, normal);
            result.X = vector.X - ((2 * FP.One * dot) * normal.X);
            result.Y = vector.Y - ((2 * FP.One * dot) * normal.Y);
        }

        public static FPVector2 Reflect(FPVector2 vector, FPVector2 normal)
        {
            FPVector2 result;
            Reflect(ref vector, ref normal, out result);
            return result;
        }

        public static FPVector2 Add(FPVector2 value1, FPVector2 value2)
        {
            value1.X += value2.X;
            value1.Y += value2.Y;
            return value1;
        }

        public static void Add(ref FPVector2 value1, ref FPVector2 value2, out FPVector2 result)
        {
            result.X = value1.X + value2.X;
            result.Y = value1.Y + value2.Y;
        }

        public static FPVector2 Barycentric(FPVector2 value1, FPVector2 value2, FPVector2 value3, FP amount1, FP amount2)
        {
            return new FPVector2(
                FPMath.Barycentric(value1.X, value2.X, value3.X, amount1, amount2),
                FPMath.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2));
        }

        public static void Barycentric(ref FPVector2 value1, ref FPVector2 value2, ref FPVector2 value3, FP amount1,
                                       FP amount2, out FPVector2 result)
        {
            result = new FPVector2(
                FPMath.Barycentric(value1.X, value2.X, value3.X, amount1, amount2),
                FPMath.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2));
        }

        public static FPVector2 CatmullRom(FPVector2 value1, FPVector2 value2, FPVector2 value3, FPVector2 value4, FP amount)
        {
            return new FPVector2(
                FPMath.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount),
                FPMath.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount));
        }

        public static void CatmullRom(ref FPVector2 value1, ref FPVector2 value2, ref FPVector2 value3, ref FPVector2 value4,
                                      FP amount, out FPVector2 result)
        {
            result = new FPVector2(
                FPMath.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount),
                FPMath.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount));
        }

        public static FPVector2 Clamp(FPVector2 value1, FPVector2 min, FPVector2 max)
        {
            return new FPVector2(
                FPMath.Clamp(value1.X, min.X, max.X),
                FPMath.Clamp(value1.Y, min.Y, max.Y));
        }

        public static void Clamp(ref FPVector2 value1, ref FPVector2 min, ref FPVector2 max, out FPVector2 result)
        {
            result = new FPVector2(
                FPMath.Clamp(value1.X, min.X, max.X),
                FPMath.Clamp(value1.Y, min.Y, max.Y));
        }

        /// <summary>
        /// Returns FP precison distanve between two vectors
        /// </summary>
        /// <param name="value1">
        /// A <see cref="FPVector2"/>
        /// </param>
        /// <param name="value2">
        /// A <see cref="FPVector2"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Single"/>
        /// </returns>
        public static FP Distance(FPVector2 value1, FPVector2 value2)
        {
            FP result;
            DistanceSquared(ref value1, ref value2, out result);
            return FP.Sqrt(result);
        }


        public static void Distance(ref FPVector2 value1, ref FPVector2 value2, out FP result)
        {
            DistanceSquared(ref value1, ref value2, out result);
            result = FP.Sqrt(result);
        }

        public static FP DistanceSquared(FPVector2 value1, FPVector2 value2)
        {
            FP result;
            DistanceSquared(ref value1, ref value2, out result);
            return result;
        }

        public static void DistanceSquared(ref FPVector2 value1, ref FPVector2 value2, out FP result)
        {
            result = (value1.X - value2.X) * (value1.X - value2.X) + (value1.Y - value2.Y) * (value1.Y - value2.Y);
        }

        /// <summary>
        /// Devide first vector with the secund vector
        /// </summary>
        /// <param name="value1">
        /// A <see cref="FPVector2"/>
        /// </param>
        /// <param name="value2">
        /// A <see cref="FPVector2"/>
        /// </param>
        /// <returns>
        /// A <see cref="FPVector2"/>
        /// </returns>
        public static FPVector2 Divide(FPVector2 value1, FPVector2 value2)
        {
            value1.X /= value2.X;
            value1.Y /= value2.Y;
            return value1;
        }

        public static void Divide(ref FPVector2 value1, ref FPVector2 value2, out FPVector2 result)
        {
            result.X = value1.X / value2.X;
            result.Y = value1.Y / value2.Y;
        }

        public static FPVector2 Divide(FPVector2 value1, FP divider)
        {
            FP factor = 1 / divider;
            value1.X *= factor;
            value1.Y *= factor;
            return value1;
        }

        public static void Divide(ref FPVector2 value1, FP divider, out FPVector2 result)
        {
            FP factor = 1 / divider;
            result.X = value1.X * factor;
            result.Y = value1.Y * factor;
        }

        public static FP Dot(FPVector2 value1, FPVector2 value2)
        {
            return value1.X * value2.X + value1.Y * value2.Y;
        }

        public static void Dot(ref FPVector2 value1, ref FPVector2 value2, out FP result)
        {
            result = value1.X * value2.X + value1.Y * value2.Y;
        }

        public override bool Equals(object obj)
        {
            return (obj is FPVector2) ? this == ((FPVector2)obj) : false;
        }

        public bool Equals(FPVector2 other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return (int)(X + Y);
        }

        public static FPVector2 Hermite(FPVector2 value1, FPVector2 tangent1, FPVector2 value2, FPVector2 tangent2, FP amount)
        {
            FPVector2 result = new FPVector2();
            Hermite(ref value1, ref tangent1, ref value2, ref tangent2, amount, out result);
            return result;
        }

        public static void Hermite(ref FPVector2 value1, ref FPVector2 tangent1, ref FPVector2 value2, ref FPVector2 tangent2,
                                   FP amount, out FPVector2 result)
        {
            result.X = FPMath.Hermite(value1.X, tangent1.X, value2.X, tangent2.X, amount);
            result.Y = FPMath.Hermite(value1.Y, tangent1.Y, value2.Y, tangent2.Y, amount);
        }

        public FP SqrMagnitude
        {
            get
            {
                FP result;
                DistanceSquared(ref this, ref Zero, out result);
                return result;
            }
        }

        public FP Magnitude
        {
            get
            {
                FP result;
                DistanceSquared(ref this, ref Zero, out result);
                return FP.Sqrt(result);
            }
        }

        public static FPVector2 ClampMagnitude(FPVector2 vector, FP maxLength)
        {
            return Normalize(vector) * maxLength;
        }

        public FP LengthSquared()
        {
            FP result;
            DistanceSquared(ref this, ref Zero, out result);
            return result;
        }

        public static FPVector2 Lerp(FPVector2 value1, FPVector2 value2, FP amount)
        {
            amount = FPMath.Clamp(amount, 0, 1);

            return new FPVector2(
                FPMath.Lerp(value1.X, value2.X, amount),
                FPMath.Lerp(value1.Y, value2.Y, amount));
        }

        public static FPVector2 LerpUnclamped(FPVector2 value1, FPVector2 value2, FP amount)
        {
            return new FPVector2(
                FPMath.Lerp(value1.X, value2.X, amount),
                FPMath.Lerp(value1.Y, value2.Y, amount));
        }

        public static void LerpUnclamped(ref FPVector2 value1, ref FPVector2 value2, FP amount, out FPVector2 result)
        {
            result = new FPVector2(
                FPMath.Lerp(value1.X, value2.X, amount),
                FPMath.Lerp(value1.Y, value2.Y, amount));
        }

        public static FPVector2 Max(FPVector2 value1, FPVector2 value2)
        {
            return new FPVector2(
                FPMath.Max(value1.X, value2.X),
                FPMath.Max(value1.Y, value2.Y));
        }

        public static void Max(ref FPVector2 value1, ref FPVector2 value2, out FPVector2 result)
        {
            result.X = FPMath.Max(value1.X, value2.X);
            result.Y = FPMath.Max(value1.Y, value2.Y);
        }

        public static FPVector2 Min(FPVector2 value1, FPVector2 value2)
        {
            return new FPVector2(
                FPMath.Min(value1.X, value2.X),
                FPMath.Min(value1.Y, value2.Y));
        }

        public static void Min(ref FPVector2 value1, ref FPVector2 value2, out FPVector2 result)
        {
            result.X = FPMath.Min(value1.X, value2.X);
            result.Y = FPMath.Min(value1.Y, value2.Y);
        }

        public void Scale(FPVector2 other)
        {
            X = X * other.X;
            Y = Y * other.Y;
        }

        public static FPVector2 Scale(FPVector2 value1, FPVector2 value2)
        {
            FPVector2 result;
            result.X = value1.X * value2.X;
            result.Y = value1.Y * value2.Y;

            return result;
        }

        public static FPVector2 Multiply(FPVector2 value1, FPVector2 value2)
        {
            value1.X *= value2.X;
            value1.Y *= value2.Y;
            return value1;
        }

        public static FPVector2 Multiply(FPVector2 value1, FP scaleFactor)
        {
            value1.X *= scaleFactor;
            value1.Y *= scaleFactor;
            return value1;
        }

        public static void Multiply(ref FPVector2 value1, FP scaleFactor, out FPVector2 result)
        {
            result.X = value1.X * scaleFactor;
            result.Y = value1.Y * scaleFactor;
        }

        public static void Multiply(ref FPVector2 value1, ref FPVector2 value2, out FPVector2 result)
        {
            result.X = value1.X * value2.X;
            result.Y = value1.Y * value2.Y;
        }

        public static FPVector2 Negate(FPVector2 value)
        {
            value.X = -value.X;
            value.Y = -value.Y;
            return value;
        }

        public static void Negate(ref FPVector2 value, out FPVector2 result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
        }

        public void Normalize()
        {
            Normalize(ref this, out this);
        }

        public static FPVector2 Normalize(FPVector2 value)
        {
            Normalize(ref value, out value);
            return value;
        }

        public FPVector2 Normalized
        {
            get
            {
                FPVector2 result;
                Normalize(ref this, out result);

                return result;
            }
        }

        public static void Normalize(ref FPVector2 value, out FPVector2 result)
        {
            FP factor;
            DistanceSquared(ref value, ref Zero, out factor);
            factor = FP.One / FP.Sqrt(factor);
            result.X = value.X * factor;
            result.Y = value.Y * factor;
        }

        public static FPVector2 SmoothStep(FPVector2 value1, FPVector2 value2, FP amount)
        {
            return new FPVector2(
                FPMath.SmoothStep(value1.X, value2.X, amount),
                FPMath.SmoothStep(value1.Y, value2.Y, amount));
        }

        public static void SmoothStep(ref FPVector2 value1, ref FPVector2 value2, FP amount, out FPVector2 result)
        {
            result = new FPVector2(
                FPMath.SmoothStep(value1.X, value2.X, amount),
                FPMath.SmoothStep(value1.Y, value2.Y, amount));
        }

        public static FPVector2 Subtract(FPVector2 value1, FPVector2 value2)
        {
            value1.X -= value2.X;
            value1.Y -= value2.Y;
            return value1;
        }

        public static void Subtract(ref FPVector2 value1, ref FPVector2 value2, out FPVector2 result)
        {
            result.X = value1.X - value2.X;
            result.Y = value1.Y - value2.Y;
        }

        public static FP Angle(FPVector2 a, FPVector2 b)
        {
            return FP.Acos(a.Normalized * b.Normalized) * FP.Rad2Deg;
        }

        public static FPVector2 Rotate(FPVector2 a, FP deg)
        {
            var rad = FPMath.Deg2Rad * deg;
            var cosa = FPMath.Cos(rad);
            var sina = FPMath.Sin(rad);

            return new FPVector2(
                a.X * cosa + a.Y * sina,
                a.Y * cosa - a.X * sina
             );
        }

        public FPVector3 ToFPVector3()
        {
            return new FPVector3(X, Y, 0);
        }


        public override string ToString()
        {
            return string.Format("({0:f5}, {1:f5})", X.AsFloat(), Y.AsFloat());
        }

        #endregion Public Methods

        #region Operators

        public static FPVector2 operator -(FPVector2 value)
        {
            value.X = -value.X;
            value.Y = -value.Y;
            return value;
        }


        public static bool operator ==(FPVector2 value1, FPVector2 value2)
        {
            return value1.X == value2.X && value1.Y == value2.Y;
        }


        public static bool operator !=(FPVector2 value1, FPVector2 value2)
        {
            return value1.X != value2.X || value1.Y != value2.Y;
        }


        public static FPVector2 operator +(FPVector2 value1, FPVector2 value2)
        {
            value1.X += value2.X;
            value1.Y += value2.Y;
            return value1;
        }


        public static FPVector2 operator -(FPVector2 value1, FPVector2 value2)
        {
            value1.X -= value2.X;
            value1.Y -= value2.Y;
            return value1;
        }


        public static FP operator *(FPVector2 value1, FPVector2 value2)
        {
            return Dot(value1, value2);
        }


        public static FPVector2 operator *(FPVector2 value, FP scaleFactor)
        {
            value.X *= scaleFactor;
            value.Y *= scaleFactor;
            return value;
        }


        public static FPVector2 operator *(FP scaleFactor, FPVector2 value)
        {
            value.X *= scaleFactor;
            value.Y *= scaleFactor;
            return value;
        }


        public static FPVector2 operator /(FPVector2 value1, FPVector2 value2)
        {
            value1.X /= value2.X;
            value1.Y /= value2.Y;
            return value1;
        }


        public static FPVector2 operator /(FPVector2 value1, FP divider)
        {
            FP factor = 1 / divider;
            value1.X *= factor;
            value1.Y *= factor;
            return value1;
        }

        #endregion Operators
    }
}