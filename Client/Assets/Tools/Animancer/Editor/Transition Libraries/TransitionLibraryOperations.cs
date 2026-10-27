// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

#if UNITY_EDITOR

using Animancer.TransitionLibraries;
using System;
using UnityEditor;
using UnityEngine;
using static Animancer.Editor.AnimancerGUI;
using static Animancer.Editor.TransitionLibraries.TransitionLibrarySelection;

namespace Animancer.Editor.TransitionLibraries
{
    /// <summary>[Editor-Only]
    /// Operations for modifying a <see cref="TransitionLibraryAsset"/>.
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.TransitionLibraries/TransitionLibraryOperations
    public static class TransitionLibraryOperations
    {
        /************************************************************************************************************************/

        /// <summary>Handles input events for the background of the `window`.</summary>
        public static void HandleBackgroundInput(
            Rect area,
            TransitionLibraryWindow window)
        {
            // Click to select the library.
            if (TryUseClickEvent(area, 0))
                window.Selection.Select(window, window.SourceObject, -1, SelectionType.Library);

            var currentEvent = Event.current;
            switch (currentEvent.type)
            {
                // Drag and drop to add transition.
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    HandleDragAndDrop(currentEvent, window);
                    break;

                case EventType.ValidateCommand:
                case EventType.ExecuteCommand:
                    switch (currentEvent.commandName)
                    {
                        // Delete to remove the selection.
                        case Commands.Delete:
                        case Commands.SoftDelete:
                            HandleDelete(currentEvent, window);
                            break;
                    }
                    break;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Handles drag and drop events to add transitions to the `window`.</summary>
        private static void HandleDragAndDrop(
            Event currentEvent,
            TransitionLibraryWindow window)
        {
            var dragging = DragAndDrop.objectReferences;

            TransitionAssetBase dropped = null;
            int index = -1;

            for (int i = dragging.Length - 1; i >= 0; i--)
            {
                var transition = TryCreateTransitionAttribute.TryCreateTransitionAsset(dragging[i]);
                if (transition != null &&
                    Array.IndexOf(window.Data.Transitions, transition) >= 0)
                    continue;

                switch (currentEvent.type)
                {
                    case EventType.DragUpdated:
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                        currentEvent.Use();
                        return;

                    case EventType.DragPerform:
                        dropped = transition;
                        index = window.Data.Transitions.Length;
                        window.RecordUndo().AddTransition(transition);
                        break;
                }
            }

            if (dropped != null)
            {
                window.Selection.Select(
                    window,
                    dropped,
                    index,
                    SelectionType.ToTransition);
                DragAndDrop.AcceptDrag();
                currentEvent.Use();
            }
        }

        /************************************************************************************************************************/

        /// <summary>Creates a new group in the <see cref="TransitionLibraryEditorDataInternal.TransitionGroups"/>.</summary>
        public static TransitionGroup CreateGroup(
            TransitionLibraryWindow window,
            TransitionLibraryEditorDataInternal data)
        {
            window.Repaint();

            var groups = data.TransitionGroups;

            var group = new TransitionGroup
            {
                Name = $"Transition Group {groups.Count}",
                Index = window.Data.Transitions.Length,
            };

            groups.Add(group);

            window.Selection.Select(window, group, groups.Count - 1, SelectionType.Group);

            return group;
        }

        /************************************************************************************************************************/

        /// <summary>Creates a new transition as a sub-asset of the `window`'s library.</summary>
        public static TransitionAssetBase CreateTransition(
            TransitionLibraryWindow window)
        {
            var createInstance = TransitionAssetBase.CreateInstance;
            if (createInstance == null)
            {
                Debug.LogError(
                    $"{nameof(CreateTransition)} failed because " +
                    $"{nameof(TransitionAssetBase)}.{nameof(TransitionAssetBase.CreateInstance)}" +
                    $" hasn't been assigned." +
                    $" It should be automatically initialized by TransitionAsset.");
                return null;
            }

            var definition = window.RecordUndo();

            var transition = createInstance(null);
            transition.name = "Transition " + (definition.Transitions.Length + 1);
            AnimancerReflection.TryInvoke(transition, "Reset");

            definition.AddTransition(transition);

            var index = definition.Transitions.Length;
            window.Selection.Select(window, transition, index, SelectionType.ToTransition);

            return transition;
        }

        /************************************************************************************************************************/

        /// <summary>Handles a delete event.</summary>
        private static void HandleDelete(
            Event currentEvent,
            TransitionLibraryWindow window)
        {
            if (currentEvent.type == EventType.ExecuteCommand)
                HandleDelete(window);

            currentEvent.Use();
        }

        /// <summary>Handles a delete event.</summary>
        public static void HandleDelete(
            TransitionLibraryWindow window)
        {
            if (!window.Selection.Validate())
                return;

            switch (window.Selection.Type)
            {
                case SelectionType.FromTransition:
                    if (window.Selection.Selected is TransitionAssetBase fromTransition)
                        AskHowToDeleteTransition(
                            fromTransition,
                            window.Selection.FromIndex,
                            window);
                    break;

                case SelectionType.ToTransition:
                    if (window.Selection.Selected is TransitionAssetBase toTransition)
                        AskHowToDeleteTransition(
                            toTransition,
                            window.Selection.ToIndex,
                            window);
                    break;

                case SelectionType.Modifier:
                    if (window.Selection.Selected is TransitionModifierDefinition modifier)
                    {
                        window.Selection.Deselect();
                        window.RecordUndo().RemoveModifier(modifier);
                    }
                    break;

                case SelectionType.Group:
                    if (window.Selection.Selected is TransitionGroup group)
                    {
                        window.Selection.Deselect();
                        window.RecordUndo();
                        window.EditorData.TransitionGroups.Remove(group);
                    }
                    break;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Asks if the user wants to delete a transition asset or just remove it from the library.</summary>
        public static void AskHowToDeleteTransition(
            TransitionAssetBase transition,
            int index,
            TransitionLibraryWindow window)
        {
            var assetPath = AssetDatabase.GetAssetPath(transition);
            if (string.IsNullOrEmpty(assetPath))
            {
                if (transition != null)
                    Undo.DestroyObjectImmediate(transition);

                window.RecordUndo().RemoveTransition(index);
                return;
            }

            var isMainAsset = AssetDatabase.IsMainAsset(transition);
            var assetType = isMainAsset ? "Asset" : "Sub-Asset";
            var isSubAssetOfLibrary =
                !isMainAsset &&
                assetPath == AssetDatabase.GetAssetPath(window.SourceObject);

            var message = assetPath;
            if (!isSubAssetOfLibrary)
                message += "\n\nRemove Transition: removes it from this Transition Library.";

            message += $"\n\nDelete {assetType}: deletes the Transition {assetType} from your project (cannot be undone).";

            int choice;
            if (isSubAssetOfLibrary)
            {
                if (EditorUtility.DisplayDialog(
                    "Delete transition?",
                    message,
                    "Delete " + assetType,
                    "Cancel"))
                    choice = 2;
                else
                    return;
            }
            else
            {
                choice = EditorUtility.DisplayDialogComplex(
                    "Remove or Delete transition?",
                    message,
                    "Remove Transition",
                    "Cancel",
                    "Delete " + assetType);
            }

            switch (choice)
            {
                case 0:// Remove.
                    window.Selection.Deselect();
                    window.RecordUndo().RemoveTransition(index);
                    break;

                case 2:// Delete.
                    window.Selection.Deselect();
                    if (isMainAsset)
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                    }
                    else
                    {
                        AnimancerEditorUtilities.DeleteSubAsset(transition);
                    }

                    window.Data.RemoveTransition(index);
                    Undo.ClearUndo(window);

                    break;

                default:
                    return;
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

