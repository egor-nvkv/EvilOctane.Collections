using System;
using System.Runtime.CompilerServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static partial class UnsafeHashMapExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashMapHelperRef<TKey> GetHelperRef<TKey, TValue>(this ref UnsafeHashMap<TKey, TValue> self)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return HashMapHelperRef<TKey>.CreateFor(ref self);
        }
    }
}
