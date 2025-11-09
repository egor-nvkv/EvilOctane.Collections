using System.Runtime.CompilerServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe partial class NativeReferenceExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetRef<T>(this NativeReference<T> self)
            where T : unmanaged
        {
            return ref *self.GetUnsafePtr();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T GetRefReadOnly<T>(this NativeReference<T> self)
            where T : unmanaged
        {
            return ref *self.GetUnsafeReadOnlyPtr();
        }
    }
}
