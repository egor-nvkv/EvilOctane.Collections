using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;

namespace Unity.Collections
{
    [GenerateTestsForBurstCompatibility]
    public static unsafe partial class CollectionHelper2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint Align(nint size, nint alignmentPowerOfTwo)
        {
            return (nint)CollectionHelper.Align((ulong)size, (ulong)alignmentPowerOfTwo);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint Align(nuint size, nuint alignmentPowerOfTwo)
        {
            return (nuint)CollectionHelper.Align(size, alignmentPowerOfTwo);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckContainerLength(int length)
        {
            if (Hint.Unlikely(length < 0))
            {
                throw new ArgumentOutOfRangeException("Length cannot be negative.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckContainerCapacity(int capacity)
        {
            if (Hint.Unlikely(capacity < 0))
            {
                throw new ArgumentOutOfRangeException("Capacity cannot be negative.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckContainerElementCount(int count)
        {
            if (Hint.Unlikely(count < 0))
            {
                throw new ArgumentOutOfRangeException("Count cannot be negative.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckContainerElementSize(int elementSize)
        {
            if (Hint.Unlikely(elementSize <= 0))
            {
                throw new ArgumentOutOfRangeException("Element size must be greater than zero.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckContainerIndexInRange(int index, int length)
        {
            CollectionHelper.CheckIndexInRange(index: index, length: length);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckAddNoResizeHasEnoughCapacity(int length, int capacity, int count)
        {
            CheckContainerLength(length);
            CheckContainerCapacity(capacity);
            CheckContainerElementCount(count);

            if (Hint.Unlikely(length + count > capacity))
            {
                throw new InvalidOperationException($"AddNoResize assumes that capacity is sufficient (Current length = {length}, Capacity = {capacity}, Count to add = {count}).");
            }
        }
    }
}
