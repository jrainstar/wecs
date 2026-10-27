// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

#if UNITY_EDITOR

#if !UNITY_6000_3_OR_NEWER
using EntityId = System.Int32;
#endif

using Animancer.TransitionLibraries;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using static Animancer.Editor.AnimancerGUI;
using Object = UnityEngine.Object;

namespace Animancer.Editor.TransitionLibraries
{
    /// <summary>[Editor-Only]
    /// An <see cref="EditorWindow"/> for configuring <see cref="TransitionLibraryAsset"/>.
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.TransitionLibraries/TransitionLibraryWindow
    public class TransitionLibraryWindow :
        SerializedDataEditorWindow<TransitionLibraryAsset, TransitionLibraryDefinition>
    {
        /************************************************************************************************************************/

        /// <summary>Opens a window for the `library`.</summary>
        public static TransitionLibraryWindow Open(TransitionLibraryAsset library)
            => Open<TransitionLibraryWindow>(library, true, typeof(SceneView));

        /************************************************************************************************************************/

        /// <summary>
        /// Double clicking a <see cref="TransitionLibraryAsset"/>
        /// opens it in the <see cref="TransitionLibraryWindow"/>.
        /// </summary>
        [OnOpenAsset]
        private static bool OnOpenAsset(EntityId entityId, int line)
        {
            var asset = Serialization.EntityIdToObject(entityId);

            if (asset is not TransitionLibraryAsset library)
                return false;

            Open(library);
            return true;
        }

        /************************************************************************************************************************/

        /// <summary>The current window instance.</summary>
        public static TransitionLibraryWindow Instance { get; private set; }

        /// <summary>Is a window currently showing the `library`.</summary>
        public static bool IsShowing(Object library)
            => Instance != null
            && Instance.SourceObject == library;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override TransitionLibraryDefinition SourceData
        {
            get => SourceObject.Definition;
            set => SourceObject.Definition = value;
        }

        /************************************************************************************************************************/

        /// <summary>The <see cref="TransitionLibraryEditorDataInternal"/> of the source asset.</summary>
        public TransitionLibraryEditorDataInternal SourceEditorData
        {
            get => SourceObject.GetOrCreateEditorData().Data;
            set => SourceObject.GetOrCreateEditorData().Data = value;
        }

        [SerializeField]
        private TransitionLibraryEditorDataInternal _EditorData;

        /// <summary>A copy of the <see cref="SourceEditorData"/> being managed by this window.</summary>
        public ref TransitionLibraryEditorDataInternal EditorData
            => ref _EditorData;

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override bool HasDataChanged
        {
            get
            {
                if (base.HasDataChanged)
                    return true;

                if (SourceObject == null ||
                    _EditorData == null)
                    return false;

                var sourceEditorData = SourceEditorData;
                return sourceEditorData != null && !_EditorData.Equals(sourceEditorData);
            }
        }

        /************************************************************************************************************************/

        [SerializeField]
        private TransitionLibrarySelection _Selection;

        /// <summary>Manages the objects which can be selected within a library.</summary>
        public TransitionLibrarySelection Selection
            => AnimancerEditorUtilities.FindOrCreate(ref _Selection);

        /************************************************************************************************************************/

        [SerializeReference]
        private List<TransitionLibraryWindowPage> _Pages;

        [SerializeField]
        private int _CurrentPage;

        /// <summary>The currently selected page.</summary>
        public TransitionLibraryWindowPage CurrentPage
        {
            get
            {
                _CurrentPage = Mathf.Clamp(_CurrentPage, 0, _Pages.Count - 1);
                return _Pages[_CurrentPage];
            }
        }

        /// <summary>Tries to find a page of the specified type and returns true if successful.</summary>
        public bool TryGetPage<T>(out T page)
            where T : TransitionLibraryWindowPage
        {
            for (int i = 0; i < _Pages.Count; i++)
            {
                page = _Pages[i] as T;
                if (page != null)
                    return true;
            }

            page = null;
            return false;
        }

        /************************************************************************************************************************/

        /// <summary>Object highlight manager.</summary>
        public readonly TransitionLibraryWindowHighlighter
            Highlighter = new();

        /// <summary>Transitions and groups ordered by group.</summary>
        public readonly TransitionGroupCache
            Items = new();

        /************************************************************************************************************************/

        /// <summary>Called when an object is selected.</summary>
        protected virtual void OnSelectionChange()
        {
            if (_Selection != null)
                _Selection.OnSelectionChange();

            var library = UnityEditor.Selection.activeObject as TransitionLibraryAsset;
            if (library != null && library != SourceObject)
                SetAndCaptureSource(library);
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void OnEnable()
        {
            base.OnEnable();

            Instance = this;
            wantsMouseMove = true;

            // MainStageView, CanvasGroup Icon, GridLayoutGroup Icon.
            titleContent = EditorGUIUtility.IconContent("CanvasGroup Icon");
            titleContent.text = "Transition Library";

            AnimancerEditorUtilities.InstantiateDerivedTypes(ref _Pages);

            for (int i = 0; i < _Pages.Count; i++)
                _Pages[i].Window = this;

            OnSelectionChange();
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void OnDisable()
        {
            base.OnDisable();

            if (Instance == this)
                Instance = null;
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            DestroyImmediate(_Selection);
        }

        /************************************************************************************************************************/

        /// <summary>Draws the GUI of this window.</summary>
        protected virtual void OnGUI()
        {
            if (SourceObject == null)
            {
                GUILayout.Label("No Transition Library has been selected");
                return;
            }

            Items.GatherTransitionsAndGroups(Data.Transitions, EditorData);

            DoHeaderGUI();
            DoBodyGUI();
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        protected override void CaptureData()
        {
            _EditorData = SourceEditorData?.CopyableClone() ?? new();
            AnimancerReflection.TryInvoke(_EditorData, "OnValidate");

            base.CaptureData();

            Data.SortAliases();
        }

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void Apply()
        {
            var editorData = SourceObject.GetOrCreateEditorData();
            using (new ModifySerializedField(editorData, name, false))
            {
                editorData.Data = _EditorData.CopyableClone();
            }

            TransitionLibrarySort.Sort(Data, editorData.Data);

            base.Apply();

            for (int i = 0; i < Data.Transitions.Length; i++)
            {
                var transition = Data.Transitions[i];
                if (transition == null ||
                    EditorUtility.IsPersistent(transition))
                    continue;

                AssetDatabase.AddObjectToAsset(transition, SourceObject);
            }
        }

        /************************************************************************************************************************/

        private static ButtonGroupStyles _ApplyRevertStyles;

        /// <summary>Draws the header GUI.</summary>
        private void DoHeaderGUI()
        {
            if (_ApplyRevertStyles.left == null)
                _ApplyRevertStyles = new(
                    EditorStyles.toolbarButton,
                    EditorStyles.toolbarButton,
                    EditorStyles.toolbarButton);

            GUILayout.BeginHorizontal();

            var style = EditorStyles.toolbar;

            var applyRevertWidth = CalculateApplyRevertWidth(_ApplyRevertStyles) - StandardSpacing - 1;

            var area = GUILayoutUtility.GetRect(position.width, style.fixedHeight);

            var currentEvent = Event.current;
            if (currentEvent.type == EventType.Repaint)
                style.Draw(area, false, false, false, false);

            var pageArea = StealFromLeft(ref area, PageSelectionWidth);
            var applyRevertArea = StealFromRight(ref area, applyRevertWidth);
            var pathArea = area;

            DoPageSelectionDropdown(pageArea);
            DoAssetPathButton(pathArea, currentEvent);

            DoApplyRevertGUI(applyRevertArea, _ApplyRevertStyles);

            GUILayout.EndHorizontal();
        }

        /************************************************************************************************************************/

        [NonSerialized]
        private float _PageSelectionWidth;

        private float PageSelectionWidth
        {
            get
            {
                if (_PageSelectionWidth == 0)
                {
                    for (int i = 0; i < _Pages.Count; i++)
                    {
                        _PageSelectionWidth = Math.Max(
                            _PageSelectionWidth,
                            EditorStyles.toolbarDropDown.CalculateWidth(_Pages[i].DisplayName));
                    }
                }

                return _PageSelectionWidth;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Draws a dropdown button for selecting the <see cref="CurrentPage"/>.</summary>
        private void DoPageSelectionDropdown(Rect area)
        {
            using (var label = PooledGUIContent.Acquire(CurrentPage.DisplayName, CurrentPage.HelpTooltip))
                if (!EditorGUI.DropdownButton(area, label, FocusType.Passive, EditorStyles.toolbarDropDown))
                    return;

            var menu = new GenericMenu();

            for (int i = 0; i < _Pages.Count; i++)
            {
                var index = i;
                var page = _Pages[index];
                menu.AddItem(
                    new(page.DisplayName),
                    _CurrentPage == index,
                    () =>
                    {
                        _CurrentPage = index;
                        Deselect();
                    });
            }

            menu.AddSeparator("");
            menu.AddItem(
                new("Documentation"),
                false,
                () => Application.OpenURL(Strings.DocsURLs.TransitionLibraries));

            menu.ShowAsContext();
        }

        /************************************************************************************************************************/

        private static GUIStyle _AssetPathStyle;

        private readonly GUIContent AssetPath = new();

        /// <summary>Draws the asset path of the target library and selects it if clicked.</summary>
        private void DoAssetPathButton(Rect area, Event currentEvent)
        {
            _AssetPathStyle ??= new(EditorStyles.toolbarButton)
            {
                richText = true,
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Italic,
                fontSize = (int)(EditorStyles.toolbarButton.fontSize * 0.8f),
            };

            if (currentEvent.type == EventType.Repaint)
            {
                var assetPath = AssetDatabase.GetAssetPath(SourceObject);
                if (string.IsNullOrEmpty(assetPath))
                {
                    AssetPath.text = "The target Transition Library isn't saved as an asset.";
                    AssetPath.tooltip = null;
                }
                else if (AssetPath.tooltip != assetPath)
                {
                    AssetPath.tooltip = assetPath;

                    var directory = Path.GetDirectoryName(assetPath).Replace('\\', '/');
                    var file = Path.GetFileNameWithoutExtension(assetPath);
                    assetPath = $"{directory}/<b>{file}</b>";

                    AssetPath.text = assetPath;
                }
            }

            if (GUI.Button(area, AssetPath, _AssetPathStyle))
            {
                if (Selection.Selected != (object)SourceObject)
                    Selection.Select(this, SourceObject, -1, TransitionLibrarySelection.SelectionType.Library);
                else
                    EditorGUIUtility.PingObject(SourceObject);
            }
        }

        /************************************************************************************************************************/

        /// <summary>Draws the <see cref="CurrentPage"/>.</summary>
        private void DoBodyGUI()
        {
            GUILayout.FlexibleSpace();
            var area = GUILayoutUtility.GetLastRect();
            area.width = position.width;

            EditorGUI.DrawRect(area, Grey(0.2f, 0.5f));

            if (_Pages.Count > 0)
            {
                Highlighter.BeginGUI(area);

                CurrentPage?.OnGUI(area);

                Highlighter.EndGUI(this);
            }
        }

        /************************************************************************************************************************/
    }
}

#endif

