// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using static Animancer.Editor.AnimancerGUI;
using static Animancer.Editor.TransitionDrawer;
using static Animancer.Editor.TransitionLibraries.TransitionLibrarySelection;
using Object = UnityEngine.Object;

namespace Animancer.Editor.TransitionLibraries
{
    /// <summary>[Editor-Only]
    /// A <see cref="TableGUI"/> for editing
    /// <see cref="Animancer.TransitionLibraries.TransitionLibraryDefinition.Modifiers"/>.
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.TransitionLibraries/TransitionModifierTableGUI
    [Serializable]
    public class TransitionModifierTableGUI : TableGUI
    {
        /************************************************************************************************************************/

        [NonSerialized] private TransitionLibraryWindow _Window;
        [NonSerialized] private Vector2Int _SelectedCell;

        /// <summary>The page displaying this table.</summary>
        [NonSerialized] public TransitionLibraryModifiersPage Page;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="TransitionModifierTableGUI"/>.</summary>
        public TransitionModifierTableGUI()
        {
            base.DoCellGUI = DoCellGUI;
            CalculateWidestLabel = CalculateWidestTransitionLabel;
            MinCellSize = new(LineHeight * 2, LineHeight);
            MaxCellSize = new(LineHeight * 4, LineHeight);
        }

        /************************************************************************************************************************/

        /// <summary>Draws the table GUI.</summary>
        public void DoGUI(
            Rect area,
            TransitionLibraryWindow window)
        {
            _Window = window;
            _SelectedCell = RecalculateSelectedCell(window.Selection);

            DoTableGUI(area, window.Items.Count, window.Items.Count);
        }

        /************************************************************************************************************************/

        /// <summary>Calculates the table coordinates of the `selection`.</summary>
        private Vector2Int RecalculateSelectedCell(TransitionLibrarySelection selection)
        {
            if (selection.Validate())
            {
                var cell = new Vector2Int(
                    selection.ToIndex,
                    selection.FromIndex);

                switch (selection.Type)
                {
                    case SelectionType.FromTransition:
                    case SelectionType.ToTransition:
                    case SelectionType.Modifier:
                        cell.x = _Window.Items.TransitionToItemIndex(cell.x);
                        cell.y = _Window.Items.TransitionToItemIndex(cell.y);
                        return cell;

                    case SelectionType.Group:
                        cell.x = _Window.Items.GroupToItemIndex(cell.x);
                        cell.y = _Window.Items.GroupToItemIndex(cell.y);
                        return cell;
                }
            }

            return new(int.MinValue, int.MinValue);
        }

        /************************************************************************************************************************/

        /// <summary>Draws a table cell.</summary>
        private new void DoCellGUI(Rect area, int column, int row)
        {
            var invertHover = false;

            if (column < 0)
            {
                if (row < 0)
                    DoCornerGUI(area);
                else
                    DoLabelGUI(
                        area,
                        row,
                        RightLabelStyle,
                        SelectionType.FromTransition);
            }
            else if (row < 0)
            {
                DoLabelGUI(
                    area,
                    column,
                    EditorStyles.label,
                    SelectionType.ToTransition);

                invertHover = true;
            }
            else
            {
                DoCellBodyGUI(area, _Window, row, column);

            }

            DrawHighlightGUI(area, column, row, invertHover);
        }

        /************************************************************************************************************************/

        /// <summary>Draws the header corner.</summary>
        private void DoCornerGUI(Rect area)
        {
            area.xMin += StandardSpacing;

            var fromArea = area;
            fromArea.y += area.height - LineHeight;
            fromArea.height = LineHeight;

            var toArea = fromArea;
            toArea.y -= toArea.height - Padding;

            var deleteTransitionArea = toArea;
            deleteTransitionArea.y -= deleteTransitionArea.height - Padding;

            var createTransitionArea = deleteTransitionArea;
            createTransitionArea.y -= createTransitionArea.height - Padding;

            var createGroupArea = createTransitionArea;
            createGroupArea.y -= createGroupArea.height - Padding;

            fromArea.width -= VerticalScrollBar.fixedWidth + Padding;

            var style = RightLabelStyle;
            var fontStyle = style.fontStyle;
            style.fontStyle = FontStyle.Bold;

            GUI.Label(fromArea, "From", style);
            GUI.Label(toArea, "To", style);

            style.fontStyle = fontStyle;

            DoCreateGroupButtonGUI(createGroupArea);
            DoCreateTransitionButtonGUI(createTransitionArea);
            DoRemoveButtonGUI(deleteTransitionArea);
        }

