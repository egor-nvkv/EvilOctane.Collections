namespace EvilOctane.Collections.LowLevel.Unsafe
{
    public struct InPlaceListHeader<T>
        where T : unmanaged
    {
        public int Length;
        public int Capacity;
    }
}
