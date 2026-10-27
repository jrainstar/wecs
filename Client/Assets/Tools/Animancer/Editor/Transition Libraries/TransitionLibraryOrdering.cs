// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

#if UNITY_EDITOR

using Animancer.TransitionLibraries;
using System;
using UnityEngine;
using static Animancer.Editor.TransitionLibraries.TransitionLibrarySelection;

namespace Animancer.Editor.TransitionLibraries
{
    /// <summary>[Editor-Only]
    /// Operations for modifying the order of items in a <see cref="TransitionLibraryAsset"/>.
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.TransitionLibraries/TransitionLibraryOrdering
    public static class TransitionLibraryOrdering
    {
        /************************************************************************************************************************/

        /// <summary>Handles a drag and drop operation.</summary>
        public static void OnDropItem(
            this TransitionLibraryWindow window,
            object item,
            int fromItemIndex,
            ListTargetCalculation target,
            SelectionType selectionType)
        {
            if (fromItemIndex == target.Index)
                return;

            window.RecordUndo();
            window.EditorData.TransitionSortMode = TransitionSortMode.Custom;

            if (item is TransitionGroup group)
                OnDropGroup(window, group, fromItemIndex, target);
            else
                OnDropTransition(window, fromItemIndex, target, selectionType);
        }

        /************************************************************************************************************************/

        /// <summary>Handles a drag and drop operation for a `group`.</summary>
        private static void OnDropGroup(
            TransitionLibraryWindow window,
            TransitionGroup group,
            int fromItemIndex,
            ListTargetCalculation target)
        {
            var toGroup = window.Items.GetGroup(target.Index);
            if (group == toGroup)
                return;

            int toTransitionIndex;

            if (toGroup == null)
            {
                toTransitionIndex = window.Items.ItemToSourceIndex(target.Index);

                if (target.LocalOffset > 0.5f)
                    toTransitionIndex++;
            }
            else
            {
                toTransitionIndex = toGroup.Index;

                if (!ReferenceEquals(window.Items.GetItem(target.Index), toGroup) ||
                    target.LocalOffset > 0.5f)
                    toTransitionIndex += toGroup.Count;
            }

            if (toTransitionIndex <= 0)
                toTransitionIndex = 0;
            else if (toTransitionIndex > window.Data.Transitions.Length)
                toTransitionIndex = window.Data.Transitions.Length;

            var fromTransitionIndex = group.Index;
            if (toTransitionIndex > fromTransitionIndex)
                toTransitionIndex -= group.Count;

            if (target.LocalOffset > 0.5f)
                target.Index++;

            MoveGroupTransitions(window, fromTransitionIndex, toTransitionIndex, group.Count);
            MoveGroup(window, group, toGroup, target.Index, toTransitionIndex);
        }

        /************************************************************************************************************************/

        /// <summary>Moves the transitions within a group.</summary>
        private static void MoveGroupTransitions(
            TransitionLibraryWindow window,
            int fromTransitionIndex,
            int toTransitionIndex,
            int count)
        {
            // Order matters with multiple items depending on the direction of the move.
            if (fromTransitionIndex < toTransitionIndex)
            {
                for (int i = count - 1; i >= 0; i--)
                {
                    TransitionLibrarySort.MoveTransition(
                        window,
                        i + fromTransitionIndex,
                        i + toTransitionIndex);
                }
            }
            else if (fromTransitionIndex > toTransitionIndex)
            {
                for (int i = 0; i < count; i++)
                {
                    TransitionLibrarySort.MoveTransition(
                        window,
                        i + fromTransitionIndex,
                        i + toTransitionIndex);
                }
            }
        }

        /************************************************************************************************************************/

