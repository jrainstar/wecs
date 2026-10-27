// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

#if UNITY_EDITOR

using Animancer.TransitionLibraries;
using System;
using UnityEngine;

namespace Animancer.Editor.TransitionLibraries
{
    /// <summary>[Editor-Only]
    /// Additional data for a <see cref="TransitionLibraryAsset"/> which is excluded from Runtime Builds.
    /// </summary>
    /// <remarks>
    /// This class isn't called <c>TransitionLibraryEditorData</c> because
    /// <see cref="TransitionLibraryEditorDataAsset"/> previously had that name
    /// and changing from a <see cref="ScriptableObject"/> to a regular class with the same name
    /// causes errors for any already existing assets of that type.
    /// </remarks>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.TransitionLibraries/TransitionLibraryEditorDataInternal
    [Serializable]
    public partial class TransitionLibraryEditorDataInternal : // TransitionLibraryEditorDataInternal.cs
        ICopyable<TransitionLibraryEditorDataInternal>,
        IEquatable<TransitionLibraryEditorDataInternal>
    {
        /************************************************************************************************************************/
        #region Equality
        /************************************************************************************************************************/

        /// <summary>Are all fields in this object equal to the equivalent in `obj`?</summary>
        public override bool Equals(object obj)
            => Equals(obj as TransitionLibraryEditorDataInternal);

        /// <summary>Are all fields in this object equal to the equivalent fields in `other`?</summary>
        public bool Equals(TransitionLibraryEditorDataInternal other)
            => other != null
            && _TransitionSortMode == other._TransitionSortMode
            && AnimancerUtilities.ContentsAreEqual(_TransitionGroups, other._TransitionGroups);

        /// <summary>Are all fields in `a` equal to the equivalent fields in `b`?</summary>
        public static bool operator ==(TransitionLibraryEditorDataInternal a, TransitionLibraryEditorDataInternal b)
            => a is null
            ? b is null
            : a.Equals(b);

        /// <summary>Are any fields in `a` not equal to the equivalent fields in `b`?</summary>
        public static bool operator !=(TransitionLibraryEditorDataInternal a, TransitionLibraryEditorDataInternal b)
            => !(a == b);

        /************************************************************************************************************************/

        /// <summary>Returns a hash code based on the values of this object's fields.</summary>
        public override int GetHashCode()
            => AnimancerUtilities.Hash(287475157,
                _TransitionSortMode.GetHashCode(),
                _TransitionGroups.SafeGetHashCode());

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public void CopyFrom(TransitionLibraryEditorDataInternal copyFrom, CloneContext context)
        {
            _TransitionSortMode = copyFrom._TransitionSortMode;

            var myGroups = TransitionGroups;
            var copyGroups = copyFrom.TransitionGroups;
            myGroups.Clear();
            for (int i = 0; i < copyGroups.Count; i++)
                myGroups.Add(copyGroups[i].CopyableClone(context));
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

