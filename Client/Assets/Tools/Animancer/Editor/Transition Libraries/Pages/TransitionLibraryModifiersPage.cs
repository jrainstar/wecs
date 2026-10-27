// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

#if UNITY_EDITOR

using Animancer.TransitionLibraries;
using Animancer.Units;
using Animancer.Units.Editor;
using System;
using UnityEngine;

namespace Animancer.Editor.TransitionLibraries
{
    /// <summary>[Editor-Only]
    /// A <see cref="TransitionLibraryWindowPage"/> for editing transition modifiers.
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor.TransitionLibraries/TransitionLibraryModifiersPage
    [Serializable]
    public abstract class TransitionLibraryModifiersPage : TransitionLibraryWindowPage
    {
        /************************************************************************************************************************/

        [SerializeReference]
        private TransitionModifierTableGUI _TableGUI;

        /************************************************************************************************************************/

        /// <summary>The drawer used for time fields on this page.</summary>
        public readonly AnimationTimeAttributeDrawer
            TimeDrawer = new();

        /// <summary>Creates a new <see cref="TransitionLibraryModifiersPage"/>.</summary>
        public TransitionLibraryModifiersPage(AnimationTimeAttribute.Units units)
        {
            TimeDrawer.Initialize(new AnimationTimeAttribute(units));
            TimeDrawer.Attribute.Rule = Validate.Value.IsFiniteOrNaN;
            TimeDrawer.Attribute.IsOptional = true;
        }

        /// <summary>Configures this page to display a single field or not.</summary>
        public virtual void ConfigureForSingleField(bool singleField, ref float value) { }

        /************************************************************************************************************************/

        /// <summary>Gets the value controlled by this page.</summary>
        public abstract float GetValue(ITransition transition);

        /// <summary>Gets the value controlled by this page.</summary>
        public abstract float GetValue(TransitionModifierDefinition modifier);

        /// <summary>Sets the value controlled by this page.</summary>
        public abstract void SetValue(ref TransitionModifierDefinition modifier, float value);

        /************************************************************************************************************************/

        /// <inheritdoc/>
        public override void OnGUI(Rect area)
        {
            _TableGUI ??= new();
            _TableGUI.Page = this;

            if (Window.Data.Transitions.Length == 0)
            {
                area = new Rect(
                    area.x + AnimancerGUI.StandardSpacing,
                    area.y + AnimancerGUI.StandardSpacing,
                    area.width - AnimancerGUI.StandardSpacing * 2,
                    AnimancerGUI.LineHeight);

                GUI.Label(
                    area,
                    "Library contains no Transitions." +
                    " Drag and Drop Transition Assets into this window or use the Create Transition button.");

                AnimancerGUI.NextVerticalArea(ref area);

                if (GUI.Button(area, "Create Transition"))
                    TransitionLibraryOperations.CreateTransition(Window);
            }
            else
            {
                _TableGUI.DoGUI(area, Window);
            }

            TransitionLibraryOperations.HandleBackgroundInput(area, Window);
        }

        /************************************************************************************************************************/
    }
}

#endif

