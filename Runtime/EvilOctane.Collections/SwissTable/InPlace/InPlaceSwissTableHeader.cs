using System;
using System.Runtime.InteropServices;

namespace EvilOctane.Collections.LowLevel.Unsafe
{
    [StructLayout(LayoutKind.Sequential)]
    public struct InPlaceSwissTableHeader<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        public int Count;
        public int CapacityCeilGroupSize;
    }
}
