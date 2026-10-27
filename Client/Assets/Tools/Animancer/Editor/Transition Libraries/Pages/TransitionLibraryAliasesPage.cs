// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

#if UNITY_EDITOR

using Animancer.TransitionLibraries;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Animancer.Editor.AnimancerGUI;
using static Animancer.Editor.TransitionLibraries.TransitionLibrarySelection;

namespace Animancer.Editor.TransitionLibraries
{
    /// <summary>[Editor-Only]
    /// A <see cref="TransitionLibraryWindowPage"/> for editing transition aliases.
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.TransitionLibraries/TransitionLibraryAliasesPage
    [Serializable]
    public class TransitionLibraryAliasesPage : TransitionLibraryWindowPage
    {
        /************************************************************************************************************************/

        [SerializeField]
        private Vector2 _ScrollPosition;

        [NonSerialized]
        private bool _HasSorted;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override string DisplayName
            => "Transition Aliases";

        /// <inheritdoc/>
        public override string HelpTooltip
            => "Aliases are custom names which can be used to refer to transitions instead of direct references.";

        /// <inheritdoc/>
        public override int Index
            => 2;

        /************************************************************************************************************************/

        private static readonly List<Rect>
            TransitionAreas = new();

        private static float ButtonWidth
            => LineHeight * 4;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnGUI(Rect area)
        {
            var definition = Window.Data;

            if (!_HasSorted)
            {
                _HasSorted = true;
                definition.SortAliases();
            }

            var currentEvent = Event.current;
            var isRepaint = currentEvent.type == EventType.Repaint;
            if (isRepaint)
                TransitionAreas.Clear();

            area.yMin += StandardSpacing;
            area.xMin += StandardSpacing;
            area.xMax -= StandardSpacing;

            var items = Window.Items;
            var aliases = definition.Aliases;

            var viewArea = new Rect(
                0,
                0,
                area.width,
                CalculateHeight(1 + items.Count + aliases.Length) + StandardSpacing);

            if (viewArea.height > area.height)
                viewArea.width -= GUI.skin.verticalScrollbar.fixedWidth;

            _ScrollPosition = GUI.BeginScrollView(area, _ScrollPosition, viewArea);

            viewArea.height = LineHeight;

            DoAliasAllGUI(viewArea);

            NextVerticalArea(ref viewArea);

            for (int i = 0; i < items.Count; i++)
            {
                if (isRepaint)
                    TransitionAreas.Add(viewArea);

                DoItemGUI(ref viewArea, i, currentEvent);
            }

            GUI.EndScrollView();
        }

        /************************************************************************************************************************/

        private void DoItemGUI(
            ref Rect area,
            int itemIndex,
            Event currentEvent)
        {
            var totalTransitionArea = area;
            var items = Window.Items;

            var item = items.GetItem(itemIndex);
            if (item is TransitionAssetBase transition)
            {
                var hasGroup = items.GetGroup(itemIndex) != null;
                if (hasGroup)
                    area.xMin += IndentSize;

                var transitions = Window.Data.Transitions;
                var transitionIndex = Window.Items.ItemToSourceIndex(itemIndex);

                DoTransitionGUI(area, transition, itemIndex, transitionIndex);

                NextVerticalArea(ref area);

                DoAliasGUI(ref area, transitionIndex);

                if (hasGroup)
                    area.xMin -= IndentSize;
            }
            else if (item is TransitionGroup group)
            {
                var groupArea = area;
                NextVerticalArea(ref area);

                var foldoutArea = StealFromLeft(ref groupArea, LineHeight, StandardSpacing);

                TransitionModifierTableGUI.HandleTransitionLabelInput(
                    ref groupArea,
                    Window,
                    group,
                    itemIndex,
                    SelectionType.Group,
                    CalculateTarget);

                GUI.Label(groupArea, group.Name);

                EditorGUI.BeginChangeCheck();

                group.IsExpanded = EditorGUI.Foldout(foldoutArea, group.IsExpanded, GUIContent.none);

                if (EditorGUI.EndChangeCheck())
                    Window.Selection.Select(Window, group, group.Index, SelectionType.Group);
            }

            // Highlights.

            totalTransitionArea.yMax = area.yMin - StandardSpacing;

            var selected = Window.Selection.Selected == item;
            var hover = totalTransitionArea.Contains(currentEvent.mousePosition);

            Window.Highlighter.DrawHighlightGUI(totalTransitionArea, selected, hover);
        }

