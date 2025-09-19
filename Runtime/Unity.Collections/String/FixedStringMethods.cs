using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using BurstRuntime = Unity.Burst.BurstRuntime;

namespace Unity.Collections
{
    public static unsafe partial class FixedStringMethods
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ByteSpan AsByteSpan<T>(this ref T self)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            return new ByteSpan(self.GetUnsafePtr(), self.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ByteSpan AsByteSpan(this NativeArray<byte> self)
        {
            return new ByteSpan((byte*)self.GetUnsafePtr(), self.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ByteSpan AsByteSpan(this UnsafeList<byte> self)
        {
            return new ByteSpan(self.Ptr, self.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ByteSpan AsByteSpan(this NativeList<byte> self)
        {
            return new ByteSpan(self.GetUnsafePtr(), self.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ByteSpan AsByteSpan(this UnsafeText self)
        {
            return (ByteSpan)self;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ByteSpan AsByteSpan(this NativeText self)
        {
            return (ByteSpan)self;
        }

        [SkipLocalsInit]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryResizeBurstOptimized<T>(this ref T self, int newLength)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            if (Constant.IsConstantExpression(true))
            {
                // Burst

                if (BurstRuntime.GetHashCode64<T>() == BurstRuntime.GetHashCode64<UnsafeText>())
                {
                    ref UnsafeText unsafeText = ref UnsafeUtility.As<T, UnsafeText>(ref self);
                    ref UnsafeList<byte> unsafeList = ref unsafeText.AsUnsafeList();

                    unsafeList.EnsureCapacity(newLength + 1);
                    unsafeList.m_length = newLength + 1;

                    unsafeList[newLength] = 0x0;
                    return true;
                }
                else if (BurstRuntime.GetHashCode64<T>() == BurstRuntime.GetHashCode64<NativeText>())
                {
                    ref NativeText nativeText = ref UnsafeUtility.As<T, NativeText>(ref self);
                    ref UnsafeList<byte> unsafeList = ref nativeText.GetUnsafeText()->AsUnsafeList();

                    unsafeList.EnsureCapacity(newLength + 1);
                    unsafeList.m_length = newLength + 1;

                    unsafeList[newLength] = 0x0;
                    return true;
                }
            }

            return self.TryResize(newLength, NativeArrayOptions.UninitializedMemory);
        }
    }
}
