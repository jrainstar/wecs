// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

#if UNITY_EDITOR

using Animancer.TransitionLibraries;

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Animancer.Editor.TransitionLibraries
{
    /// <summary>[Editor-Only]
    /// Additional data for a <see cref="TransitionLibraryAsset"/> which is excluded from Runtime Builds.
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.TransitionLibraries/TransitionLibraryEditorDataAsset
    [AnimancerHelpUrl(typeof(TransitionLibraryEditorDataAsset))]
    public partial class TransitionLibraryEditorDataAsset : ScriptableObject // TransitionLibraryEditorDataAsset.cs
    {
        /************************************************************************************************************************/

        /// <summary>Libraries mapped to their editor data.</summary>
        /// <remarks>
        /// Libraries can't have a direct reference to this class
        /// because it's in the Editor assembly which the Runtime assembly doesn't reference.
        /// </remarks>
        private static readonly Dictionary<TransitionLibraryAsset, TransitionLibraryEditorDataAsset>
            LibraryToEditorData = new();

        /************************************************************************************************************************/

        /// <summary>The name of the serialized backing field of <see cref="Library"/>.</summary>
        internal const string LibraryFieldName = nameof(_Library);

        [SerializeField]
        private TransitionLibraryAsset _Library;

        /// <summary>The library this data is associated with.</summary>
        public TransitionLibraryAsset Library
            => _Library;

        /************************************************************************************************************************/

        /// <summary>The name of the serialized backing field of <see cref="Data"/>.</summary>
        internal const string DataFieldName = nameof(_Data);

        [SerializeField]
        private TransitionLibraryEditorDataInternal _Data;

        /// <summary>[<see cref="SerializeField"/>] The data contained in this asset.</summary>
        public TransitionLibraryEditorDataInternal Data
        {
            get => _Data ??= new();
            set
            {
                SetLibrary(this, _Library);
                _Data = value;
                EditorUtility.SetDirty(this);
            }
        }

        /************************************************************************************************************************/

        /// <summary>Registers this data for the <see cref="Library"/>.</summary>
        protected virtual void OnEnable()
        {
            if (_Library != null)
                LibraryToEditorData[_Library] = this;
        }

        /// <summary>Un-registers this data for the <see cref="Library"/>.</summary>
        protected virtual void OnDisable()
        {
            if (_Library != null)
                LibraryToEditorData.Remove(_Library);
        }

        /************************************************************************************************************************/

        /// <summary>Sets the <see cref="Library"/>.</summary>
        public static void SetLibrary(TransitionLibraryEditorDataAsset data, TransitionLibraryAsset library)
        {
            if (library != null)
                LibraryToEditorData.Remove(library);

            data._Library = library;

            if (library != null)
                LibraryToEditorData.Add(library, data);
        }

        /************************************************************************************************************************/

        /// <summary>Tries to get the `data` associated with the `library`.</summary>
        private static bool TryGet(
            TransitionLibraryAsset library,
            out TransitionLibraryEditorDataAsset asset)
        {
            if (!LibraryToEditorData.TryGetValue(library, out asset))
                return false;

            if (asset != null)
            {
                SetLibrary(asset, library);
                return true;
            }

            LibraryToEditorData.Remove(library);
            return false;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the <see cref="TransitionLibraryEditorDataInternal"/> sub-asset of the `library` if one exists.
        /// </summary>
        public static TransitionLibraryEditorDataAsset GetEditorData(TransitionLibraryAsset library)
        {
            if (TryGet(library, out var asset))
                return asset;

            var assetPath = AssetDatabase.GetAssetPath(library);
            if (string.IsNullOrEmpty(assetPath))
                return null;

            var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            for (int i = 0; i < subAssets.Length; i++)
            {
                if (subAssets[i] is TransitionLibraryEditorDataAsset editorData)
                {
                    asset = editorData;
                    SetLibrary(asset, library);
                    return asset;
                }
            }

            return null;
        }

        /************************************************************************************************************************/

        /// <summary>
        /// Returns the <see cref="TransitionLibraryEditorDataAsset"/> sub-asset of the `library` if one exists.
        /// Otherwise, creates and saves a new one.
        /// </summary>
        public static TransitionLibraryEditorDataAsset GetOrCreateEditorData(TransitionLibraryAsset library)
        {
            var data = library.GetEditorData();
            if (data != null)
                return data;

            data = CreateInstance<TransitionLibraryEditorDataAsset>();
            data.name = "Editor Data";
            data.hideFlags = HideFlags.DontSaveInBuild | HideFlags.HideInHierarchy;

            SetLibrary(data, library);

            EditorApplication.CallbackFunction addSubAsset = null;

            addSubAsset = () =>
            {
                if (AssetDatabase.Contains(library))
                {
                    EditorApplication.update -= addSubAsset;

                    AssetDatabase.AddObjectToAsset(data, library);
                    AssetDatabase.SaveAssets();
                }
            };

            EditorApplication.update += addSubAsset;

            return data;
        }

        /************************************************************************************************************************/
    }

    /// <summary>[Editor-Only] Extension methods for <see cref="TransitionLibraryEditorDataAsset"/>.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.TransitionLibraries/TransitionLibraryEditorDataExtensions
    public static class TransitionLibraryEditorDataExtensions
    {
        /************************************************************************************************************************/

        /// <summary><see cref="TransitionLibraryEditorDataAsset.GetEditorData"/></summary>
        public static TransitionLibraryEditorDataAsset GetEditorData(this TransitionLibraryAsset library)
            => TransitionLibraryEditorDataAsset.GetEditorData(library);

        /// <summary><see cref="TransitionLibraryEditorDataAsset.GetOrCreateEditorData"/></summary>
        public static TransitionLibraryEditorDataAsset GetOrCreateEditorData(this TransitionLibraryAsset library)
            => TransitionLibraryEditorDataAsset.GetOrCreateEditorData(library);

        /************************************************************************************************************************/
    }
}

#endif

