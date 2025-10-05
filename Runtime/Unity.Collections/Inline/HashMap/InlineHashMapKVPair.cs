using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.Unsafe;

namespace Unity.Collections.LowLevel.Unsafe
{
    [DebuggerDisplay("Key = {Key}, Value = {ValueOpt}")]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
    public unsafe struct InlineHashMapKVPair<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal InlineHashMapHeader<TKey>* header;
        internal int index;
        internal int next;

        public static KVPair<TKey, TValue> Null => new() { m_Index = -1 };

        public readonly TKey Key
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => index >= 0 ? KeyRefRO : new();
        }

        public readonly ref readonly TKey KeyRefRO
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CollectionHelper.CheckIndexInRange(index, header == null ? 0 : header->Count);
                return ref InlineHashMap<TKey, TValue>.GetKeyPtr(header)[index];
            }
        }

        public readonly ref TValue ValueRefRW
        {
            get
            {
                CollectionHelper.CheckIndexInRange(index, header == null ? 0 : header->Count);
                return ref InlineHashMap<TKey, TValue>.GetValuePtr(header)[index];
            }
        }

        public readonly TValue? ValueOpt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => index >= 0 ? ValueRefRW : new();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool GetKeyValue(out TKey key, out TValue value)
        {
            if (index < 0)
            {
                SkipInit(out key);
                SkipInit(out value);
                return false;
            }

            key = Key;
            value = ValueRefRW;
            return true;
        }
    }
}
