namespace Unity.Collections.LowLevel.Unsafe
{
    public struct InlineListHeader<T>
        where T : unmanaged
    {
        public int Length;
        public int Capacity;
    }
}
