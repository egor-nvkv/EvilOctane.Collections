using System.Runtime.CompilerServices;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe partial class UnsafeListExtensions2
    {
        public static UnsafeList<T> Create<T>(int capacity, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged
        {
            T* ptr = MemoryExposed.AllocateList<T>(capacity, allocator, out nint actualCapacity);

            return new UnsafeList<T>()
            {
                Ptr = ptr,
                m_length = 0,
                m_capacity = (int)actualCapacity,
                Allocator = allocator
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureCapacity<T>(this ref UnsafeList<T> self, int capacity, bool keepOldData = true)
            where T : unmanaged
        {
            MemoryExposed.EnsureListCapacity<T>(ref ReinterpretExact<UnsafeList<T>, UntypedUnsafeListMutable>(ref self), capacity, keepOldData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureSlack<T>(this ref UnsafeList<T> self, int slack)
            where T : unmanaged
        {
            MemoryExposed.EnsureListSlack<T>(ref ReinterpretExact<UnsafeList<T>, UntypedUnsafeListMutable>(ref self), slack);
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
