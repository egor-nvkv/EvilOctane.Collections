using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
    public readonly unsafe struct Pointer<T>
        where T : unmanaged
    {
        public readonly T* Value;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public ref T Ref => ref *Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Pointer(T* value)
        {
            Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Pointer<T>(T* value)
        {
            return new Pointer<T>(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator T*(Pointer<T> self)
        {
            return self.Value;
        }
    }
}