        /************************************************************************************************************************/

        /// <summary>Draws <see cref="TransitionLibraryDefinition.AliasAllTransitions"/>.</summary>
        private void DoAliasAllGUI(Rect area)
        {
            var definition = Window.Data;

            using (var label = PooledGUIContent.Acquire(
                "Alias All Transitions",
                TransitionLibraryDefinition.AliasAllTransitionsTooltip))
                definition.AliasAllTransitions = EditorGUI.Toggle(area, label, definition.AliasAllTransitions);

            if (TryUseClickEvent(area, 0))
                definition.AliasAllTransitions = !definition.AliasAllTransitions;
        }

        /************************************************************************************************************************/

        /// <summary>Draws a `transition`.</summary>
        private void DoTransitionGUI(Rect area, TransitionAssetBase transition, int itemIndex, int transitionIndex)
        {
            var addArea = StealFromLeft(ref area, ButtonWidth, StandardSpacing);

            TransitionModifierTableGUI.HandleTransitionLabelInput(
                ref area,
                Window,
                transition,
                itemIndex,
                SelectionType.ToTransition,
                CalculateTarget);

            var typeArea = StealFromRight(ref area, area.width * 0.5f, StandardSpacing);

            var label = transition.GetCachedName();
            GUI.Label(area, label);

            var wrappedTransition = transition.GetTransition();
            var type = wrappedTransition != null
                ? wrappedTransition.GetType().GetNameCS(false)
                : "Null";
            GUI.Label(typeArea, type);

            if (GUI.Button(addArea, "Add"))
            {
                var alias = new NamedIndex(null, transitionIndex);
                Window.RecordUndo().AddAlias(alias);
            }
        }

        /************************************************************************************************************************/

        /// <summary>Calculates the target index for a drag and drop operation.</summary>
        private static ListTargetCalculation CalculateTarget(
            Rect area,
            int index,
            Event currentEvent)
        {
            var y = currentEvent.mousePosition.y;
            for (int i = 0; i < TransitionAreas.Count; i++)
            {
                area = TransitionAreas[i];
                var yMax = area.yMax;
                if (y > yMax)
                    continue;

                return new(
                    i,
                    Mathf.InverseLerp(area.y, yMax, y));
            }

            return new(TransitionAreas.Count, 1);
        }

        /************************************************************************************************************************/

        /// <summary>Draws all aliases for the specified `transitionIndex`.</summary>
        private void DoAliasGUI(ref Rect area, int transitionIndex)
        {
            var aliases = Window.Data.Aliases;
            for (int i = 0; i < aliases.Length; i++)
            {
                var alias = aliases[i];

                if (alias.Index != transitionIndex)
                    continue;

                DoAliasGUI(area, alias, i);

                NextVerticalArea(ref area);
            }
        }

        /// <summary>Draws an `alias`.</summary>
        private void DoAliasGUI(Rect area, NamedIndex alias, int aliasIndex)
        {
            var removeArea = StealFromLeft(ref area, ButtonWidth, StandardSpacing);

            EditorGUI.BeginChangeCheck();

            var name = StringAssetDrawer.DrawGUI(area, GUIContent.none, alias.Name, Window.SourceObject, out _);

            if (EditorGUI.EndChangeCheck())
            {
                Window.RecordUndo().Aliases[aliasIndex] = alias.With(name as StringAsset);
            }

            if (GUI.Button(removeArea, "Remove"))
            {
                Window.RecordUndo().RemoveAlias(aliasIndex);
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

