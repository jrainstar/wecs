// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Animancer.Editor.TransitionLibraries
{
    /// <summary>[Editor-Only] Custom Inspector for <see cref="TransitionLibraryEditorDataAsset"/>.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.TransitionLibraries/TransitionLibraryEditorDataEditor
    [CustomEditor(typeof(TransitionLibraryEditorDataAsset), true)]
    public class TransitionLibraryEditorDataEditor : UnityEditor.Editor
    {
        /************************************************************************************************************************/

        private SerializedProperty _Library;
        private SerializedProperty _Sort;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected virtual void OnEnable()
        {
            _Library = serializedObject.FindProperty(TransitionLibraryEditorDataAsset.LibraryFieldName);
            var data = serializedObject.FindProperty(TransitionLibraryEditorDataAsset.DataFieldName);
            _Sort = data.FindPropertyRelative(TransitionLibraryEditorDataInternal.TransitionSortModeFieldName);
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            var target = this.target as TransitionLibraryEditorDataAsset;
            if (target == null)
                return;

            if (_Library != null)
            {
                var enabled = GUI.enabled;
                if (_Library.objectReferenceValue != null)
                    GUI.enabled = false;

                EditorGUILayout.PropertyField(_Library);

                GUI.enabled = enabled;
            }

            if (_Sort != null)
            {
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(_Sort);

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    TransitionLibrarySort.Sort(target.Library);
                }
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

