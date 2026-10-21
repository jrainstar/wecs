using System.Collections.Generic;
using System;
using UnityEngine;

namespace GameCore
{
    public class Ease3
    {
        public static Vector3 Linear(Vector3 from, Vector3 to, float time)
        {
            return Vector3.Lerp(from, to, time);
        }

        public static Vector3 SineIn(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.SineIn(from.x, to.x, time), Ease.SineIn(from.y, to.y, time), Ease.SineIn(from.z, to.z, time));
        }

        public static Vector3 SineOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.SineOut(from.x, to.x, time), Ease.SineOut(from.y, to.y, time), Ease.SineOut(from.z, to.z, time));
        }

        public static Vector3 SineInOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.SineInOut(from.x, to.x, time), Ease.SineInOut(from.y, to.y, time), Ease.SineInOut(from.z, to.z, time));
        }

        public static Vector3 QuadIn(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.QuadIn(from.x, to.x, time), Ease.QuadIn(from.y, to.y, time), Ease.QuadIn(from.z, to.z, time));
        }

        public static Vector3 QuadOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.QuadOut(from.x, to.x, time), Ease.QuadOut(from.y, to.y, time), Ease.QuadOut(from.z, to.z, time));
        }

        public static Vector3 QuadInOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.QuadInOut(from.x, to.x, time), Ease.QuadInOut(from.y, to.y, time), Ease.QuadInOut(from.z, to.z, time));
        }

        public static Vector3 CubicIn(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.CubicIn(from.x, to.x, time), Ease.CubicIn(from.y, to.y, time), Ease.CubicIn(from.z, to.z, time));
        }

        public static Vector3 CubicOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.CubicOut(from.x, to.x, time), Ease.CubicOut(from.y, to.y, time), Ease.CubicOut(from.z, to.z, time));
        }

        public static Vector3 CubicInOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.CubicInOut(from.x, to.x, time), Ease.CubicInOut(from.y, to.y, time), Ease.CubicInOut(from.z, to.z, time));
        }

        public static Vector3 QuartIn(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.QuartIn(from.x, to.x, time), Ease.QuartIn(from.y, to.y, time), Ease.QuartIn(from.z, to.z, time));
        }

        public static Vector3 QuartOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.QuartOut(from.x, to.x, time), Ease.QuartOut(from.y, to.y, time), Ease.QuartOut(from.z, to.z, time));
        }

        public static Vector3 QuartInOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.QuartInOut(from.x, to.x, time), Ease.QuartInOut(from.y, to.y, time), Ease.QuartInOut(from.z, to.z, time));
        }

        public static Vector3 QuintIn(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.QuintIn(from.x, to.x, time), Ease.QuintIn(from.y, to.y, time), Ease.QuintIn(from.z, to.z, time));
        }

        public static Vector3 QuintOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.QuintOut(from.x, to.x, time), Ease.QuintOut(from.y, to.y, time), Ease.QuintOut(from.z, to.z, time));
        }

        public static Vector3 QuintInOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.QuintInOut(from.x, to.x, time), Ease.QuintInOut(from.y, to.y, time), Ease.QuintInOut(from.z, to.z, time));
        }

        public static Vector3 ExpoIn(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.ExpoIn(from.x, to.x, time), Ease.ExpoIn(from.y, to.y, time), Ease.ExpoIn(from.z, to.z, time));
        }

        public static Vector3 ExpoOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.ExpoOut(from.x, to.x, time), Ease.ExpoOut(from.y, to.y, time), Ease.ExpoOut(from.z, to.z, time));
        }

        public static Vector3 ExpoInOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.ExpoInOut(from.x, to.x, time), Ease.ExpoInOut(from.y, to.y, time), Ease.ExpoInOut(from.z, to.z, time));
        }

        public static Vector3 CircIn(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.CircIn(from.x, to.x, time), Ease.CircIn(from.y, to.y, time), Ease.CircIn(from.z, to.z, time));
        }

        public static Vector3 CircOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.CircOut(from.x, to.x, time), Ease.CircOut(from.y, to.y, time), Ease.CircOut(from.z, to.z, time));
        }

        public static Vector3 CircInOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.CircInOut(from.x, to.x, time), Ease.CircInOut(from.y, to.y, time), Ease.CircInOut(from.z, to.z, time));
        }

        public static Vector3 BackIn(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.BackIn(from.x, to.x, time), Ease.BackIn(from.y, to.y, time), Ease.BackIn(from.z, to.z, time));
        }

        public static Vector3 BackOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.BackOut(from.x, to.x, time), Ease.BackOut(from.y, to.y, time), Ease.BackOut(from.z, to.z, time));
        }

        public static Vector3 BackInOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.BackInOut(from.x, to.x, time), Ease.BackInOut(from.y, to.y, time), Ease.BackInOut(from.z, to.z, time));
        }

        public static Vector3 ElasticIn(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.ElasticIn(from.x, to.x, time), Ease.ElasticIn(from.y, to.y, time), Ease.ElasticIn(from.z, to.z, time));
        }

        public static Vector3 ElasticOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.ElasticOut(from.x, to.x, time), Ease.ElasticOut(from.y, to.y, time), Ease.ElasticOut(from.z, to.z, time));
        }

        public static Vector3 ElasticInOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.ElasticInOut(from.x, to.x, time), Ease.ElasticInOut(from.y, to.y, time), Ease.ElasticInOut(from.z, to.z, time));
        }

        public static Vector3 BounceIn(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.BounceIn(from.x, to.x, time), Ease.BounceIn(from.y, to.y, time), Ease.BounceIn(from.z, to.z, time));
        }

        public static Vector3 BounceOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.BounceOut(from.x, to.x, time), Ease.BounceOut(from.y, to.y, time), Ease.BounceOut(from.z, to.z, time));
        }

        public static Vector3 BounceInOut(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.BounceInOut(from.x, to.x, time), Ease.BounceInOut(from.y, to.y, time), Ease.BounceInOut(from.z, to.z, time));
        }

        public static Vector3 Spring(Vector3 from, Vector3 to, float time)
        {
            return new Vector3(Ease.Spring(from.x, to.x, time), Ease.Spring(from.y, to.y, time), Ease.Spring(from.z, to.z, time));
        }
    }

}
