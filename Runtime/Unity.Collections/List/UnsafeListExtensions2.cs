using System.Runtime.CompilerServices;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe class UnsafeListExtensions2
    {
        public static UnsafeList<T> Create<T>(int capacity, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged
        {
            T* ptr = MemoryExposed.AllocateList_Inline<T>(capacity, allocator, out int actualCapacity);

            return new UnsafeList<T>()
            {
                Ptr = ptr,
                m_length = 0,
                m_capacity = actualCapacity,
                Allocator = allocator
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeList<T> Create_NoInline<T>(int capacity, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged
        {
            T* ptr = MemoryExposed.AllocateList_NoInline<T>(capacity, allocator, out int actualCapacity);

            return new UnsafeList<T>()
            {
                Ptr = ptr,
                m_length = 0,
                m_capacity = actualCapacity,
                Allocator = allocator
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureCapacity<T>(this ref UnsafeList<T> self, int capacity)
            where T : unmanaged
        {
            MemoryExposed.EnsureListCapacity<T>(ref Reinterpret<UnsafeList<T>, UntypedUnsafeListMutable>(ref self), capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureSlack<T>(this ref UnsafeList<T> self, int slack)
            where T : unmanaged
        {
            MemoryExposed.EnsureListSlack<T>(ref Reinterpret<UnsafeList<T>, UntypedUnsafeListMutable>(ref self), slack);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddRange<T>(this ref UnsafeList<T> self, UnsafeSpan<T> valueSpan)
            where T : unmanaged
        {
            self.AddRange(valueSpan.Ptr, valueSpan.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddRangeNoResize<T>(this ref UnsafeList<T> self, UnsafeSpan<T> valueSpan)
            where T : unmanaged
        {
            self.AddRangeNoResize(valueSpan.Ptr, valueSpan.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeSpan<T> AsSpan<T>(this UnsafeList<T> self)
            where T : unmanaged
        {
            return new UnsafeSpan<T>(self.Ptr, self.Length);
        }
    }
}
