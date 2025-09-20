using System.Runtime.CompilerServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe class NativeArrayExtensions2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* GetPtr<T>(this NativeArray<T> self)
            where T : unmanaged
        {
            return (T*)self.GetUnsafePtr();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* GetReadOnlyPtr<T>(this NativeArray<T> self)
            where T : unmanaged
        {
            return (T*)self.GetUnsafeReadOnlyPtr();
        }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeSpan<T> AsSpanRW<T>(this NativeArray<T> self)
            where T : unmanaged
        {
            return new UnsafeSpan<T>((T*)self.GetUnsafePtr(), self.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeSpan<T> AsSpanRO<T>(this NativeArray<T> self)
            where T : unmanaged
        {
            return new UnsafeSpan<T>((T*)self.GetUnsafeReadOnlyPtr(), self.Length);
        }
    }
}
