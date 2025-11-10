using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using static System.Runtime.CompilerServices.Unsafe;
using static Unity.Collections.CollectionHelper2;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryResizeBurstOptimized<T>(this ref T self, int newLength)
            where T : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            CheckContainerLength(newLength);

            if (Constant.IsConstantExpression(true))
            {
                // Burst

                if (BurstRuntime.GetHashCode64<T>() == BurstRuntime.GetHashCode64<UnsafeText>())
                {
                    ref UnsafeText unsafeText = ref ReinterpretExact<T, UnsafeText>(ref self);
                    ref UnsafeList<byte> unsafeList = ref unsafeText.AsUnsafeList();

                    unsafeList.EnsureCapacity(newLength + 1);
                    unsafeList.m_length = newLength + 1;

                    unsafeList[newLength] = 0x0;
                    return true;
                }
                else if (BurstRuntime.GetHashCode64<T>() == BurstRuntime.GetHashCode64<NativeText>())
                {
                    ref NativeText nativeText = ref ReinterpretExact<T, NativeText>(ref self);
                    ref UnsafeList<byte> unsafeList = ref nativeText.GetUnsafeText()->AsUnsafeList();

                    unsafeList.EnsureCapacity(newLength + 1);
                    unsafeList.m_length = newLength + 1;

                    unsafeList[newLength] = 0x0;
                    return true;
                }
            }

            return self.TryResize(newLength, NativeArrayOptions.UninitializedMemory);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FormatError Append<S0, S1, S2>(this ref S0 str0, in S1 str1, in S2 str2)
            where S0 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S1 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S2 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            ref S1 str1Ref = ref AsRef(in str1);
            ref S2 str2Ref = ref AsRef(in str2);

            int oldLength = str0.Length;

            int extraLength =
                str1Ref.Length +
                str2Ref.Length;

            if (Hint.Unlikely(!str0.TryResize(oldLength + extraLength, NativeArrayOptions.UninitializedMemory)))
            {
                return FormatError.Overflow;
            }

            int offset = oldLength;

            // Copy 1
            new ByteSpan(str0.GetUnsafePtr() + offset, str1Ref.Length).CopyFrom(str1Ref.AsByteSpan());
            offset += str1Ref.Length;

            // Copy 2
            new ByteSpan(str0.GetUnsafePtr() + offset, str2Ref.Length).CopyFrom(str2Ref.AsByteSpan());

            return FormatError.None;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FormatError Append<S0, S1, S2, S3>(this ref S0 str0, in S1 str1, in S2 str2, in S3 str3)
            where S0 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S1 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S2 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S3 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            ref S1 str1Ref = ref AsRef(in str1);
            ref S2 str2Ref = ref AsRef(in str2);
            ref S3 str3Ref = ref AsRef(in str3);

            int oldLength = str0.Length;

            int extraLength =
                str1Ref.Length +
                str2Ref.Length +
                str3Ref.Length;

            if (Hint.Unlikely(!str0.TryResize(oldLength + extraLength, NativeArrayOptions.UninitializedMemory)))
            {
                return FormatError.Overflow;
            }

            int offset = oldLength;

            // Copy 1
            new ByteSpan(str0.GetUnsafePtr() + offset, str1Ref.Length).CopyFrom(str1Ref.AsByteSpan());
            offset += str1Ref.Length;

            // Copy 2
            new ByteSpan(str0.GetUnsafePtr() + offset, str2Ref.Length).CopyFrom(str2Ref.AsByteSpan());
            offset += str2Ref.Length;

            // Copy 3
            new ByteSpan(str0.GetUnsafePtr() + offset, str3Ref.Length).CopyFrom(str3Ref.AsByteSpan());

            return FormatError.None;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FormatError Append<S0, S1, S2, S3, S4>(this ref S0 str0, in S1 str1, in S2 str2, in S3 str3, in S4 str4)
            where S0 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S1 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S2 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S3 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S4 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            ref S1 str1Ref = ref AsRef(in str1);
            ref S2 str2Ref = ref AsRef(in str2);
            ref S3 str3Ref = ref AsRef(in str3);
            ref S4 str4Ref = ref AsRef(in str4);

            int oldLength = str0.Length;

            int extraLength =
                str1Ref.Length +
                str2Ref.Length +
                str3Ref.Length +
                str4Ref.Length;

            if (Hint.Unlikely(!str0.TryResize(oldLength + extraLength, NativeArrayOptions.UninitializedMemory)))
            {
                return FormatError.Overflow;
            }

            int offset = oldLength;

            // Copy 1
            new ByteSpan(str0.GetUnsafePtr() + offset, str1Ref.Length).CopyFrom(str1Ref.AsByteSpan());
            offset += str1Ref.Length;

            // Copy 2
            new ByteSpan(str0.GetUnsafePtr() + offset, str2Ref.Length).CopyFrom(str2Ref.AsByteSpan());
            offset += str2Ref.Length;

            // Copy 3
            new ByteSpan(str0.GetUnsafePtr() + offset, str3Ref.Length).CopyFrom(str3Ref.AsByteSpan());
            offset += str3Ref.Length;

            // Copy 4
            new ByteSpan(str0.GetUnsafePtr() + offset, str4Ref.Length).CopyFrom(str4Ref.AsByteSpan());

            return FormatError.None;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FormatError Append<S0, S1, S2, S3, S4, S5>(this ref S0 str0, in S1 str1, in S2 str2, in S3 str3, in S4 str4, in S5 str5)
            where S0 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S1 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S2 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S3 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S4 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S5 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            ref S1 str1Ref = ref AsRef(in str1);
            ref S2 str2Ref = ref AsRef(in str2);
            ref S3 str3Ref = ref AsRef(in str3);
            ref S4 str4Ref = ref AsRef(in str4);
            ref S5 str5Ref = ref AsRef(in str5);

            int oldLength = str0.Length;

            int extraLength =
                str1Ref.Length +
                str2Ref.Length +
                str3Ref.Length +
                str4Ref.Length +
                str5Ref.Length;

            if (Hint.Unlikely(!str0.TryResize(oldLength + extraLength, NativeArrayOptions.UninitializedMemory)))
            {
                return FormatError.Overflow;
            }

            int offset = oldLength;

            // Copy 1
            new ByteSpan(str0.GetUnsafePtr() + offset, str1Ref.Length).CopyFrom(str1Ref.AsByteSpan());
            offset += str1Ref.Length;

            // Copy 2
            new ByteSpan(str0.GetUnsafePtr() + offset, str2Ref.Length).CopyFrom(str2Ref.AsByteSpan());
            offset += str2Ref.Length;

            // Copy 3
            new ByteSpan(str0.GetUnsafePtr() + offset, str3Ref.Length).CopyFrom(str3Ref.AsByteSpan());
            offset += str3Ref.Length;

            // Copy 4
            new ByteSpan(str0.GetUnsafePtr() + offset, str4Ref.Length).CopyFrom(str4Ref.AsByteSpan());
            offset += str4Ref.Length;

            // Copy 5
            new ByteSpan(str0.GetUnsafePtr() + offset, str5Ref.Length).CopyFrom(str5Ref.AsByteSpan());

            return FormatError.None;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendTruncateUnchecked<S0, S1>(this ref S0 str0, in S1 str1)
            where S0 : unmanaged, INativeList<byte>, IUTF8Bytes
            where S1 : unmanaged, INativeList<byte>, IUTF8Bytes
        {
            ref S1 str1Ref = ref AsRef(in str1);

            int oldLength = str0.Length;
            int slack = str0.Capacity - str0.Length;

            int countClamped = math.min(str1Ref.Length, slack);
            str0.Length = oldLength + countClamped;

            ByteSpan str1Span = new(str1Ref.GetUnsafePtr(), countClamped);
            new ByteSpan(str0.GetUnsafePtr() + oldLength, countClamped).CopyFrom(str1Span);
        }
    }
}