        /************************************************************************************************************************/

        /// <summary>Draws a button to create a new group.</summary>
        private void DoCreateGroupButtonGUI(Rect area)
        {
            if (GUI.Button(area, "Create Group"))
            {
                TransitionLibraryOperations.CreateGroup(_Window, _Window.EditorData);
                GUIUtility.ExitGUI();
            }
        }

        /************************************************************************************************************************/

        /// <summary>Draws a button to create a new transition.</summary>
        private void DoCreateTransitionButtonGUI(Rect area)
        {
            if (GUI.Button(area, "Create Transition"))
            {
                TransitionLibraryOperations.CreateTransition(_Window);
                GUIUtility.ExitGUI();
            }
        }

        /************************************************************************************************************************/

        /// <summary>Draws a button to remove the selected object.</summary>
        private void DoRemoveButtonGUI(Rect area)
        {
            var enabled = GUI.enabled;

            var selection = _Window.Selection;
            string label = "Remove Selection";
            switch (selection.Type)
            {
                case SelectionType.FromTransition:
                    GUI.enabled = selection.FromIndex >= 0 && selection.FromIndex < _Window.Data.Transitions.Length;
                    label = "Remove Transition";
                    break;

                case SelectionType.ToTransition:
                    GUI.enabled = selection.ToIndex >= 0 && selection.ToIndex < _Window.Data.Transitions.Length;
                    label = "Remove Transition";
                    break;

                case SelectionType.Modifier:
                    label = "Remove Modifier";
                    break;

                case SelectionType.Group:
                    label = "Remove Group";
                    break;

                default:
                    GUI.enabled = false;
                    break;
            }

            using var content = PooledGUIContent.Acquire(label,
                "Remove the selected object from this library. [Delete]");

            if (GUI.Button(area, content))
            {
                TransitionLibraryOperations.HandleDelete(_Window);
                Deselect();
                GUIUtility.ExitGUI();
            }

            GUI.enabled = enabled;
        }

        /************************************************************************************************************************/

        /// <summary>Draws a row or column label.</summary>
        private void DoLabelGUI(
            Rect area,
            int itemIndex,
            GUIStyle style,
            SelectionType selectionType)
        {
            if (!_Window.Items.TryGet(itemIndex, out var transitionOrGroup))
                return;

            if (transitionOrGroup is TransitionAssetBase transition)
            {
                var group = _Window.Items.GetGroup(itemIndex);
                if (group != null)
                    StealGroupFoldoutSpace(ref area, style);

                HandleTransitionLabelInput(
                    ref area,
                    _Window,
                    transition,
                    itemIndex,
                    selectionType,
                    CalculateTarget);

                GUI.Label(area, GetTransitionName(transition), style);
            }
            else if (transitionOrGroup is TransitionGroup group)
            {
                var foldoutArea = StealGroupFoldoutSpace(ref area, style);

                HandleTransitionLabelInput(
                    ref area,
                    _Window,
                    group,
                    itemIndex,
                    SelectionType.Group,
                    CalculateTarget);

                GUI.Label(area, group.Name, style);

                EditorGUI.BeginChangeCheck();

                group.IsExpanded = EditorGUI.Foldout(foldoutArea, group.IsExpanded, GUIContent.none);

                if (EditorGUI.EndChangeCheck())
                {
                    itemIndex = _Window.Items.ItemToSourceIndex(itemIndex);
                    _Window.Selection.Select(_Window, group, itemIndex, SelectionType.Group);
                }
            }
            else
            {
                group = _Window.Items.GetGroup(itemIndex);
                if (group != null)
                    StealGroupFoldoutSpace(ref area, style);

                HandleTransitionLabelInput(
                    ref area,
                    _Window,
                    transitionOrGroup,
                    itemIndex,
                    selectionType,
                    CalculateTarget);

                GUI.Label(area, MissingTransitionLabel, style);
            }
        }

        /************************************************************************************************************************/

