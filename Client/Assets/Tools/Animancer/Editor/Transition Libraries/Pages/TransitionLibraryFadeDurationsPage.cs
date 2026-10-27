// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

#if UNITY_EDITOR

using Animancer.TransitionLibraries;
using System;

namespace Animancer.Editor.TransitionLibraries
{
    /// <summary>[Editor-Only]
    /// A <see cref="TransitionLibraryWindowPage"/> for editing 
    /// <see cref="TransitionModifierDefinition.FadeDuration"/>.
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.TransitionLibraries/TransitionLibraryFadeDurationsPage
    [Serializable]
    public class TransitionLibraryFadeDurationsPage : TransitionLibraryModifiersPage
    {
        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override string DisplayName
            => "Fade Duration Modifiers";

        /// <inheritdoc/>
        public override string HelpTooltip
            => "Modifiers allow you to replace the usual fade duration for specific combinations of transitions.";

        /// <inheritdoc/>
        public override int Index
            => 0;

        /// <inheritdoc/>
        public TransitionLibraryFadeDurationsPage()
            : base(Units.AnimationTimeAttribute.Units.Seconds)
        { }

        /// <inheritdoc/>
        public override float GetValue(ITransition transition)
            => transition.FadeDuration;

        /// <inheritdoc/>
        public override float GetValue(TransitionModifierDefinition modifier)
            => modifier.FadeDuration;

        /// <inheritdoc/>
        public override void SetValue(ref TransitionModifierDefinition modifier, float value)
            => modifier = modifier.WithFadeDuration(value);

        /************************************************************************************************************************/
    }
}

#endif

