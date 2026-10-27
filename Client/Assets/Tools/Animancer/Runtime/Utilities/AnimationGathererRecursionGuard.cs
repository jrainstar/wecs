// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Animancer
{
    /// <summary>
    /// A system which protects the <see cref="Editor.AnimationGatherer"/>
    /// from checking the same object multiple times.
    /// </summary>
    /// https://kybernetik.com.au/animancer/api/Animancer/AnimationGathererRecursionGuard
    /// 
    public struct AnimationGathererRecursionGuard : IDisposable
    {
        /************************************************************************************************************************/

        /// <summary>The maximum number of recursive fields to check through before stopping.</summary>
        public static int MaxFieldDepth = 7;

        /************************************************************************************************************************/

        /// <summary>Types which will be skipped when attempting to gather animations from an unknown object type.</summary>
        public static readonly HashSet<Type> DontGatherFrom = new();

        /************************************************************************************************************************/

        private static readonly HashSet<object>
            ObjectsChecked = new();

        private static int _GuardCount;

#if UNITY_EDITOR
        /// <summary>[Editor-Only] Resets static fields in case the Play Mode Domain Reload is disabled.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            ObjectsChecked.Clear();
            _GuardCount = 0;
        }
#endif

        /************************************************************************************************************************/

        /// <summary>Call this with a <c>using</c> statement before calling <see cref="HasCheckedObject"/>.</summary>
        public static AnimationGathererRecursionGuard Begin()
        {
            _GuardCount++;
            return default;
        }

        /************************************************************************************************************************/

        /// <summary>Ends a block started by <see cref="Begin"/>.</summary>
        public readonly void Dispose()
        {
            _GuardCount--;
            if (_GuardCount == 0)
                ObjectsChecked.Clear();
        }

        /************************************************************************************************************************/

        /// <summary>Stores the specified object and returns true if it wasn't already stored.</summary>
        public static bool HasCheckedObject(object obj)
        {
            if (_GuardCount <= 0)
                Debug.LogError(
                    $"{nameof(AnimationGathererRecursionGuard)} is being used without {nameof(Begin)} being caled");

            return !ObjectsChecked.Add(obj);
        }

        /************************************************************************************************************************/
    }
}

