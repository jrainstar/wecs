// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

#if UNITY_EDITOR

using Animancer.TransitionLibraries;
using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Animancer.Editor.TransitionLibraries
{
    /// <summary>[Editor-Only]
    /// A custom Inspector for <see cref="TransitionLibrarySelection"/>.
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.TransitionLibraries/TransitionLibrarySelectionEditor
    [CustomEditor(typeof(TransitionLibrarySelection), true)]
    public class TransitionLibrarySelectionEditor : UnityEditor.Editor
    {
        /************************************************************************************************************************/

        /// <summary>Casts the <see cref="UnityEditor.Editor.target"/>.</summary>
        public TransitionLibrarySelection Target
            => target as TransitionLibrarySelection;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            var target = Target;
            if (target == null || !target.Validate())
                return;

            EditorGUI.BeginChangeCheck();

            switch (target.Type)
            {
                case TransitionLibrarySelection.SelectionType.Library:
                    using (new EditorGUI.DisabledScope(true))
                        DoNestedEditorGUI(target.Selected as TransitionLibraryAsset, "Transition Library");
                    break;

                case TransitionLibrarySelection.SelectionType.FromTransition:
                    DoTransitionGUI(target.Selected as TransitionAssetBase, target.FromIndex);
                    break;

                case TransitionLibrarySelection.SelectionType.ToTransition:
                    DoTransitionGUI(target.Selected as TransitionAssetBase, target.ToIndex);
                    break;

                case TransitionLibrarySelection.SelectionType.Modifier:
                    DoModifierGUI(target, (TransitionModifierDefinition)target.Selected);
                    break;

                case TransitionLibrarySelection.SelectionType.Group:
                    DoGroupGUI(target, (TransitionGroup)target.Selected);
                    break;

                default:
                    target.Deselect();
                    break;
            }

            if (EditorGUI.EndChangeCheck() &&
                target != null &&
                target.Window != null)
                target.Window.Repaint();
        }

        /************************************************************************************************************************/
        #region Nested Editor
        /************************************************************************************************************************/

        [NonSerialized] private readonly CachedEditor NestedEditor = new();
        [NonSerialized] private readonly CachedEditor NestedEditor2 = new();

        /************************************************************************************************************************/

        /// <summary>Draws the <see cref="UnityEditor.Editor"/> for the `target`.</summary>
        private T DoNestedEditorGUI<T>(T target, string referenceLabel)
            where T : Object
        {
            target = AnimancerGUI.DoObjectFieldGUI(referenceLabel, target, false);

            var editor = NestedEditor.GetEditor(target);
            if (editor != null)
                editor.OnInspectorGUI();

            return target;
        }

        /************************************************************************************************************************/

        /// <summary>Cleans up any nested editors.</summary>
        protected virtual void OnDestroy()
        {
            NestedEditor.Dispose();
            NestedEditor2.Dispose();
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Transitions
        /************************************************************************************************************************/

        /// <summary>Draws the GUI for the `transition`.</summary>
        private void DoTransitionGUI(
            TransitionAssetBase transition,
            int transitionIndex)
        {
            DoTransitionNameGUI(transition);

            EditorGUI.BeginChangeCheck();

            var newTransition = DoNestedEditorGUI(transition, "Transition Asset");

            if (EditorGUI.EndChangeCheck())
                SetTransition(transition, newTransition, transitionIndex);

            if (transition == null && GUILayout.Button("Remove"))
            {
                Target.Deselect();
                Target.Window.RecordUndo().RemoveTransition(transitionIndex);
            }
        }

        /************************************************************************************************************************/

        /// <summary>Replaces or removes the specified transition.</summary>
        private void SetTransition(
            TransitionAssetBase oldTransition,
            TransitionAssetBase newTransition,
            int transitionIndex)
        {
            var library = Target.Window.RecordUndo();
            if (newTransition == null)
            {
                TransitionLibraryOperations.AskHowToDeleteTransition(
                    oldTransition,
                    transitionIndex,
                    Target.Window);
            }
            else
            {
                library.Transitions[transitionIndex] = newTransition;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Draws a field for editing the name of the `transition`.</summary>
        private void DoTransitionNameGUI(
            TransitionAssetBase transition)
        {
            if (transition == null)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.DelayedTextField(
                        "Name",
                        TransitionModifierTableGUI.MissingTransitionLabel);
                return;
            }

            var isSubAsset = AssetDatabase.IsSubAsset(transition);
            var isMainAsset = !isSubAsset && AssetDatabase.IsMainAsset(transition);
            var label = isSubAsset
                ? "Sub-Asset Name"
                : isMainAsset
                ? "File Name"
                : "Name";

            EditorGUI.BeginChangeCheck();

            var name = TransitionModifierTableGUI.GetTransitionName(transition);
            name = EditorGUILayout.DelayedTextField(label, name);

            if (EditorGUI.EndChangeCheck() && transition != null)
            {
                transition.SetName(name);

                if (isSubAsset)
                {
                    AssetDatabase.SaveAssets();
                }
                else if (isMainAsset)
                {
                    AssetDatabase.RenameAsset(
                        AssetDatabase.GetAssetPath(transition),
                        name);
                }
            }
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Modifiers
        /************************************************************************************************************************/

        private static readonly BoolPref
            IsFromExpanded = new($"{nameof(TransitionLibrarySelectionEditor)}.{nameof(IsFromExpanded)}"),
            IsToExpanded = new($"{nameof(TransitionLibrarySelectionEditor)}.{nameof(IsToExpanded)}");

        /************************************************************************************************************************/

        /// <summary>Draws the GUI for the `modifier`.</summary>
        private void DoModifierGUI(
            TransitionLibrarySelection selection,
            TransitionModifierDefinition modifier)
        {
            var library = selection.Window.Data;
            DoTransitionField(library, NestedEditor, IsFromExpanded, modifier.FromIndex, "From");
            DoTransitionField(library, NestedEditor2, IsToExpanded, modifier.ToIndex, "To");

            if (selection.Window.TryGetPage<TransitionLibraryFadeDurationsPage>(out var fadeDurations))
            {
                var area = AnimancerGUI.LayoutSingleLineRect();
                TransitionModifierTableGUI.DoModifierValueGUI(
                    area,
                    selection.Window,
                    fadeDurations,
                    modifier.FromIndex,
                    modifier.ToIndex,
                    "Fade Duration",
                    false);
            }

            if (selection.Window.TryGetPage<TransitionLibraryStartTimesPage>(out var startTimes))
            {
                var area = AnimancerGUI.LayoutSingleLineRect();
                TransitionModifierTableGUI.DoModifierValueGUI(
                    area,
                    selection.Window,
                    startTimes,
                    modifier.FromIndex,
                    modifier.ToIndex,
                    "Start Time",
                    false);
            }
        }

        /************************************************************************************************************************/

        /// <summary>Draws the GUI for a transition.</summary>
        private TransitionAssetBase DoTransitionField(
            TransitionLibraryDefinition library,
            CachedEditor cachedEditor,
            BoolPref isExpanded,
            int transitionIndex,
            string label)
        {
            library.TryGetTransition(transitionIndex, out var transition);

            var area = AnimancerGUI.LayoutSingleLineRect(AnimancerGUI.SpacingMode.After);
            var labelArea = area;
            labelArea.width = EditorGUIUtility.labelWidth;

            isExpanded.Value = EditorGUI.Foldout(labelArea, isExpanded, GUIContent.none, true);

            EditorGUI.BeginChangeCheck();

            var newTransition = AnimancerGUI.DoObjectFieldGUI(area, label, transition, false);

            if (EditorGUI.EndChangeCheck())
                SetTransition(transition, newTransition, transitionIndex);

            if (isExpanded)
            {
                GUILayout.BeginVertical(GUI.skin.box);

                var editor = cachedEditor.GetEditor(transition);
                editor.OnInspectorGUI();

                GUILayout.EndVertical();
            }

            return transition;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
        #region Groups
        /************************************************************************************************************************/

        /// <summary>Draws the GUI for the `group`.</summary>
        private void DoGroupGUI(
            TransitionLibrarySelection selection,
            TransitionGroup group)
        {
            group.Name = EditorGUILayout.TextField("Group Name", group.Name);

            var enabled = GUI.enabled;
            GUI.enabled = false;

            EditorGUILayout.LabelField("Transition Count", group.Count.ToStringCached());

            var transitions = selection.Window.Data.Transitions;
            for (int i = 0; i < group.Count; i++)
            {
                var label = $"Transition {i.ToStringCached()}";
                transitions.TryGet(group.Index + i, out var transition);
                EditorGUILayout.ObjectField(
                    label,
                    transition,
                    typeof(TransitionAssetBase),
                    false);
            }

            GUI.enabled = enabled;
        }

        /************************************************************************************************************************/
        #endregion
        /************************************************************************************************************************/
    }
}

#endif

