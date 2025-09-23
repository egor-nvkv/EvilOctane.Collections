using System;
using System.Runtime.CompilerServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    [GenerateTestsForBurstCompatibility]
    public static unsafe partial class UnsafeSpanExtensions
    {
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T, U>(this UnsafeSpan<T> self, U value)
            where T : unmanaged, IEquatable<U>
        {
            return NativeArrayExtensions.IndexOf<T, U>(self.Ptr, self.Length, value);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T, U>(this UnsafeSpan<T> self, U value)
            where T : unmanaged, IEquatable<U>
        {
            return NativeArrayExtensions.Contains<T, U>(self.Ptr, self.Length, value);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BinarySearch<T>(this UnsafeSpan<T> self, T value)
            where T : unmanaged, IComparable<T>
        {
            return NativeSortExtension.BinarySearch(self.Ptr, self.Length, value);
        }
    }
}
