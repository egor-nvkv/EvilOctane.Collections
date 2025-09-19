using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using static System.Runtime.CompilerServices.Unsafe;

namespace Unity.Collections
{
    public static unsafe class NativeTextExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeText* GetUnsafeText(this NativeText self)
        {
            return self.m_Data;
        }

        public static NativeText Create(int capacity, AllocatorManager.AllocatorHandle allocator)
        {
            SkipInit(out NativeText result);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.CheckAllocator(allocator);

            result.m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
            CollectionHelper.SetStaticSafetyId(ref result.m_Safety, ref NativeText.s_staticSafetyId.Data, "Unity.Collections.NativeText");

            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(result.m_Safety, true);
#endif
            result.m_Data = UnsafeText.Alloc(allocator);
            *result.m_Data = UnsafeTextExtensions2.Create(capacity, allocator);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureCapacity(this NativeText self, int capacity)
        {
            UnsafeTextExtensions2.EnsureCapacity(ref *self.m_Data, capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureSlack(this NativeText self, int slack)
        {
            UnsafeTextExtensions2.EnsureSlack(ref *self.m_Data, slack);
        }
    }
}
