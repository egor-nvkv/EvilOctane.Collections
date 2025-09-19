using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe class UnsafeTextExtensions2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref UnsafeList<byte> AsUnsafeList(this ref UnsafeText self)
        {
            return ref UnsafeUtility.As<UntypedUnsafeList, UnsafeList<byte>>(ref self.m_UntypedListData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeText Create(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            UnsafeList<byte> list = UnsafeListExtensions2.Create<byte>(capacity + 1, allocator);

            list.m_length = 1;
            list.Ptr[0] = 0x0;

            return UnsafeUtility.As<UnsafeList<byte>, UnsafeText>(ref list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeText Create(ReadOnlySpan<char> source, AllocatorManager.AllocatorHandle allocator)
        {
            int maxCapacity = source.Length * 3; // 2-byte codepoint -> replacement

            UnsafeList<byte> list = UnsafeListExtensions2.Create<byte>(maxCapacity + 1, allocator);
            int byteCount = Encoding.UTF8.GetBytes(source, new Span<byte>(list.Ptr, maxCapacity));

            list.m_length = byteCount + 1;
            list.Ptr[byteCount] = 0x0;

            return UnsafeUtility.As<UnsafeList<byte>, UnsafeText>(ref list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureCapacity(this ref UnsafeText self, int capacity)
        {
            MemoryExposed.EnsureListCapacity<byte>(ref UnsafeUtility.As<UnsafeText, UntypedUnsafeListMutable>(ref self), capacity + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureSlack(this ref UnsafeText self, int slack)
        {
            EnsureCapacity(ref self, self.Length + slack);
        }
    }
}
