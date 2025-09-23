using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using static System.Runtime.CompilerServices.Unsafe;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe class UnsafeHashMapUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeHashMap<TKey, TValue> CreateHashMap<TKey, TValue>(int initialCapacity, int minGrowth, AllocatorManager.AllocatorHandle allocator)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CollectionHelper2.CheckContainerCapacity(initialCapacity);
            CheckMinGrowth(minGrowth);
            CollectionHelper.CheckAllocator(allocator);

            SkipInit(out UnsafeHashMap<TKey, TValue> result);
            result.m_Data.Init(initialCapacity, sizeof(TValue), minGrowth, allocator);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashMapHelperRef<TKey> GetHelperRef<TKey, TValue>(this ref UnsafeHashMap<TKey, TValue> self)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return HashMapHelperRef<TKey>.CreateFor(ref self);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CheckMinGrowth(int minGrowth)
        {
            if (Hint.Unlikely(minGrowth <= 1))
            {
                throw new ArgumentException("MinGrowth must be greater than 1.");
            }

            if (Hint.Unlikely(!CollectionHelper.IsPowerOfTwo(minGrowth)))
            {
                throw new ArgumentException("MinGrowth must be a power of 2.");
            }
        }
    }
}
