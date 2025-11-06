using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using static System.Runtime.CompilerServices.Unsafe;

namespace EvilOctane.Collections
{
    public unsafe struct XXH3StringHasher<T> : IHasher64<T>
        where T : unmanaged, INativeList<byte>, IUTF8Bytes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ulong CalculateHash(in T value)
        {
            ref T asRef = ref AsRef(in value);
            uint2 hash = xxHash3.Hash64(asRef.GetUnsafePtr(), asRef.Length);
            return ReadUnaligned<ulong>(&hash);
        }
    }
}
