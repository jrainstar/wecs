// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

using System.Collections.Generic;

namespace Animancer.TransitionLibraries
{
    /// <summary>
    /// An <see cref="ITransition"/> and a dictionary to modify it based on the previous state.
    /// </summary>
    /// <remarks>
    /// <strong>Documentation:</strong>
    /// <see href="https://kybernetik.com.au/animancer/docs/manual/transitions/libraries">
    /// Transition Libraries</see>
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer.TransitionLibraries/TransitionModifierGroup
    public class TransitionModifierGroup :
        ICloneable<TransitionModifierGroup>,
        ICopyable<TransitionModifierGroup>
    {
        /************************************************************************************************************************/

        /// <summary>The index at which this group was added to its <see cref="TransitionLibrary"/>.</summary>
        public readonly int Index;

        /************************************************************************************************************************/

        private ITransition _Transition;

        /// <summary>The target transition of this group.</summary>
        /// <remarks>Can't be <c>null</c>.</remarks>
        public ITransition Transition
        {
            get => _Transition;
            set
            {
                AnimancerUtilities.Assert(
                    value != null,
                    $"{nameof(TransitionModifierGroup)}.{nameof(Transition)} can't be null.");

                _Transition = value;
            }
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Custom modifiers to use when playing a <see cref="Transition"/>
        /// depending on the <see cref="IHasKey.Key"/> of the source state it is coming from.
        /// </summary>
        /// <remarks>This is <c>null</c> by default until <see cref="SetModifier"/> adds something.</remarks>
        public Dictionary<object, TransitionDetails> FromKeyToModifier;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="TransitionModifierGroup"/>.</summary>
        public TransitionModifierGroup(
            int index,
            ITransition transition)
        {
            Index = index;
            Transition = transition;
        }

        /************************************************************************************************************************/

        /// <summary>Sets the `modifier` to use when transitioning from `from` to the <see cref="Transition"/>.</summary>
        public void SetModifier(object from, TransitionDetails modifier)
        {
            FromKeyToModifier ??= new();
            FromKeyToModifier[from] = modifier;
        }

        /// <summary>Removes the fade duration modifier set for transitioning from `from` to the <see cref="Transition"/>.</summary>
        public void ResetModifier(object from)
            => FromKeyToModifier?.Remove(from);

        /************************************************************************************************************************/

        /// <summary>
        /// Sets the <see cref="TransitionDetails.FadeDuration"/>
        /// to use when transitioning from `from` to the <see cref="Transition"/>.
        /// </summary>
        public void SetFadeDuration(object from, float fadeDuration)
        {
            FromKeyToModifier ??= new();

            if (!FromKeyToModifier.TryGetValue(from, out var modifier))
                modifier = TransitionDetails.NaN;

            modifier.FadeDuration = fadeDuration;

            FromKeyToModifier[from] = modifier;
        }

        /// <summary>
        /// Sets the <see cref="TransitionDetails.NormalizedStartTime"/>
        /// to use when transitioning from `from` to the <see cref="Transition"/>.
        /// </summary>
        public void SetNormalizedStartTime(object from, float normalizedStartTime)
        {
            FromKeyToModifier ??= new();

            if (!FromKeyToModifier.TryGetValue(from, out var modifier))
                modifier = TransitionDetails.NaN;

            modifier.NormalizedStartTime = normalizedStartTime;

            FromKeyToModifier[from] = modifier;
        }

        /************************************************************************************************************************/

        /// <summary>Returns the fade duration to use when transitioning from `from` to the <see cref="Transition"/>.</summary>
        public TransitionDetails GetDetails(object from)
        {
            if (FromKeyToModifier != null && from != null)
            {
                from = AnimancerUtilities.GetRootKey(from);
                if (FromKeyToModifier.TryGetValue(from, out var details))
                {
                    if (float.IsNaN(details.FadeDuration))
                        details.FadeDuration = Transition.FadeDuration;

                    if (float.IsNaN(details.NormalizedStartTime))
                        details.NormalizedStartTime = Transition.NormalizedStartTime;

                    return details;
                }
            }

            return new(Transition);
        }

        /************************************************************************************************************************/

        /// <summary>Returns the fade duration to use when transitioning from `from` to the <see cref="Transition"/>.</summary>
        public float GetFadeDuration(object from)
            => FromKeyToModifier != null
            && FromKeyToModifier.TryGetValue(AnimancerUtilities.GetRootKey(from), out var modifier)
            ? modifier.FadeDuration
            : Transition.FadeDuration;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public TransitionModifierGroup Clone(CloneContext context)
        {
            var clone = new TransitionModifierGroup(Index, null);
            clone.CopyFrom(this);
            return clone;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public void CopyFrom(TransitionModifierGroup copyFrom, CloneContext context)
        {
            Transition = copyFrom.Transition;

            if (copyFrom.FromKeyToModifier == null)
            {
                FromKeyToModifier?.Clear();
            }
            else
            {
                FromKeyToModifier ??= new();
                foreach (var item in copyFrom.FromKeyToModifier)
                    FromKeyToModifier[item.Key] = item.Value;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Describes this object.</summary>
        public override string ToString()
            => $"{nameof(TransitionModifierGroup)}([{Index}] {AnimancerUtilities.ToStringOrNull(Transition)})";

        /************************************************************************************************************************/
    }
}

