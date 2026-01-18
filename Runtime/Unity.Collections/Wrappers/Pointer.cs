using System.Diagnostics;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.Unsafe;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;

namespace Unity.Collections.LowLevel.Unsafe
{
    public readonly unsafe struct Pointer<T>
        where T : unmanaged
    {
        public readonly T* Value;

        public readonly bool IsNull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Value == null;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly ref T AsRef
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckPointerIsNotNull(Value);
                return ref *Value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Pointer(ref T reference)
        {
            Value = (T*)AsPointer(ref reference);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Pointer(T* pointer)
        {
            Value = pointer;
        }

        public override readonly string ToString()
        {
            return Value == null ? "null" : Value->ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Pointer<T>(T* other)
        {
            return new Pointer<T>(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T*(Pointer<T> self)
        {
            return self.Value;
        }
    }
}
