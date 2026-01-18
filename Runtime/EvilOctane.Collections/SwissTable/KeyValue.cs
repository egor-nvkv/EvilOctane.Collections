using System.Runtime.CompilerServices;

namespace EvilOctane.Collections
{
    public struct KeyValue<TKey, TValue>
        where TKey : unmanaged
        where TValue : unmanaged
    {
        public TKey Key;
        public TValue Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyValue(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }

        public override readonly string ToString()
        {
            return $"[{Key}, {Value}]";
        }
    }
}
