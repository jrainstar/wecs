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
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.TransitionLibraries/TransitionLibraryStartTimesPage
    [Serializable]
    public class TransitionLibraryStartTimesPage : TransitionLibraryModifiersPage
    {
        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override string DisplayName
            => "Start Time Modifiers";

        /// <inheritdoc/>
        public override string HelpTooltip
            => "Modifiers allow you to replace the usual start time for specific combinations of transitions.";

        /// <inheritdoc/>
        public override int Index
            => 1;

        private readonly string[] ConvertedZeroes;

        /// <inheritdoc/>
        public TransitionLibraryStartTimesPage()
            : base(Units.AnimationTimeAttribute.Units.Normalized)
        {
            TimeDrawer.Attribute.DisabledText = Strings.Tooltips.StartTimeDisabled;

            var converters = TimeDrawer.DisplayConverters;
            ConvertedZeroes = new string[converters.Length];
            for (int i = 0; i < converters.Length; i++)
                ConvertedZeroes[i] = converters[i].ConvertedZero;
        }

        /// <inheritdoc/>
        public override void ConfigureForSingleField(bool singleField, ref float value)
        {
            var isSingleFieldNaN = singleField && float.IsNaN(value);
            if (isSingleFieldNaN)
                value = 0;

            var converters = TimeDrawer.DisplayConverters;
            for (int i = 0; i < converters.Length; i++)
            {
                var converter = converters[i];
                converter.ConvertedZero = isSingleFieldNaN
                    ? Strings.Tooltips.StartTimeDisabled
                    : ConvertedZeroes[i];
            }
        }

        /// <inheritdoc/>
        public override float GetValue(ITransition transition)
            => transition.NormalizedStartTime;

        /// <inheritdoc/>
        public override float GetValue(TransitionModifierDefinition modifier)
            => modifier.NormalizedStartTime;

        /// <inheritdoc/>
        public override void SetValue(ref TransitionModifierDefinition modifier, float value)
            => modifier = modifier.WithNormalizedStartTime(value);

        /************************************************************************************************************************/
    }
}

#endif

