using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace EvilOctane.Collections
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct KeyValueRef<TKey, TValue>
        where TKey : unmanaged
        where TValue : unmanaged
    {
        public readonly Ref<KeyValue<TKey, TValue>> Pointer;

        public readonly ref TKey KeyRefRO
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Pointer.RefRW.Key;
        }

        public readonly ref TValue ValueRef
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Pointer.RefRW.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyValueRef(Ref<KeyValue<TKey, TValue>> pointer)
        {
            Pointer = pointer;
        }

        public override readonly string ToString()
        {
            return Pointer.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator KeyValueRef<TKey, TValue>(Ref<KeyValue<TKey, TValue>> other)
        {
            return new KeyValueRef<TKey, TValue>(other);
        }
    }
}
