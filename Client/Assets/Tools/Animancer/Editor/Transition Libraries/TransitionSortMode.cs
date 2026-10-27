// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

#if UNITY_EDITOR

using UnityEngine;

namespace Animancer.Editor.TransitionLibraries
{
    /// <summary>[Editor-Only]
    /// Sorting algorithms for <see cref="Animancer.TransitionLibraries.TransitionLibraryDefinition.Transitions"/>.
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.TransitionLibraries/TransitionSortMode
    public enum TransitionSortMode
    {
        /************************************************************************************************************************/

        /// <summary>Manual sorting.</summary>
        Custom,

        /// <summary>Based on the transition file names.</summary>
        Name,

        /// <summary>Based on the transition file paths.</summary>
        Path,

        /// <summary>Based on the transition types then file names.</summary>
        TypeThenName,

        /// <summary>Based on the transition types then file paths.</summary>
        TypeThenPath,

        /************************************************************************************************************************/
    }

    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.TransitionLibraries/TransitionLibraryEditorDataInternal
    partial class TransitionLibraryEditorDataInternal // TransitionSortMode.cs
    {
        /************************************************************************************************************************/

        /// <summary>The name of the serialized backing field of <see cref="TransitionSortMode"/>.</summary>
        internal const string TransitionSortModeFieldName = nameof(_TransitionSortMode);

        [SerializeField]
        private TransitionSortMode _TransitionSortMode;

        /// <summary>[<see cref="SerializeField"/>] The algorithm to use for sorting transitions.</summary>
        public TransitionSortMode TransitionSortMode
        {
            get => _TransitionSortMode;
            set => _TransitionSortMode = value;
        }

        /************************************************************************************************************************/
    }
}

#endif

