using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    public readonly unsafe struct Pointer<T>
        where T : unmanaged
    {
        public readonly T* Ptr;

        public readonly bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Ptr == null;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public ref T Ref => ref *Ptr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Pointer(T* value)
        {
            Ptr = value;
        }

        public override readonly string ToString()
        {
            return Ptr == null ? "null" : Ptr->ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Pointer<T>(T* value)
        {
            return new Pointer<T>(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T*(Pointer<T> self)
        {
            return self.Ptr;
        }
    }
}
