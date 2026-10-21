using UnityEngine;

namespace GameCore
{
    public class Ease
    {
        private const float HalfPi = Mathf.PI * .5f;
        private const float DoublePi = Mathf.PI * 2f;

        public static float SineIn(float from, float to, float time)
        {
            return Mathf.Lerp(from, to, 1f - Mathf.Cos(time * HalfPi));
        }

        public static float SineOut(float from, float to, float time)
        {
            return Mathf.Lerp(from, to, Mathf.Sin(time * HalfPi));
        }

        public static float SineInOut(float from, float to, float time)
        {
            return Mathf.Lerp(from, to, .5f * (1f - Mathf.Cos(Mathf.PI * time)));
        }

        public static float QuadIn(float from, float to, float time)
        {
            return Mathf.Lerp(from, to, time * time);
        }

        public static float QuadOut(float from, float to, float time)
        {
            return Mathf.Lerp(from, to, -time * (time - 2f));
        }

        public static float QuadInOut(float from, float to, float time)
        {
            if ((time /= .5f) < 1f)
                return Mathf.Lerp(from, to, .5f * time * time);
            return Mathf.Lerp(from, to, -.5f * (((--time) * (time - 2f) - 1f)));
        }

        public static float CubicIn(float from, float to, float time)
        {
            return Mathf.Lerp(from, to, time * time * time);
        }

        public static float CubicOut(float from, float to, float time)
        {
            return Mathf.Lerp(from, to, (time -= 1f) * time * time + 1f);
        }

        public static float CubicInOut(float from, float to, float time)
        {
            if ((time /= .5f) < 1f)
                return Mathf.Lerp(from, to, .5f * time * time * time);
            return Mathf.Lerp(from, to, .5f * ((time -= 2) * time * time + 2f));
        }

        public static float QuartIn(float from, float to, float time)
        {
            return Mathf.Lerp(from, to, time * time * time * time);
        }

        public static float QuartOut(float from, float to, float time)
        {
            return Mathf.Lerp(from, to, -((time -= 1f) * time * time * time - 1f));
        }

        public static float QuartInOut(float from, float to, float time)
        {
            if ((time /= .5f) < 1f)
                return Mathf.Lerp(from, to, .5f * time * time * time * time);
            return Mathf.Lerp(from, to, -.5f * ((time -= 2f) * time * time * time - 2f));
        }

        public static float QuintIn(float from, float to, float time)
        {
            return Mathf.Lerp(from, to, time * time * time * time * time);
        }

        public static float QuintOut(float from, float to, float time)
        {
            return Mathf.Lerp(from, to, (time -= 1f) * time * time * time * time + 1f);
        }

        public static float QuintInOut(float from, float to, float time)
        {
            if ((time /= .5f) < 1f)
                return Mathf.Lerp(from, to, .5f * time * time * time * time * time);
            return Mathf.Lerp(from, to, .5f * ((time -= 2f) * time * time * time * time + 2f));
        }

        public static float ExpoIn(float from, float to, float time)
        {
            return Mathf.Lerp(from, to, Mathf.Pow(2f, 10f * (time - 1f)));
        }

        public static float ExpoOut(float from, float to, float time)
        {
            return Mathf.Lerp(from, to, -Mathf.Pow(2f, -10f * time) + 1f);
        }

        public static float ExpoInOut(float from, float to, float time)
        {
            if ((time /= .5f) < 1f)
                return Mathf.Lerp(from, to, .5f * Mathf.Pow(2f, 10f * (time - 1f)));
            return Mathf.Lerp(from, to, .5f * (-Mathf.Pow(2f, -10f * --time) + 2f));
        }

        public static float CircIn(float from, float to, float time)
        {
            return Mathf.Lerp(from, to, -(Mathf.Sqrt(1f - time * time) - 1f));
        }

        public static float CircOut(float from, float to, float time)
        {
            return Mathf.Lerp(from, to, Mathf.Sqrt(1f - (time -= 1f) * time));
        }

        public static float CircInOut(float from, float to, float time)
        {
            if ((time /= .5f) < 1f)
                return Mathf.Lerp(from, to, -.5f * (Mathf.Sqrt(1f - time * time) - 1f));
            return Mathf.Lerp(from, to, .5f * (Mathf.Sqrt(1f - (time -= 2f) * time) + 1f));
        }

        public static float BackIn(float from, float to, float time)
        {
            const float s = 1.70158f;
            to -= from;
            return to * time * time * ((s + 1f) * time - s) + from;
        }

        public static float BackOut(float from, float to, float time)
        {
            const float s = 1.70158f;
            to -= from;
            return to * (--time * time * ((s + 1f) * time + s) + 1f) + from;
        }

        public static float BackInOut(float from, float to, float time)
        {
            const float s = 1.70158f * 1.525f;
            to -= from;
            if ((time /= .5f) < 1f)
                return to * .5f * (time * time * ((s + 1f) * time - s)) + from;
            return to * .5f * ((time -= 2) * time * ((s + 1f) * time + s) + 2f) + from;
        }

        public static float ElasticIn(float from, float to, float time)
        {
            const float p = .3f;
            const float s = p / 4f;
            to -= from;
            return to * -(Mathf.Pow(2f, 10f * (time -= 1f)) * Mathf.Sin((time - s) * DoublePi / p)) + from;
        }

        public static float ElasticOut(float from, float to, float time)
        {
            const float p = .3f;
            const float s = p / 4f;
            to -= from;
            return to * Mathf.Pow(2f, -10f * time) * Mathf.Sin((time - s) * DoublePi / p) + to + from;
        }

        public static float ElasticInOut(float from, float to, float time)
        {
            const float p = .3f * 1.5f;
            const float s = p / 4f;
            to -= from;
            if ((time /= .5f) < 1f)
                return -.5f * (to * Mathf.Pow(2f, 10f * (time -= 1f)) * Mathf.Sin((time - s) * DoublePi / p)) + from;
            return to * Mathf.Pow(2f, -10f * (time -= 1f)) * Mathf.Sin((time - s) * DoublePi / p) * .5f + to + from;
        }

        public static float BounceIn(float from, float to, float time)
        {
            to -= from;
            return to - BounceOut(0f, to, 1f - time) + from;
        }

        public static float BounceOut(float from, float to, float time)
        {
            to -= from;
            if (time < (1f / 2.75f))
                return to * (7.5625f * time * time) + from;
            if (time < (2f / 2.75f))
                return to * (7.5625f * (time -= (1.5f / 2.75f)) * time + .75f) + from;
            if (time < (2.5f / 2.75f))
                return to * (7.5625f * (time -= (2.25f / 2.75f)) * time + .9375f) + from;
            return to * (7.5625f * (time -= (2.625f / 2.75f)) * time + .984375f) + from;
        }

        public static float BounceInOut(float from, float to, float time)
        {
            to -= from;
            if (time < .5f)
                return BounceIn(0f, to, time * 2f) * .5f + from;
            return BounceOut(0f, to, time * 2f - 1f) * .5f + to * .5f + from;
        }

        public static float Spring(float from, float to, float time)
        {
            time = Mathf.Clamp01(time);
            time = (Mathf.Sin(time * Mathf.PI * (.2f + 2.5f * time * time * time)) * Mathf.Pow(1f - time, 2.2f) + time) * (1f + (1.2f * (1f - time)));
            return from + (to - from) * time;
        }
    }

}
