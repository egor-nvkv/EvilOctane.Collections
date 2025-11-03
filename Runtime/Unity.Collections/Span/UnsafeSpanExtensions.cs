using System;
using System.Runtime.CompilerServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe partial class UnsafeSpanExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ByteSpan AsByteSpan(this UnsafeSpan<byte> self)
        {
            return new ByteSpan(self.Ptr, self.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T, U>(this UnsafeSpan<T> self, U value)
            where T : unmanaged, IEquatable<U>
        {
            return NativeArrayExtensions.IndexOf<T, U>(self.Ptr, self.Length, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T, U>(this UnsafeSpan<T> self, U value)
            where T : unmanaged, IEquatable<U>
        {
            return NativeArrayExtensions.Contains<T, U>(self.Ptr, self.Length, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<T>(this UnsafeSpan<T> self, T value)
            where T : unmanaged, IComparable<T>
        {
            return NativeSortExtension.BinarySearch(self.Ptr, self.Length, value);
        }
    }
}
