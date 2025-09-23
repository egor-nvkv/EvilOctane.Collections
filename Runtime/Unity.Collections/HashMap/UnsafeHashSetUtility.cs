using System;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.Unsafe;
using static Unity.Collections.LowLevel.Unsafe.UnsafeHashMapUtility;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static class UnsafeHashSetUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeHashSet<TKey> CreateHashSet<TKey>(int initialCapacity, int minGrowth, AllocatorManager.AllocatorHandle allocator)
            where TKey : unmanaged, IEquatable<TKey>
        {
            CollectionHelper2.CheckContainerCapacity(initialCapacity);
            CheckMinGrowth(minGrowth);
            CollectionHelper.CheckAllocator(allocator);

            SkipInit(out UnsafeHashSet<TKey> result);
            result.m_Data.Init(initialCapacity, 0, minGrowth, allocator);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashMapHelperRef<TKey> GetHelperRef<TKey>(this ref UnsafeHashSet<TKey> self)
            where TKey : unmanaged, IEquatable<TKey>
        {
            return HashMapHelperRef<TKey>.CreateFor(ref self);
        }
    }
}
