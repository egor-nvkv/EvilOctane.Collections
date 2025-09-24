using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;

namespace Unity.Collections
{
    [GenerateTestsForBurstCompatibility]
    public static unsafe partial class CollectionHelper2
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckContainerCapacity(int capacity)
        {
            if (Hint.Unlikely(capacity < 0))
            {
                throw new ArgumentException("Capacity cannot be negative.");
            }
        }
    }
}
