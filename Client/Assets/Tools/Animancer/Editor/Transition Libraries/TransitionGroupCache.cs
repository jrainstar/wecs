// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;

namespace Animancer.Editor.TransitionLibraries
{
    /// <summary>A list of items in the <see cref="TransitionLibraryWindow"/> organised by group.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.TransitionLibraries/TransitionGroupCache
    public class TransitionGroupCache
    {
        /************************************************************************************************************************/

        private readonly List<object> Items = new();
        private readonly List<int> ItemIndexToSourceIndex = new();
        private readonly List<int> TransitionIndexToItemIndex = new();
        private readonly List<int> GroupIndexToItemIndex = new();
        private readonly List<TransitionGroup> ItemToGroup = new();

        /************************************************************************************************************************/

        /// <summary>The total number of items in this cache.</summary>
        public int Count
            => Items.Count;

        /************************************************************************************************************************/

        /// <summary>Tries to get the item at the specified `itemIndex`.</summary>
        public bool TryGet(int itemIndex, out object item)
            => Items.TryGet(itemIndex, out item);

        /************************************************************************************************************************/

        /// <summary>Returns the item at the specified `itemIndex`.</summary>
        public object GetItem(int itemIndex)
            => Items.TryGet(itemIndex, out var item)
            ? item
            : null;

        /// <summary>Returns the group containing the item at the specified `itemIndex`.</summary>
        public TransitionGroup GetGroup(int itemIndex)
            => ItemToGroup.TryGet(itemIndex, out var group)
            ? group
            : null;

        /************************************************************************************************************************/

        /// <summary>
        /// Converts the index of an item to its index in the source data,
        /// i.e. the <see cref="Animancer.TransitionLibraries.TransitionLibraryDefinition.Transitions"/>
        /// of <see cref="TransitionLibraryEditorDataInternal.TransitionGroups"/>.
        /// </summary>
        public int ItemToSourceIndex(int itemIndex)
            => itemIndex < 0
            ? -1
            : itemIndex >= ItemIndexToSourceIndex.Count
            ? ItemIndexToSourceIndex.Count
            : ItemIndexToSourceIndex[itemIndex];

        /// <summary>Converts the index of a transition to its index in the item list.</summary>
        public int TransitionToItemIndex(int transitionIndex)
            => TransitionIndexToItemIndex.TryGet(transitionIndex, out var itemIndex)
            ? itemIndex
            : int.MinValue;

        /// <summary>Converts the index of a group to its index in the item list.</summary>
        public int GroupToItemIndex(int groupIndex)
            => GroupIndexToItemIndex.TryGet(groupIndex, out var itemIndex)
            ? itemIndex
            : int.MinValue;

        /************************************************************************************************************************/

        /// <summary>
        /// Converts the `index` to a value for the <see cref="TransitionGroup.Index"/>,
        /// meaning it skips any items inside groups.
        /// </summary>
        public int ItemToGroupIndex(int index)
        {
            index = Mathf.Clamp(index, 0, Items.Count - 1);

            for (int i = index; i >= 0; i--)
            {
                var group = ItemToGroup[i];
                if (group != null && !ReferenceEquals(group, Items[i]))
                    index--;
            }

            return index;
        }

        /************************************************************************************************************************/

        /// <summary>Gathers the items from the specified library.</summary>
        public void GatherTransitionsAndGroups(
            TransitionAssetBase[] transitions,
            TransitionLibraryEditorDataInternal editorData)
            => GatherTransitionsAndGroups(transitions, editorData.TransitionGroups);

        /// <summary>Gathers the items from the specified library.</summary>
        public void GatherTransitionsAndGroups(
            TransitionAssetBase[] transitions,
            List<TransitionGroup> groups)
        {
            Items.Clear();
            ItemIndexToSourceIndex.Clear();
            TransitionIndexToItemIndex.Clear();
            GroupIndexToItemIndex.Clear();
            ItemToGroup.Clear();

            ValidateGroups(groups, transitions.Length);
            GatherTransitions(transitions, groups);
        }

        /************************************************************************************************************************/

        /// <summary>Ensures that the `groups` don't overlap and are within valid bounds.</summary>
        public static void ValidateGroups(
            List<TransitionGroup> groups,
            int transitionCount)
        {
            var previousGroupEnd = 0;

            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];

                if (group == null)
                {
                    groups.RemoveAt(i);
                    i--;
                    continue;
                }

                var groupIndex = group.Index;
                if (group.Index < previousGroupEnd)
                {
                    group.ReduceCount(previousGroupEnd - groupIndex);

                    group.Index = groupIndex = previousGroupEnd;
                }
                else if (group.Index >= transitionCount)
                {
                    group.Index = transitionCount;
                    group.Count = 0;
                }

                previousGroupEnd = groupIndex + group.Count;

                if (previousGroupEnd > transitionCount)
                {
                    group.ReduceCount(previousGroupEnd - transitionCount);

                    previousGroupEnd = transitionCount;
                }
            }
        }

        /************************************************************************************************************************/

        /// <summary>Gathers the `transitions` and `groups` while arranging their associated indices.</summary>
        private void GatherTransitions(
            TransitionAssetBase[] transitions,
            List<TransitionGroup> groups)
        {
            var transitionIndex = 0;

            // Iterate through each group and add preceding transitions, the group header, and group members.
            for (int iGroup = 0; iGroup < groups.Count; iGroup++)
            {
                var group = groups[iGroup];

                // Add transitions before this group that aren't part of any group.
                while (transitionIndex < group.Index)
                {
                    Items.Add(transitions[transitionIndex]);
                    ItemIndexToSourceIndex.Add(transitionIndex);
                    ItemToGroup.Add(null);
                    TransitionIndexToItemIndex.Add(Items.Count - 1);
                    transitionIndex++;
                }

                // Add the group header.
                GroupIndexToItemIndex.Add(Items.Count);
                Items.Add(group);
                ItemIndexToSourceIndex.Add(iGroup);
                ItemToGroup.Add(group);

                // Add transitions inside the group (either as items if expanded, or map them to the header if collapsed).
                var groupEnd = group.Index + group.Count;
                for (int iTransition = group.Index; iTransition < groupEnd; iTransition++)
                {
                    if (iTransition >= transitions.Length)
                        break;

                    if (group.IsExpanded)
                    {
                        Items.Add(transitions[iTransition]);
                        ItemIndexToSourceIndex.Add(iTransition);
                        ItemToGroup.Add(group);
                    }

                    TransitionIndexToItemIndex.Add(Items.Count - 1);
                }

                transitionIndex = group.Index + group.Count;
            }

            // Add any remaining transitions after the last group.
            while (transitionIndex < transitions.Length)
            {
                Items.Add(transitions[transitionIndex]);
                ItemIndexToSourceIndex.Add(transitionIndex);
                ItemToGroup.Add(null);
                TransitionIndexToItemIndex.Add(Items.Count - 1);
                transitionIndex++;
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

