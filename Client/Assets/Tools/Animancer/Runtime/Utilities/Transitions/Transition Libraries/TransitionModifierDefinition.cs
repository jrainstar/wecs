// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

using System;
using UnityEngine;

namespace Animancer.TransitionLibraries
{
    /// <summary>[<see cref="SerializableAttribute"/>]
    /// Details about how to modify a transition when it comes from a specific source.
    /// </summary>
    /// <remarks>
    /// Multiple of these can be used to build a <see cref="TransitionModifierGroup"/> at runtime.
    /// <para></para>
    /// <strong>Documentation:</strong>
    /// <see href="https://kybernetik.com.au/animancer/docs/manual/transitions/libraries">
    /// Transition Libraries</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer.TransitionLibraries/TransitionModifierDefinition
    [Serializable]
    public struct TransitionModifierDefinition :
        IEquatable<TransitionModifierDefinition>
    {
        /************************************************************************************************************************/

        [SerializeField]
        private int _From;

        /// <summary>The index of the source transition in the <see cref="TransitionLibraryDefinition"/>.</summary>
        public readonly int FromIndex
            => _From;

        /************************************************************************************************************************/

        [SerializeField]
        private int _To;

        /// <summary>The index of the destination transition in the <see cref="TransitionLibraryDefinition"/>.</summary>
        public readonly int ToIndex
            => _To;

        /************************************************************************************************************************/

        [SerializeField]
        private float _Fade;

        /// <summary>The fade duration for this modifier to use instead of the transition's default value.</summary>
        public readonly float FadeDuration
            => _Fade;

        /************************************************************************************************************************/

        [SerializeField]
        private float _NormalizedStartTime;

        /// <summary>The normalized start time for this modifier to use instead of the transition's default value.</summary>
        public readonly float NormalizedStartTime
            => _NormalizedStartTime;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="TransitionModifierDefinition"/>.</summary>
        public TransitionModifierDefinition(
            int fromIndex,
            int toIndex,
            float fadeDuration,
            float normalizedStartTime)
        {
            _From = fromIndex;
            _To = toIndex;
            _Fade = fadeDuration;
            _NormalizedStartTime = normalizedStartTime;
        }

        /************************************************************************************************************************/

        /// <summary>Does this modifier contain valid values?</summary>
        public bool Validate()
        {
            var noFade = float.IsNaN(_Fade);
            var noStart = float.IsNaN(_NormalizedStartTime);
            if (noFade && noStart)
                return false;

            if (!noFade)
            {
                if (_Fade < 0)
                    _Fade = 0;
            }

            return true;
        }

        /************************************************************************************************************************/

        /// <summary>Creates a copy of this modifier with the specified <see cref="FadeDuration"/>.</summary>
        public readonly TransitionModifierDefinition WithFadeDuration(float fadeDuration)
            => new(_From, _To, fadeDuration, _NormalizedStartTime);

        /// <summary>Creates a copy of this modifier with the specified <see cref="NormalizedStartTime"/>.</summary>
        public readonly TransitionModifierDefinition WithNormalizedStartTime(float normalizedStartTime)
            => new(_From, _To, _Fade, normalizedStartTime);

        /// <summary>Creates a copy of this modifier with the specified <see cref="FadeDuration"/> and <see cref="NormalizedStartTime"/>.</summary>
        public readonly TransitionModifierDefinition WithDetails(float fadeDuration, float normalizedStartTime)
            => new(_From, _To, fadeDuration, normalizedStartTime);

        /// <summary>Creates a copy of this modifier with the specified <see cref="FromIndex"/> and <see cref="ToIndex"/>.</summary>
        public readonly TransitionModifierDefinition WithIndices(int fromIndex, int toIndex)
            => new(fromIndex, toIndex, _Fade, _NormalizedStartTime);

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="TransitionDetails"/> from this modifier.</summary>
        public readonly TransitionDetails ToTransitionDetails()
            => new(_Fade, _NormalizedStartTime);

        /************************************************************************************************************************/

        /// <summary>Creates a new string describing this modifier.</summary>
        public override readonly string ToString()
            => $"{nameof(TransitionModifierDefinition)}({_From}->{_To}, F={_Fade}, S={_NormalizedStartTime})";

        /************************************************************************************************************************/
        #region Equality
        /************************************************************************************************************************/

        /// <summary>Are all fields in this object equal to the equivalent in `obj`?</summary>
        public override readonly bool Equals(object obj)
            => obj is TransitionModifierDefinition value
            && Equals(value);

        /// <summary>Are all fields in this object equal to the equivalent fields in `other`?</summary>
        public readonly bool Equals(TransitionModifierDefinition other)
            => _From == other._From
            && _To == other._To
            && _Fade.IsEqualOrBothNaN(other._Fade)
            && _NormalizedStartTime.IsEqualOrBothNaN(other._NormalizedStartTime);

        /// <summary>Are all fields in `a` equal to the equivalent fields in `b`?</summary>
        public static bool operator ==(TransitionModifierDefinition a, TransitionModifierDefinition b)
            => a.Equals(b);

        /// <summary>Are any fields in `a` not equal to the equivalent fields in `b`?</summary>
        public static bool operator !=(TransitionModifierDefinition a, TransitionModifierDefinition b)
            => !(a == b);

        /************************************************************************************************************************/

        /// <summary>Returns a hash code based on the values of this object's fields.</summary>
        public override readonly int GetHashCode()
            => AnimancerUtilities.Hash(-871379578,
                _From.GetHashCode(),
                _To.GetHashCode(),
                _Fade.GetHashCode(),
                _NormalizedStartTime.GetHashCode());

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

