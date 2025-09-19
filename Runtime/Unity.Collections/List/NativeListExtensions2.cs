using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.Unsafe;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe class NativeListExtensions2
    {
        public static NativeList<T> Create<T>(int capacity, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged
        {
            SkipInit(out NativeList<T> result);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.CheckAllocator(allocator.Handle);

            result.m_Safety = CollectionHelper.CreateSafetyHandle(allocator.Handle);
            CollectionHelper.InitNativeContainer<T>(result.m_Safety);

            CollectionHelper.SetStaticSafetyId<NativeList<T>>(ref result.m_Safety, ref NativeList<T>.s_staticSafetyId.Data);

            result.m_SafetyIndexHint = allocator.Handle.AddSafetyHandle(result.m_Safety);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(result.m_Safety, true);
#endif

            result.m_ListData = (UnsafeList<T>*)Memory.Unmanaged.Allocate(sizeof(UnsafeList<T>), UnsafeUtility.AlignOf<UnsafeList<T>>(), allocator);
            *result.m_ListData = UnsafeListExtensions2.Create<T>(capacity, allocator);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureCapacity<T>(this ref NativeList<T> self, int capacity)
            where T : unmanaged
        {
            if (capacity > self.Capacity)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(self.m_Safety);
#endif
                UnsafeListExtensions2.EnsureCapacity(ref *self.GetUnsafeList(), capacity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureSlack<T>(this ref NativeList<T> self, int slack)
            where T : unmanaged
        {
            EnsureCapacity(ref self, self.Length + slack);
        }
    }
}
