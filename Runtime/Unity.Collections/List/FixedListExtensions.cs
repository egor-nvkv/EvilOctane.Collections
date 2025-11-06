using System.Runtime.CompilerServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe partial class FixedListExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeSpan<T> AsSpan<T>(this ref FixedList32Bytes<T> self)
            where T : unmanaged
        {
            return new UnsafeSpan<T>((T*)self.Buffer, self.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeSpan<T> AsSpan<T>(this ref FixedList64Bytes<T> self)
            where T : unmanaged
        {
            return new UnsafeSpan<T>((T*)self.Buffer, self.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeSpan<T> AsSpan<T>(this ref FixedList128Bytes<T> self)
            where T : unmanaged
        {
            return new UnsafeSpan<T>((T*)self.Buffer, self.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeSpan<T> AsSpan<T>(this ref FixedList512Bytes<T> self)
            where T : unmanaged
        {
            return new UnsafeSpan<T>((T*)self.Buffer, self.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeSpan<T> AsSpan<T>(this ref FixedList4096Bytes<T> self)
            where T : unmanaged
        {
            return new UnsafeSpan<T>((T*)self.Buffer, self.Length);
        }
    }
}
