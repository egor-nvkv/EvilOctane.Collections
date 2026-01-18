using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace EvilOctane.Collections
{
    public readonly struct KeyValueRef<TKey, TValue>
        where TKey : unmanaged
        where TValue : unmanaged
    {
        public readonly Pointer<KeyValue<TKey, TValue>> Pointer;

        public readonly ref TKey KeyRefRO
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Pointer.AsRef.Key;
        }

        public readonly ref TValue ValueRef
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Pointer.AsRef.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyValueRef(Pointer<KeyValue<TKey, TValue>> pointer)
        {
            Pointer = pointer;
        }

        public override readonly string ToString()
        {
            return Pointer.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator KeyValueRef<TKey, TValue>(Pointer<KeyValue<TKey, TValue>> other)
        {
            return new KeyValueRef<TKey, TValue>(other);
        }
    }
}
