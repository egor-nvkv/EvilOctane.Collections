using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Unity.Collections
{
    public unsafe struct MemoryExposed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* AllocateList_Inline(int elementSize, int elementAlignment, int capacity, AllocatorManager.AllocatorHandle allocator, out int actualCapacity)
        {
            CheckElementSize(elementSize);
            CollectionHelper.CheckIntPositivePowerOfTwo(elementAlignment);

            CollectionHelper2.CheckContainerCapacity(capacity);
            CollectionHelper.CheckAllocator(allocator);

            Hint.Assume(elementSize >= 1);
            int elementSizeLog2 = math.ceillog2(elementSize);

            actualCapacity = math.max(capacity, CollectionHelper.CacheLineSize >> elementSizeLog2);
            actualCapacity = math.ceilpow2(actualCapacity);

            return Memory.Unmanaged.Allocate(size: actualCapacity * elementSize, align: elementAlignment, allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AllocateList_Inline<T>(int capacity, AllocatorManager.AllocatorHandle allocator, out int actualCapacity)
            where T : unmanaged
        {
            return (T*)AllocateList_Inline(sizeof(T), UnsafeUtility.AlignOf<T>(), capacity, allocator, out actualCapacity);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void* AllocateList_NoInline(int elementSize, int elementAlignment, int capacity, AllocatorManager.AllocatorHandle allocator, out int actualCapacity)
        {
            return AllocateList_Inline(elementSize, elementAlignment, capacity, allocator, out actualCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AllocateList_NoInline<T>(int capacity, AllocatorManager.AllocatorHandle allocator, out int actualCapacity)
            where T : unmanaged
        {
            return (T*)AllocateList_NoInline(sizeof(T), UnsafeUtility.AlignOf<T>(), capacity, allocator, out actualCapacity);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void IncreaseListCapacity_NoInline(ref UntypedUnsafeListMutable list, int elementSize, int elementAlignment, int capacity)
        {
            Assert.IsTrue(list.Ptr != null);
            Assert.IsTrue(capacity > list.m_capacity);

            CheckElementSize(elementSize);
            CollectionHelper.CheckIntPositivePowerOfTwo(elementAlignment);

            CollectionHelper.CheckCapacityInRange(capacity, list.m_length);
            CollectionHelper.CheckAllocator(list.Allocator);

            int capacityCeilpow2 = math.ceilpow2(capacity);

            void* oldPtr = list.Ptr;
            list.Ptr = Memory.Unmanaged.Allocate(size: capacityCeilpow2 * elementSize, align: elementAlignment, list.Allocator);

            list.m_capacity = capacityCeilpow2;

            UnsafeUtility.MemCpy(list.Ptr, oldPtr, list.m_length * elementSize);
            Memory.Unmanaged.Free(oldPtr, allocator: list.Allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureListCapacity<T>(ref UntypedUnsafeListMutable list, int capacity)
            where T : unmanaged
        {
            CollectionHelper2.CheckContainerCapacity(capacity);

            if (capacity > list.m_capacity)
            {
                IncreaseListCapacity_NoInline(ref list, elementSize: sizeof(T), elementAlignment: UnsafeUtility.AlignOf<T>(), capacity: capacity);
            }

            Assert.IsTrue(list.m_capacity >= capacity);
            Hint.Assume(list.m_capacity >= capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureListSlack<T>(ref UntypedUnsafeListMutable list, int slack)
            where T : unmanaged
        {
            CollectionHelper2.CheckContainerCapacity(slack);
            EnsureListCapacity<T>(ref list, list.m_length + slack);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckElementSize(int elementSize)
        {
            if (Hint.Unlikely(elementSize <= 0))
            {
                throw new ArgumentException("Element size must be greater than zero.");
            }
        }

        public struct Unmanaged
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void* Allocate(long size, int align, AllocatorManager.AllocatorHandle allocator)
            {
                return Memory.Unmanaged.Allocate(size, align, allocator);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Free(void* pointer, AllocatorManager.AllocatorHandle allocator)
            {
                Memory.Unmanaged.Free(pointer, allocator);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T* Allocate<T>(AllocatorManager.AllocatorHandle allocator) where T : unmanaged
            {
                return Memory.Unmanaged.Allocate<T>(allocator);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Free<T>(T* pointer, AllocatorManager.AllocatorHandle allocator) where T : unmanaged
            {
                Memory.Unmanaged.Free<T>(pointer, allocator);
            }
        }
    }
}
