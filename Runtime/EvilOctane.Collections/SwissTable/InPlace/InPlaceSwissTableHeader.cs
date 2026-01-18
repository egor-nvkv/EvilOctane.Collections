using System;
using System.Runtime.CompilerServices;

namespace EvilOctane.Collections.LowLevel.Unsafe
{
    public struct InPlaceSwissTableHeader<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        public int Count;
        public int CapacityCeilGroupSize;

        public readonly bool IsFull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => SwissTable.IsFull(CapacityCeilGroupSize, Count);
        }
    }
}
