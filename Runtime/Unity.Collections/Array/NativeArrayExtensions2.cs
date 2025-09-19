using System.Runtime.CompilerServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe class NativeArrayExtensions2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ElementAt<T>(this NativeArray<T> self, int index)
            where T : unmanaged
        {
            CollectionHelper.CheckIndexInRange(index, self.Length);
            return ref ((T*)self.GetUnsafePtr())[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ElementAtReadonly<T>(this NativeArray<T> self, int index)
            where T : unmanaged
        {
            CollectionHelper.CheckIndexInRange(index, self.Length);
            return ref ((T*)self.GetUnsafeReadOnlyPtr())[index];
        }
    }
}
