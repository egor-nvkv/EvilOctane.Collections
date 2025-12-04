using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;

namespace Unity.Collections
{
    public static unsafe partial class CollectionHelper2
    {
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint Align(nint size, int alignmentPowerOfTwo)
        {
            return (nint)CollectionHelper.Align((ulong)size, (ulong)alignmentPowerOfTwo);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nuint Align(nuint size, uint alignmentPowerOfTwo)
        {
            return (nuint)CollectionHelper.Align(size, alignmentPowerOfTwo);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* AlignPointer(void* ptr, int alignmentPowerOfTwo)
        {
            return CollectionHelper.AlignPointer(ptr, alignmentPowerOfTwo);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckSpanPtr(void* ptr, int length)
        {
            CheckContainerLength(length);

            if (Hint.Unlikely(ptr == null && length > 0))
            {
                throw new ArgumentException("Ptr cannot be null with non-zero length.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckContainerLength(nint length)
        {
            if (Hint.Unlikely(length < 0))
            {
                throw new ArgumentOutOfRangeException("Length cannot be negative.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckContainerCapacity(nint capacity)
        {
            if (Hint.Unlikely(capacity < 0))
            {
                throw new ArgumentOutOfRangeException("Capacity cannot be negative.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckContainerElementCount(nint count)
        {
            if (Hint.Unlikely(count < 0))
            {
                throw new ArgumentOutOfRangeException("Element count cannot be negative.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckContainerElementSize(nint elementSize)
        {
            if (Hint.Unlikely(elementSize <= 0))
            {
                throw new ArgumentOutOfRangeException("Element size must be greater than zero.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckContainerStartIndex(nint startIndex)
        {
            if (Hint.Unlikely(startIndex < 0))
            {
                throw new ArgumentOutOfRangeException("Start index cannot be negative.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckContainerIndexInRange(nint index, nint length)
        {
            if ((nuint)index >= (nuint)length)
            {
                throw new IndexOutOfRangeException($"Index = {(long)index} is out of range in container of Length = {(long)length}.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckAddNoResizeHasEnoughCapacity(nint length, nint capacity, nint count)
        {
            CheckContainerLength(length);
            CheckContainerCapacity(capacity);
            CheckContainerElementCount(count);

            if (Hint.Unlikely(length + count > capacity))
            {
                throw new InvalidOperationException($"AddNoResize assumes that capacity is sufficient (Length = {(long)length}, Capacity = {(long)capacity}, Count = {(long)count}).");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckSliceArgs(int length, int startIndex, int count)
        {
            CheckContainerLength(length);
            CheckContainerStartIndex(startIndex);
            CheckContainerIndexInRange(startIndex, length + 1);
            CheckContainerElementCount(count);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckCopyLengths(int sourceLength, int destLength)
        {
            if (sourceLength != destLength)
            {
                throw new ArgumentException("Source and Destination lengths must be the same.");
            }
        }
    }
}
