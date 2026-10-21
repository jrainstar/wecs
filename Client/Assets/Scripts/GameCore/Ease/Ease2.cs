using System.Collections.Generic;
using System;
using UnityEngine;

namespace GameCore
{
    public class Ease2
    {
        public static Vector2 Linear(Vector2 from, Vector2 to, float time)
        {
            return Vector2.Lerp(from, to, time);
        }

        public static Vector2 SineIn(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.SineIn(from.x, to.x, time), Ease.SineIn(from.y, to.y, time));
        }

        public static Vector2 SineOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.SineOut(from.x, to.x, time), Ease.SineOut(from.y, to.y, time));
        }

        public static Vector2 SineInOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.SineInOut(from.x, to.x, time), Ease.SineInOut(from.y, to.y, time));
        }

        public static Vector2 QuadIn(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.QuadIn(from.x, to.x, time), Ease.QuadIn(from.y, to.y, time));
        }

        public static Vector2 QuadOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.QuadOut(from.x, to.x, time), Ease.QuadOut(from.y, to.y, time));
        }

        public static Vector2 QuadInOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.QuadInOut(from.x, to.x, time), Ease.QuadInOut(from.y, to.y, time));
        }

        public static Vector2 CubicIn(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.CubicIn(from.x, to.x, time), Ease.CubicIn(from.y, to.y, time));
        }

        public static Vector2 CubicOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.CubicOut(from.x, to.x, time), Ease.CubicOut(from.y, to.y, time));
        }

        public static Vector2 CubicInOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.CubicInOut(from.x, to.x, time), Ease.CubicInOut(from.y, to.y, time));
        }

        public static Vector2 QuartIn(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.QuartIn(from.x, to.x, time), Ease.QuartIn(from.y, to.y, time));
        }

        public static Vector2 QuartOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.QuartOut(from.x, to.x, time), Ease.QuartOut(from.y, to.y, time));
        }

        public static Vector2 QuartInOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.QuartInOut(from.x, to.x, time), Ease.QuartInOut(from.y, to.y, time));
        }

        public static Vector2 QuintIn(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.QuintIn(from.x, to.x, time), Ease.QuintIn(from.y, to.y, time));
        }

        public static Vector2 QuintOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.QuintOut(from.x, to.x, time), Ease.QuintOut(from.y, to.y, time));
        }

        public static Vector2 QuintInOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.QuintInOut(from.x, to.x, time), Ease.QuintInOut(from.y, to.y, time));
        }

        public static Vector2 ExpoIn(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.ExpoIn(from.x, to.x, time), Ease.ExpoIn(from.y, to.y, time));
        }

        public static Vector2 ExpoOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.ExpoOut(from.x, to.x, time), Ease.ExpoOut(from.y, to.y, time));
        }

        public static Vector2 ExpoInOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.ExpoInOut(from.x, to.x, time), Ease.ExpoInOut(from.y, to.y, time));
        }

        public static Vector2 CircIn(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.CircIn(from.x, to.x, time), Ease.CircIn(from.y, to.y, time));
        }

        public static Vector2 CircOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.CircOut(from.x, to.x, time), Ease.CircOut(from.y, to.y, time));
        }

        public static Vector2 CircInOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.CircInOut(from.x, to.x, time), Ease.CircInOut(from.y, to.y, time));
        }

        public static Vector2 BackIn(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.BackIn(from.x, to.x, time), Ease.BackIn(from.y, to.y, time));
        }

        public static Vector2 BackOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.BackOut(from.x, to.x, time), Ease.BackOut(from.y, to.y, time));
        }

        public static Vector2 BackInOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.BackInOut(from.x, to.x, time), Ease.BackInOut(from.y, to.y, time));
        }

        public static Vector2 ElasticIn(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.ElasticIn(from.x, to.x, time), Ease.ElasticIn(from.y, to.y, time));
        }

        public static Vector2 ElasticOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.ElasticOut(from.x, to.x, time), Ease.ElasticOut(from.y, to.y, time));
        }

        public static Vector2 ElasticInOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.ElasticInOut(from.x, to.x, time), Ease.ElasticInOut(from.y, to.y, time));
        }

        public static Vector2 BounceIn(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.BounceIn(from.x, to.x, time), Ease.BounceIn(from.y, to.y, time));
        }

        public static Vector2 BounceOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.BounceOut(from.x, to.x, time), Ease.BounceOut(from.y, to.y, time));
        }

        public static Vector2 BounceInOut(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.BounceInOut(from.x, to.x, time), Ease.BounceInOut(from.y, to.y, time));
        }

        public static Vector2 Spring(Vector2 from, Vector2 to, float time)
        {
            return new Vector2(Ease.Spring(from.x, to.x, time), Ease.Spring(from.y, to.y, time));
        }
    }
}

