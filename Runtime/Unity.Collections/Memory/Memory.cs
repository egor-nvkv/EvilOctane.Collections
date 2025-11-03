using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using static Unity.Collections.CollectionHelper;
using static Unity.Collections.CollectionHelper2;

namespace Unity.Collections
{
    public unsafe struct MemoryExposed
    {
        public static void* AllocateList(int elementSize, int elementAlignment, nint capacity, AllocatorManager.AllocatorHandle allocator, out nint actualCapacity)
        {
            CheckContainerElementSize(elementSize);
            CheckIntPositivePowerOfTwo(elementAlignment);

            CheckContainerCapacity(capacity);
            CheckAllocator(allocator);

            Hint.Assume(elementSize >= 1);
            int elementSizeLog2 = math.ceillog2(elementSize);

            actualCapacity = (nint)math.max(capacity, CacheLineSize >> elementSizeLog2);
            actualCapacity = (nint)math.ceilpow2(actualCapacity);

            return Memory.Unmanaged.Allocate(size: actualCapacity * elementSize, align: elementAlignment, allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* AllocateList<T>(nint capacity, AllocatorManager.AllocatorHandle allocator, out nint actualCapacity)
            where T : unmanaged
        {
            return (T*)AllocateList(sizeof(T), UnsafeUtility.AlignOf<T>(), capacity, allocator, out actualCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureListCapacity<T>(ref UntypedUnsafeListMutable list, nint capacity, bool keepOldData = true)
            where T : unmanaged
        {
            CheckContainerCapacity(capacity);

            if (capacity > list.m_capacity)
            {
                if (keepOldData)
                {
                    IncreaseListCapacityKeepOldData(ref list, elementSize: sizeof(T), elementAlignment: UnsafeUtility.AlignOf<T>(), capacity: capacity);
                }
                else
                {
                    IncreaseListCapacityTrashOldData(ref list, elementSize: sizeof(T), elementAlignment: UnsafeUtility.AlignOf<T>(), capacity: capacity);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureListSlack<T>(ref UntypedUnsafeListMutable list, nint slack)
            where T : unmanaged
        {
            CheckContainerElementCount(slack);
            EnsureListCapacity<T>(ref list, list.m_length + slack);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JobHandle ScheduleDispose<T>(T* ptr, AllocatorManager.AllocatorHandle allocator, JobHandle inputDeps = new())
            where T : unmanaged
        {
            return new UnsafeDisposeJob
            {
                Ptr = ptr,
                Allocator = allocator
            }.Schedule(inputDeps);
        }

        internal static void IncreaseListCapacityKeepOldData(ref UntypedUnsafeListMutable list, int elementSize, int elementAlignment, nint capacity)
        {
            IncreaseListCapacity(ref list, elementSize, elementAlignment, capacity, keepOldData: true);
        }

        internal static void IncreaseListCapacityTrashOldData(ref UntypedUnsafeListMutable list, int elementSize, int elementAlignment, nint capacity)
        {
            IncreaseListCapacity(ref list, elementSize, elementAlignment, capacity, keepOldData: false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void IncreaseListCapacity(ref UntypedUnsafeListMutable list, int elementSize, int elementAlignment, nint capacity, bool keepOldData)
        {
            Assert.IsTrue(list.Ptr != null);
            Assert.IsTrue(capacity > list.m_capacity);

            CheckContainerElementSize(elementSize);
            CheckIntPositivePowerOfTwo(elementAlignment);

            CheckCapacityInRange((int)capacity, list.m_length);
            CheckAllocator(list.Allocator);

            long capacityCeilPow2 = math.ceilpow2(capacity);

            void* oldPtr = list.Ptr;
            list.Ptr = Memory.Unmanaged.Allocate(size: capacityCeilPow2 * elementSize, align: elementAlignment, list.Allocator);

            Assert.IsTrue(capacityCeilPow2 <= int.MaxValue);
            list.m_capacity = (int)capacityCeilPow2;

            if (keepOldData)
            {
                UnsafeUtility.MemCpy(list.Ptr, oldPtr, list.m_length * elementSize);
            }

            Memory.Unmanaged.Free(oldPtr, allocator: list.Allocator);
        }

        public struct Unmanaged
        {
            [HideInCallstack]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void* Allocate(long size, int align, AllocatorManager.AllocatorHandle allocator)
            {
                return Memory.Unmanaged.Allocate(size, align, allocator);
            }

            [HideInCallstack]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Free(void* pointer, AllocatorManager.AllocatorHandle allocator)
            {
                Memory.Unmanaged.Free(pointer, allocator);
            }

            [HideInCallstack]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T* Allocate<T>(AllocatorManager.AllocatorHandle allocator) where T : unmanaged
            {
                return Memory.Unmanaged.Allocate<T>(allocator);
            }

            [HideInCallstack]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Free<T>(T* pointer, AllocatorManager.AllocatorHandle allocator) where T : unmanaged
            {
                Memory.Unmanaged.Free(pointer, allocator);
            }
        }
    }
}
