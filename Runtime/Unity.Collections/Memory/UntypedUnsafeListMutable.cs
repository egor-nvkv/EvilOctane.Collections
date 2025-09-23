using System.Runtime.InteropServices;

namespace Unity.Collections.LowLevel.Unsafe
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UntypedUnsafeListMutable
    {
#pragma warning disable 169
        // <WARNING>
        // 'Header' of this struct must binary match `UntypedUnsafeList`, `UnsafeList`, `UnsafePtrList`, and `NativeArray` struct.
        [NativeDisableUnsafePtrRestriction]
        public void* Ptr;
        public int m_length;
        public int m_capacity;
        public AllocatorManager.AllocatorHandle Allocator;
        public int padding;
#pragma warning restore 169
    }
}
