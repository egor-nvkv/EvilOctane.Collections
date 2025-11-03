using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace EvilOctane.Collections
{
    public unsafe struct XXH3StringHasher<T> : IHasher64<T>
        where T : unmanaged, INativeList<byte>, IUTF8Bytes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ulong CalculateHash(in T value)
        {
            uint2 hash = xxHash3.Hash64(value.GetUnsafePtr(), value.Length);
            return hash.x | ((ulong)hash.y << 32);
        }
    }
}
