using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;

namespace Unity.Collections
{
    [GenerateTestsForBurstCompatibility]
    public static unsafe class CollectionHelper2
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckReinterpretArgs<TSource, TDestination>()
            where TSource : unmanaged
            where TDestination : unmanaged
        {
            int sizeSource = sizeof(TSource);
            int sizeDestination = sizeof(TDestination);

            if (sizeSource != sizeDestination)
            {
                throw new InvalidOperationException($"Cannot reinterpret between types of different sizes: source = {sizeSource}, destination = {sizeDestination}.");
            }

            int alignSource = AlignOf<TSource>();
            int alignDestination = AlignOf<TDestination>();

            if (alignDestination > alignSource)
            {
                throw new InvalidOperationException($"Cannot reinterpret to over-aligned type: source = {alignSource}, destination = {alignDestination}.");
            }
        }
    }
}
