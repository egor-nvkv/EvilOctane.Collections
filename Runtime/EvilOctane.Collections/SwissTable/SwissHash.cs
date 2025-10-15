using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;

namespace EvilOctane.Collections
{
    public unsafe struct SwissHash
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong H1(ulong hash)
        {
            return hash >> 7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte H2(ulong hash)
        {
            return (byte)(hash & SwissTable.ControlFullMask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint H1Compressed(ulong h1)
        {
            return (uint)Reinterpret<ulong, uint2>(ref h1).GetHashCode();
        }
    }
}
