using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Unity.Collections
{
    public unsafe struct MemoryExposed
    {
        public static T* AllocateList<T>(int capacity, AllocatorManager.AllocatorHandle allocator, out int actualCapacity)
            where T : unmanaged
        {
            CollectionHelper.CheckCapacityInRange(capacity: int.MaxValue, length: capacity);
            CollectionHelper.CheckAllocator(allocator);

            actualCapacity = math.max(capacity, CollectionHelper.CacheLineSize / sizeof(T));
            actualCapacity = math.ceilpow2(actualCapacity);

            return (T*)Memory.Unmanaged.Allocate(size: actualCapacity * sizeof(T), align: UnsafeUtility.AlignOf<T>(), allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureListCapacity<T>(ref UntypedUnsafeListMutable list, int capacity)
            where T : unmanaged
        {
            if (capacity > list.m_capacity)
            {
                IncreaseListCapacityNoInline(ref list, elementSize: sizeof(T), elementAlignment: UnsafeUtility.AlignOf<T>(), capacity: capacity);
            }

            Assert.IsTrue(list.m_capacity >= capacity);
            Hint.Assume(list.m_capacity >= capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureListSlack<T>(ref UntypedUnsafeListMutable list, int slack)
            where T : unmanaged
        {
            EnsureListCapacity<T>(ref list, list.m_length + slack);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void IncreaseListCapacityNoInline(ref UntypedUnsafeListMutable list, [AssumeRange(1, int.MaxValue)] int elementSize, [AssumeRange(1, int.MaxValue)] int elementAlignment, [AssumeRange(1, int.MaxValue)] int capacity)
        {
            Assert.IsTrue(list.Ptr != null);
            Hint.Assume(list.Ptr != null);

            Assert.IsTrue(capacity > list.m_capacity);
            Hint.Assume(capacity > list.m_capacity);

            CollectionHelper.CheckCapacityInRange(capacity: int.MaxValue, length: capacity);
            CollectionHelper.CheckAllocator(list.Allocator);

            int capacityCeilpow2 = math.ceilpow2(capacity);

            void* oldPtr = list.Ptr;
            list.Ptr = Memory.Unmanaged.Allocate(size: capacityCeilpow2 * elementSize, align: elementAlignment, list.Allocator);

            list.m_capacity = capacityCeilpow2;

            UnsafeUtility.MemCpy(list.Ptr, oldPtr, list.m_length * elementSize);
            Memory.Unmanaged.Free(oldPtr, allocator: list.Allocator);
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

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UntypedUnsafeListMutable
    {
#pragma warning disable 169
        // <WARNING>
        // 'Header' of this struct must binary match `UntypedUnsafeList`, `UnsafeList`, `UnsafePtrList`, and `NativeArray` struct.
        [NativeDisableUnsafePtrRestriction]
        public void* Ptr;
        public int m_length;
        public int m_capacity;
        public AllocatorManager.AllocatorHandle Allocator;
#pragma warning restore 169
    }
}
