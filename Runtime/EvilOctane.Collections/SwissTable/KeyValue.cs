using System.Runtime.InteropServices;

namespace EvilOctane.Collections
{
    [StructLayout(LayoutKind.Sequential)]
    public struct KeyValue<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;

        public KeyValue(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
}
