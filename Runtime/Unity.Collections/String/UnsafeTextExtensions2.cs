using System;
using System.Runtime.CompilerServices;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe partial class UnsafeTextExtensions2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref UnsafeList<byte> AsUnsafeList(this ref UnsafeText self)
        {
            return ref ReinterpretExact<UntypedUnsafeList, UnsafeList<byte>>(ref self.m_UntypedListData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeText Create(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            UnsafeList<byte> list = UnsafeListExtensions2.Create<byte>(capacity + 1, allocator);

            list.m_length = 1;
            list.Ptr[0] = 0x0;

            return ReinterpretExact<UnsafeList<byte>, UnsafeText>(ref list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeText Create(ByteSpan source, AllocatorManager.AllocatorHandle allocator)
        {
            UnsafeList<byte> list = UnsafeListExtensions2.Create<byte>(source.Length + 1, allocator);

            list.m_length = source.Length + 1;
            new ByteSpan(list.Ptr, source.Length).CopyFrom(source);
            list.Ptr[source.Length] = 0x0;

            return ReinterpretExact<UnsafeList<byte>, UnsafeText>(ref list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeText Create(ReadOnlySpan<char> source, AllocatorManager.AllocatorHandle allocator)
        {
            int maxCapacity =
                4 + // BOM
                (source.Length * 3); // 2-byte codepoint -> 3-byte replacement

            UnsafeList<byte> list = UnsafeListExtensions2.Create<byte>(maxCapacity + 1, allocator);
            int byteCount;

            fixed (char* sourcePtr = source)
            {
                _ = Unicode.Utf16ToUtf8(sourcePtr, source.Length, list.Ptr, out byteCount, maxCapacity);
            }

            list.m_length = byteCount + 1;
            list.Ptr[byteCount] = 0x0;

            return ReinterpretExact<UnsafeList<byte>, UnsafeText>(ref list);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureCapacity(this ref UnsafeText self, int capacity)
        {
            MemoryExposed.EnsureListCapacity<byte>(ref ReinterpretExact<UnsafeText, UntypedUnsafeListMutable>(ref self), capacity + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureSlack(this ref UnsafeText self, int slack)
        {
            EnsureCapacity(ref self, self.Length + slack);
        }
    }
}
