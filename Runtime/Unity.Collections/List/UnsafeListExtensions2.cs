using System.Runtime.CompilerServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe class UnsafeListExtensions2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeList<T> Create<T>(int capacity, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged
        {
            T* ptr = MemoryExposed.AllocateList<T>(capacity, allocator, out int actualCapacity);

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
            MemoryExposed.EnsureListCapacity<T>(ref UnsafeUtility.As<UnsafeList<T>, UntypedUnsafeListMutable>(ref self), capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureSlack<T>(this ref UnsafeList<T> self, int slack)
            where T : unmanaged
        {
            MemoryExposed.EnsureListSlack<T>(ref UnsafeUtility.As<UnsafeList<T>, UntypedUnsafeListMutable>(ref self), slack);
        }
    }
}
