using System;
using System.Runtime.CompilerServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static partial class UnsafeHashSetExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashMapHelperRef<TKey> GetHelperRef<TKey>(this ref UnsafeHashSet<TKey> self)
            where TKey : unmanaged, IEquatable<TKey>
        {
            return HashMapHelperRef<TKey>.CreateFor(ref self);
        }
    }
}
