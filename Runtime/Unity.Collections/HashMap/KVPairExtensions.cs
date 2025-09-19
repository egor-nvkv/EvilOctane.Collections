using System;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe class KVPairExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AssumeIndexIsValid<TKey, TValue>(this KVPair<TKey, TValue> self)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            CollectionHelper.CheckIndexInRange(self.m_Index, self.m_Data->Capacity);
            Hint.Assume(self.m_Index != -1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TKey GetKeyReadOnlyRef<TKey, TValue>(this KVPair<TKey, TValue> self)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (self.m_Index == -1)
            {
                throw new ArgumentException("must be valid");
            }
#endif

            return ref self.m_Data->Keys[self.m_Index];
        }
    }
}
