using System;

namespace Unity.Collections.LowLevel.Unsafe
{
    public struct InlineHashMapHeader<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        public int Count;
        public int Capacity;
        public int BucketCapacity;
    }
}
