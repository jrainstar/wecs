// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

#if UNITY_EDITOR

using Animancer.TransitionLibraries;
using UnityEditor;
using UnityEngine;
using static Animancer.Editor.AnimancerGUI;

namespace Animancer.Editor.TransitionLibraries
{
    /// <summary>[Editor-Only] A custom Inspector for <see cref="TransitionLibraryAsset"/> fields.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.TransitionLibraries/TransitionLibraryDrawer
    [CustomPropertyDrawer(typeof(TransitionLibraryAsset), true)]
    public class TransitionLibraryDrawer : PropertyDrawer
    {
        /************************************************************************************************************************/

        private const string EditLabel = "Edit";

        private static float _EditButtonWidth;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => LineHeight;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnGUI(Rect area, SerializedProperty property, GUIContent label)
        {
            var library = property.objectReferenceValue as TransitionLibraryAsset;
            if (library != null)
            {
                var style = EditorStyles.miniButton;

                if (_EditButtonWidth <= 0)
                    _EditButtonWidth = style.CalculateWidth(EditLabel);

                var editArea = StealFromRight(ref area, _EditButtonWidth, StandardSpacing);

                if (GUI.Button(editArea, EditLabel, style))
                    TransitionLibraryWindow.Open(library);
            }

            EditorGUI.PropertyField(area, property, label);
        }

        /************************************************************************************************************************/
    }
}

#endif