        /// <summary>Calculates the area for a group foldout and subtracts it from the appropriate side of the `area`.</summary>
        private Rect StealGroupFoldoutSpace(ref Rect area, GUIStyle style)
        {
            if (style.alignment == TextAnchor.MiddleRight)
            {
                return StealFromRight(ref area, LineHeight, StandardSpacing);
            }
            else
            {
                var space = StealFromLeft(ref area, LineHeight, StandardSpacing);
                space.x += StandardSpacing;
                return space;
            }
        }

        /************************************************************************************************************************/
        
        /// <summary>The label to use for <c>null</c> transitions.</summary>
        public const string MissingTransitionLabel = "<Missing Transition>";

        /// <summary>Returns the name of the `transition` with a special message for <c>null</c>.</summary>
        public static string GetTransitionName(TransitionAssetBase transition)
            => transition != null
            ? transition.GetCachedName()
            : MissingTransitionLabel;

        /************************************************************************************************************************/

        private static readonly int LabelHint = "Label".GetHashCode();

        [NonSerialized] private static bool _IsLabelDrag;

        /// <summary>Handles input events on transition labels.</summary>
        public static void HandleTransitionLabelInput(
            ref Rect area,
            TransitionLibraryWindow window,
            object item,
            int itemIndex,
            SelectionType selectionType,
            Func<Rect, int, Event, ListTargetCalculation> calculateTarget)
        {
            var control = new GUIControl(area, LabelHint);

            switch (control.EventType)
            {
                case EventType.MouseDown:
                    if (control.Event.button == 0 &&
                        control.TryUseMouseDown())
                    {
                        if (control.Event.clickCount == 2)
                        {
                            if (item is Object unityObject)
                                EditorGUIUtility.PingObject(unityObject);
                        }
                        else
                        {
                            var index = window.Items.ItemToSourceIndex(itemIndex);
                            window.Selection.Select(window, item, index, selectionType);
                        }

                        _IsLabelDrag = false;
                    }

                    break;

                case EventType.MouseUp:
                    if (control.TryUseMouseUp() && _IsLabelDrag)
                    {
                        var target = calculateTarget(area, itemIndex, control.Event);
                        window.OnDropItem(item, itemIndex, target, selectionType);
                    }
                    break;

                case EventType.MouseDrag:
                    if (control.TryUseHotControl())
                        _IsLabelDrag = true;
                    break;
            }

            if (GUIUtility.hotControl == control.ID && _IsLabelDrag)
            {
                RepaintEverything();
                area.y = control.Event.mousePosition.y - area.height * 0.5f;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Calculates the target index for a drag and drop operation.</summary>
        private static ListTargetCalculation CalculateTarget(
            Rect area,
            int itemIndex,
            Event currentEvent)
        {
            var target = new ListTargetCalculation(area.y, area.height, currentEvent.mousePosition.y);
            target.Index += itemIndex;
            return target;
        }

        /************************************************************************************************************************/

        private static readonly int GroupHint = "Group".GetHashCode();

        /// <summary>
        /// Calls <see cref="DoModifierValueGUI"/> if the specified cell
        /// contains a transition combination rather than a group.
        /// </summary>
        public void DoCellBodyGUI(
            Rect area,
            TransitionLibraryWindow window,
            int from,
            int to)
        {
            // For transition intersections, show the modifier value.
            if (window.Items.TryGet(from, out var fromItem) &&
                fromItem is not TransitionGroup &&
                window.Items.TryGet(to, out var toItem) &&
                toItem is not TransitionGroup)
            {
                from = window.Items.ItemToSourceIndex(from);
                to = window.Items.ItemToSourceIndex(to);

                DoModifierValueGUI(area, _Window, Page, from, to, "", true);
            }
            else // For group intersections, allow clicking to select the group.
            {
                var group = fromItem;
                var groupIndex = from;
                if (group is not TransitionGroup)
                {
                    window.Items.TryGet(to, out group);
                    groupIndex = to;
                }

                if (group is TransitionGroup)
                {
                    var control = new GUIControl(area, LabelHint);
                    if (control.Event.type == EventType.MouseDown &&
                        control.Event.button == 0 &&
                        control.TryUseMouseDown())
                    {
                        groupIndex = _Window.Items.ItemToSourceIndex(groupIndex);
                        _Window.Selection.Select(_Window, group, groupIndex, SelectionType.Group);
                    }
                }

                return;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Draws the fade duration for a particular transition combination.</summary>
        public static void DoModifierValueGUI(
            Rect area,
            TransitionLibraryWindow window,
            TransitionLibraryModifiersPage page,
            int from,
            int to,
            string label,
            bool singleField)
        {
            var previousHotControl = GUIUtility.hotControl;

            var hasModifier = window.Data.TryGetModifier(from, to, out var modifier);
            var hasModifierWithValue = hasModifier;
            var value = page.GetValue(modifier);

            window.Data.Transitions.TryGet(to, out var transition);

            if (float.IsNaN(value))
            {
                hasModifierWithValue = false;

                if (transition != null)
                    value = page.GetValue(transition);
            }

            var labelStyle = EditorStyles.label.fontStyle;
            var numberAlignment = EditorStyles.numberField.alignment;
            var numberStyle = EditorStyles.numberField.fontStyle;
            var numberSize = EditorStyles.numberField.fontSize;
            try
            {
                EditorStyles.numberField.alignment = TextAnchor.MiddleLeft;

                if (hasModifierWithValue)
                {
                    EditorStyles.label.fontStyle = FontStyle.Bold;
                    EditorStyles.numberField.fontStyle = FontStyle.Bold;
                }
                else
                {
                    EditorStyles.numberField.fontSize = EditorStyles.numberField.fontSize * 4 / 5;
                }

                EditorGUI.BeginChangeCheck();

                page.ConfigureForSingleField(singleField, ref value);
                if (singleField)
                    transition = null;

                using (new DrawerContext(transition))
                using (var content = PooledGUIContent.Acquire(label))
                    page.TimeDrawer.OnGUI(area, content, ref value);

                if (TryUseClickEvent(area, 2))
                    value = float.NaN;

                if (EditorGUI.EndChangeCheck())
                {
                    if (EditorGUIUtility.editingTextField &&
                        !float.TryParse(CurrentFieldText, out value))
                        value = float.NaN;

                    if (!hasModifier)
                        modifier = modifier.WithDetails(float.NaN, float.NaN);

                    var data = window.RecordUndo();
                    page.SetValue(ref modifier, value);
                    data.SetModifier(modifier);

                    hasModifier = true;

                    RepaintEverything();
                }
            }
            finally
            {
                EditorStyles.label.fontStyle = labelStyle;
                EditorStyles.numberField.alignment = numberAlignment;
                EditorStyles.numberField.fontStyle = numberStyle;
                EditorStyles.numberField.fontSize = numberSize;
            }

            if (previousHotControl != GUIUtility.hotControl)
            {
                window.Selection.Select(
                    window,
                    modifier,
                    modifier.FromIndex,
                    SelectionType.Modifier);
            }
        }

        /************************************************************************************************************************/

        /// <summary>Draws the selection and hover highlights for a particular cell.</summary>
        private void DrawHighlightGUI(Rect area, int column, int row, bool invertHover)
        {
            if (_Window.Highlighter.EventType != EventType.Repaint)
                return;

            var selected =
                _SelectedCell.x == column ||
                _SelectedCell.y == row;

            var hover = false;

            if (_Window.Highlighter.IsMouseOver)
            {
                if (invertHover)
                    (row, column) = (column, row);

                var mousePosition = Event.current.mousePosition;
                if ((column >= 0 && IsInlineWithX(area, mousePosition.x)) ||
                    (row >= 0 && IsInlineWithY(area, mousePosition.y)))
                {
                    hover = true;
                }
            }

            _Window.Highlighter.DrawHighlightGUI(area, selected, hover);
        }

        /************************************************************************************************************************/

        /// <summary>Is `x` inside the `area`.</summary>
        private static bool IsInlineWithX(Rect area, float x)
            => area.xMin <= x
            && area.xMax > x;

        /// <summary>Is `y` inside the `area`.</summary>
        private static bool IsInlineWithY(Rect area, float y)
            => area.yMin <= y
            && area.yMax > y;

        /************************************************************************************************************************/

        /// <summary>Calculates the largest width of all transition labels.</summary>
        private float CalculateWidestTransitionLabel()
        {
            var widest = LineHeight * 2;

            var transitions = _Window.Data.Transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                var transition = transitions[i];
                if (transition == null)
                    continue;

                var label = transition.GetCachedName();
                var width = CalculateLabelWidth(label);
                if (widest < width)
                    widest = width;
            }

            return widest;
        }

        /************************************************************************************************************************/
    }
}

#endif