        private static void MoveGroup(
            TransitionLibraryWindow window,
            TransitionGroup moveGroup,
            TransitionGroup toGroup,
            int toItemIndex,
            int toTransitionIndex)
        {
            moveGroup.Index = toTransitionIndex;

            var groups = window.EditorData.TransitionGroups;
            if (groups.Count <= 1)
            {
                window.Selection.Select(window, moveGroup, 0, SelectionType.Group);
                return;
            }

            var fromGroupIndex = groups.IndexOf(moveGroup);

            var toGroupIndex = 0;

            if (toGroup != null)
            {
                toGroupIndex = groups.IndexOf(toGroup);

                var toGroupItemIndex = window.Items.GroupToItemIndex(toGroupIndex);
                if (toItemIndex > toGroupItemIndex)
                    toGroupIndex++;
            }
            else
            {
                while (toGroupIndex < groups.Count &&
                    groups[toGroupIndex].Index < toTransitionIndex)
                {
                    toGroupIndex++;
                }
            }

            if (toGroupIndex > fromGroupIndex)
                toGroupIndex--;

            if (toGroupIndex == fromGroupIndex)
                return;

            groups.RemoveAt(fromGroupIndex);
            groups.Insert(toGroupIndex, moveGroup);

            if (toGroupIndex > fromGroupIndex)
            {
                for (int i = fromGroupIndex; i < toGroupIndex; i++)
                    groups[i].Index -= moveGroup.Count;
            }
            else
            {
                for (int i = toGroupIndex + 1; i <= fromGroupIndex; i++)
                    groups[i].Index += moveGroup.Count;
            }

            window.Selection.Select(window, moveGroup, toGroupIndex, SelectionType.Group);
        }

        /************************************************************************************************************************/

        /// <summary>Handles a drag and drop operation for a transition.</summary>
        private static void OnDropTransition(
            TransitionLibraryWindow window,
            int fromItemIndex,
            ListTargetCalculation target,
            SelectionType selectionType)
        {
            AdjustGroupsOnTransitionMoved(
                window,
                fromItemIndex,
                target,
                out var fromTransitionIndex,
                out var toTransitionIndex);

            TransitionLibrarySort.MoveTransition(
                window,
                fromTransitionIndex,
                toTransitionIndex);

            if (window.Data.Transitions.TryGet(toTransitionIndex, out var transition))
                window.Selection.Select(window, transition, toTransitionIndex, selectionType);
        }

        /************************************************************************************************************************/

        /// <summary>Adjusts the indices of transition groups when a transition is moved.</summary>
        private static void AdjustGroupsOnTransitionMoved(
            TransitionLibraryWindow window,
            int fromItemIndex,
            ListTargetCalculation target,
            out int fromTransitionIndex,
            out int toTransitionIndex)
        {
            fromTransitionIndex = window.Items.ItemToSourceIndex(fromItemIndex);
            var toItem = window.Items.GetItem(target.Index);
            toTransitionIndex = toItem is TransitionGroup toGroup
                ? toGroup.Index
                : window.Items.ItemToSourceIndex(target.Index);

            if (target.LocalOffset > 0.5f)
                toTransitionIndex++;

            if (toTransitionIndex > fromTransitionIndex)
                toTransitionIndex--;

            var fromGroup = window.Items.GetGroup(fromItemIndex);
            toGroup = window.Items.GetGroup(target.Index);

            if (fromGroup != null)
            {
                if (fromGroup == toGroup)
                {
                    if (ReferenceEquals(toItem, toGroup))
                    {
                        // Top half = move outside the group.
                        if (target.LocalOffset <= 0.5f)
                        {
                            toGroup.Count--;
                            toGroup.Index++;
                        }
                        else // Bottom half = reorder within the group.
                        {
                            toTransitionIndex--;
                        }
                    }

                    return;
                }

                fromGroup.Count--;

                if (fromGroup.Index >= toTransitionIndex)
                    fromGroup.Index++;
            }

            if (toGroup != null)
            {
                // Drop directly onto a group.
                if (ReferenceEquals(toItem, toGroup))
                {
                    // Top half = move outside the group.
                    if (target.LocalOffset <= 0.5f)
                    {
                        if (toGroup.Index < fromTransitionIndex)
                            toGroup.Index++;
                    }
                    else // Bottom half = add to the group.
                    {
                        toTransitionIndex--;

                        toGroup.Count++;

                        if (toGroup.Index > fromTransitionIndex)
                            toGroup.Index--;
                    }
                }
                else // Drop onto a transition in a group = add to the group.
                {
                    toGroup.Count++;

                    if (toGroup.Index > fromTransitionIndex)
                        toGroup.Index--;
                }
            }

            int minTransitionIndex, maxTransitionIndex, increment;
            if (fromTransitionIndex < toTransitionIndex)
            {
                minTransitionIndex = fromTransitionIndex;
                maxTransitionIndex = toTransitionIndex;
                increment = -1;
            }
            else
            {
                minTransitionIndex = toTransitionIndex;
                maxTransitionIndex = fromTransitionIndex;
                increment = 1;
            }

            var groups = window.EditorData.TransitionGroups;
            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                if (group == fromGroup ||
                    group == toGroup ||
                    !group.IsInRange(minTransitionIndex, maxTransitionIndex))
                    continue;

                group.Index += increment;
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

