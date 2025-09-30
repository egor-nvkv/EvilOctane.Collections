using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;
using static Unity.Collections.CollectionHelper;
using static Unity.Collections.CollectionHelper2;

namespace Unity.Collections
{
    public unsafe struct MemoryExposed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* AllocateList_Inline(int elementSize, int elementAlignment, int capacity, AllocatorManager.AllocatorHandle allocator, out int actualCapacity)
        {
            CheckContainerElementSize(elementSize);
            CheckIntPositivePowerOfTwo(elementAlignment);

            CheckContainerCapacity(capacity);
            CheckAllocator(allocator);

            Hint.Assume(elementSize >= 1);
            int elementSizeLog2 = math.ceillog2(elementSize);

            actualCapacity = math.max(capacity, CacheLineSize >> elementSizeLog2);
            actualCapacity = math.ceilpow2(actualCapacity);

            void* ptr = Memory.Unmanaged.Allocate(size: actualCapacity * elementSize, align: elementAlignment, allocator);

            Hint.Assume(ptr != null);
            Hint.Assume(actualCapacity >= capacity);
            return ptr;
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

            CheckContainerElementSize(elementSize);
            CheckIntPositivePowerOfTwo(elementAlignment);

            CheckCapacityInRange(capacity, list.m_length);
            CheckAllocator(list.Allocator);

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
            CheckContainerCapacity(capacity);

            if (capacity > list.m_capacity)
            {
                IncreaseListCapacity_NoInline(ref list, elementSize: sizeof(T), elementAlignment: UnsafeUtility.AlignOf<T>(), capacity: capacity);

                Hint.Assume(list.Ptr != null);
                Hint.Assume(list.m_capacity >= capacity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureListSlack<T>(ref UntypedUnsafeListMutable list, int slack)
            where T : unmanaged
        {
            CheckContainerCapacity(slack);
            EnsureListCapacity<T>(ref list, list.m_length + slack);
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
