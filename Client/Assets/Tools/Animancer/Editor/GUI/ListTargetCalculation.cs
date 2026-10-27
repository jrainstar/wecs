// Animancer // https://kybernetik.com.au/animancer // Copyright 2018-2026 Kybernetik //

#if UNITY_EDITOR

using UnityEngine;

namespace Animancer.Editor
{
    /// <summary>[Editor-Only] A utility for calculating where a pointer is aiming inside a uniformly sized list.</summary>
    /// https://kybernetik.com.au/animancer/api/Animancer.Editor/ListTargetCalculation
    public struct ListTargetCalculation
    {
        /************************************************************************************************************************/

        /// <summary>The target list index.</summary>
        public int Index;

        /// <summary>The target position within the target list index.</summary>
        /// <remarks>0 means the target is right at the start, 0.5 in the middle, and 1 at the end.</remarks>
        public float LocalOffset;

        /************************************************************************************************************************/

        /// <summary>Creates a new <see cref="ListTargetCalculation"/>.</summary>
        public ListTargetCalculation(
            int index,
            float localOffset)
        {
            Index = index;
            LocalOffset = localOffset;
        }

        /// <summary>Calculates the target of the specified parameters.</summary>
        public ListTargetCalculation(
            float start,
            float size,
            float target)
        {
            target -= start;
            target /= size;
            Index = Mathf.FloorToInt(target);
            LocalOffset = target - Index;
        }

        /// <summary>Calculates the target of the specified parameters.</summary>
        public ListTargetCalculation(
            float start,
            float size,
            int count,
            float target)
            : this(start, size, target)
        {
            Index = Mathf.Clamp(Mathf.FloorToInt(Index), 0, count);
        }

        /************************************************************************************************************************/

        /// <summary>Describes the <see cref="Index"/> and <see cref="LocalOffset"/>.</summary>
        public override readonly string ToString()
            => $"({Index}, {LocalOffset})";

        /************************************************************************************************************************/
    }
}

#endif

