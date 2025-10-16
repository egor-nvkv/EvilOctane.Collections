using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EvilOctane.Collections
{
    [StructLayout(LayoutKind.Sequential)]
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
