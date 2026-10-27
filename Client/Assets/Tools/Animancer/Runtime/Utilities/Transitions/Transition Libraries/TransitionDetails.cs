// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

namespace Animancer.TransitionLibraries
{
    /// <summary>Values which determine how a transition is played.</summary>
    /// <remarks>
    /// <strong>Documentation:</strong>
    /// <see href="https://kybernetik.com.au/animancer/docs/manual/transitions/libraries">
    /// Transition Libraries</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer.TransitionLibraries/TransitionDetails
    public struct TransitionDetails
    {
        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="TransitionDetails"/> with all values as <see cref="float.NaN"/>.</summary>
        public static TransitionDetails NaN
            => new(float.NaN, float.NaN);

        /************************************************************************************************************************/

        /// <summary><see cref="ITransition.FadeDuration"/></summary>
        public float FadeDuration;

        /// <summary><see cref="ITransition.NormalizedStartTime"/></summary>
        public float NormalizedStartTime;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="TransitionDetails"/>.</summary>
        public TransitionDetails(
            float fadeDuration,
            float normalizedStartTime)
        {
            FadeDuration = fadeDuration;
            NormalizedStartTime = normalizedStartTime;
        }

        /// <summary>Creates a new <see cref="TransitionDetails"/>.</summary>
        public TransitionDetails(
            ITransition transition)
        {
            FadeDuration = transition.FadeDuration;
            NormalizedStartTime = transition.NormalizedStartTime;
        }

        /************************************************************************************************************************/

        /// <summary>Returns a description of this object.</summary>
        public override readonly string ToString()
            => $"{nameof(TransitionDetails)}(" +
            $"{nameof(FadeDuration)}={FadeDuration}, " +
            $"{nameof(NormalizedStartTime)}={NormalizedStartTime})";

        /************************************************************************************************************************/
    }
}

