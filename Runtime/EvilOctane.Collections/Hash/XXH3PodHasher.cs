using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace EvilOctane.Collections
{
    public unsafe struct XXH3PodHasher<T> : IHasher64<T>
        where T : unmanaged
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ulong CalculateHash(in T value)
        {
            fixed (T* ptr = &value)
            {
                uint2 hash = xxHash3.Hash64(ptr, sizeof(T));
                return hash.x | ((ulong)hash.y << 32);
            }
        }
    }
}
