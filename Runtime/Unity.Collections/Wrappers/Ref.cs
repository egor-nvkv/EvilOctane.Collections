using System.Diagnostics;
using System.Runtime.CompilerServices;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;

namespace Unity.Collections.LowLevel.Unsafe
{
    public readonly unsafe struct Ref<T>
        where T : unmanaged
    {
        public readonly T* Ptr;

        public readonly bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Ptr == null;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly ref T RefRW
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckPointerIsNotNull(Ptr);
                return ref *Ptr;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly ref readonly T RefRO
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckPointerIsNotNull(Ptr);
                return ref *Ptr;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Ref(T* ptr)
        {
            Ptr = ptr;
        }

        public override readonly string ToString()
        {
            return Ptr == null ? "null" : Ptr->ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Ref<T>(T* value)
        {
            return new Ref<T>(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T*(Ref<T> self)
        {
            return self.Ptr;
        }
    }
}
