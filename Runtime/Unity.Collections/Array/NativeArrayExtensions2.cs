using System.Runtime.CompilerServices;
using UnityEngine;
using static Unity.Collections.CollectionHelper2;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe partial class NativeArrayExtensions2
    {
        [HideInCallstack]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* GetPtr<T>(this NativeArray<T> self)
            where T : unmanaged
        {
            return (T*)self.GetUnsafePtr();
        }

        [HideInCallstack]
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
            CheckContainerIndexInRange(index, self.Length);
            return ref ((T*)self.GetUnsafePtr())[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ElementAtReadonly<T>(this NativeArray<T> self, int index)
            where T : unmanaged
        {
            CheckContainerIndexInRange(index, self.Length);
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
