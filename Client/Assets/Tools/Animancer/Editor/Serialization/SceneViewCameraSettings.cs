// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] A serializable copy of the <see cref="SceneView"/> camera settings.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/SceneViewCameraSettings
    [Serializable]
    public struct SceneViewCameraSettings
    {
        /************************************************************************************************************************/

        /// <summary><see cref="SceneView.pivot"/>.</summary>
        public Vector3 Pivot;

        /// <summary><see cref="SceneView.rotation"/>.</summary>
        public Quaternion Rotation;

        /// <summary><see cref="SceneView.in2DMode"/>.</summary>
        public bool In2DMode;

        /// <summary><see cref="SceneView.orthographic"/>.</summary>
        public bool Orthographic;

        /// <summary><see cref="SceneView.size"/>.</summary>
        public float Size;

        /************************************************************************************************************************/

        /// <summary>Have these settings been set to something other than default values?</summary>
        // All zeroes is an invalid rotation quaternion (identity is 0, 0, 0, 1).
        public readonly bool IsInitialized
            => Rotation.w != 0
            || Rotation.x != 0
            || Rotation.y != 0
            || Rotation.z != 0;

        /************************************************************************************************************************/

        /// <summary>Captures the current values of the `sceneView`.</summary>
        public SceneViewCameraSettings(SceneView sceneView)
        {
            In2DMode = sceneView.in2DMode;
            Orthographic = sceneView.orthographic;
            Pivot = sceneView.pivot;
            Rotation = sceneView.rotation;
            Size = sceneView.size;
        }

        /************************************************************************************************************************/

        /// <summary>Sets the `sceneView` to use the stored values.</summary>
        public readonly void Apply(SceneView sceneView)
        {
            sceneView.in2DMode = In2DMode;
            sceneView.orthographic = Orthographic;
            sceneView.pivot = Pivot;
            sceneView.rotation = Rotation;
            sceneView.size = Size;
        }

        /************************************************************************************************************************/
    }
}

#endif

