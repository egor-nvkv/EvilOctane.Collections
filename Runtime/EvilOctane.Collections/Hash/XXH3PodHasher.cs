using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace EvilOctane.Collections
{
    public unsafe struct XXH3PodHasher<T> : IHasher64<T>
        where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ulong CalculateHash(T value)
        {
            uint2 hash = xxHash3.Hash64(&value, sizeof(T));
            return hash.x | ((ulong)hash.y << 32);
        }
    }
}
