using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Mathematics;
using static Unity.Collections.CollectionHelper;
using static Unity.Collections.CollectionHelper2;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;
using SystemUnsafe = System.Runtime.CompilerServices.Unsafe;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// Unmanaged version of <see cref="Span{T}"/>.
    /// </summary>
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(UnsafeSpanTDebugView<>))]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
    public readonly unsafe struct UnsafeSpan<T> : INativeList<T>, IEnumerable<T>
        where T : unmanaged
    {
        public const int MaxCapacity = int.MaxValue;

        [NativeDisableUnsafePtrRestriction]
        public readonly T* Ptr;
        public readonly int LengthField;

        public static UnsafeSpan<T> Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(null, 0);
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => AssumePositive(LengthField);
            [Obsolete("UnsafeSpan is immutable.", true)]
            set => throw new NotSupportedException();
        }

        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Length;
            [Obsolete("UnsafeSpan is immutable.", true)]
            set => throw new NotSupportedException();
        }

        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Length == 0;
        }

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                CheckIndexInRange(index, LengthField);
                return Ptr[index];
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                CheckIndexInRange(index, LengthField);
                Ptr[index] = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeSpan(T* ptr, int length)
        {
            CheckContainerLength(length);
            CheckPtr(ptr, length);

            Ptr = ptr;
            LengthField = length;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckPtr(T* ptr, int length)
        {
            if (ptr == null && (uint)length > 0)
            {
                throw new ArgumentException("Ptr cannot be null with non-zero length.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckCopyLengths(int srcLength, int dstLength)
        {
            if (srcLength != dstLength)
            {
                throw new ArgumentException("Source and Destination length must be the same.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref T ElementAt(int index)
        {
            CheckIndexInRange(index, LengthField);
            return ref Ptr[index];
        }

        [Obsolete("UnsafeSpan is immutable.", true)]
        public readonly void Clear()
        {
            throw new NotSupportedException();
        }

        readonly IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly int GetHashCode()
        {
            HashCode hashCode = new();

            for (int index = 0; index != Length; ++index)
            {
                hashCode.Add(Ptr[index]);
            }

            return hashCode.ToHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Enumerator GetEnumerator()
        {
            return new Enumerator(Ptr, Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly UnsafeSpan<T> Slice(int startIndex)
        {
            int resultLength = Length - startIndex;
            CheckSliceArgs(startIndex, resultLength);

            return new UnsafeSpan<T>(Ptr + startIndex, resultLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly UnsafeSpan<T> Slice(int startIndex, int length)
        {
            int resultLength = math.min(length, Length - startIndex);
            CheckSliceArgs(startIndex, resultLength);

            return new UnsafeSpan<T>(Ptr + startIndex, resultLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly UnsafeSpan<U> Reinterpret<U>()
            where U : unmanaged
        {
            CheckReinterpretArgs<T, U>();
            return new UnsafeSpan<U>((U*)Ptr, Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly NativeArray<T> AsNativeArray()
        {
            return ConvertExistingDataToNativeArray<T>(Ptr, Length, Allocator.None, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator)
        {
            NativeArray<T> array = CreateNativeArray<T>(Length, allocator, NativeArrayOptions.UninitializedMemory);
            array.AsSpanRW().CopyFrom(this);
            return array;
        }

        [ExcludeFromBurstCompatTesting("Returns managed object")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T[] ToArray()
        {
            return ((ReadOnlySpan<T>)this).ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void MemClear()
        {
            int byteCount = Length * sizeof(T);

            if (Constant.IsConstantExpression(true))
            {
                // Burst
                UnsafeUtility.MemClear(Ptr, byteCount);
            }
            else
            {
                // No Burst
                SystemUnsafe.InitBlock(Ptr, 0, (uint)byteCount);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Fill(T value)
        {
            if (sizeof(T) == sizeof(byte))
            {
                // Byte

                if (Constant.IsConstantExpression(true))
                {
                    // Burst
                    UnsafeUtility.MemSet(Ptr, *(byte*)&value, Length);
                }
                else
                {
                    // No Burst
                    SystemUnsafe.InitBlock(Ptr, *(byte*)&value, (uint)Length);
                }
            }
            else if (((32 % sizeof(T)) == 0) || ((sizeof(T) % 32) == 0))
            {
                // Vectorizable

                for (int index = 0; index != Length; ++index)
                {
                    Ptr[index] = value;
                }
            }
            else
            {
                // Generic
                UnsafeUtility.MemCpyReplicate(Ptr, &value, sizeof(T), Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void CopyFrom(UnsafeSpan<T> other)
        {
            CheckCopyLengths(srcLength: other.LengthField, dstLength: LengthField);

            int byteCount = Length * sizeof(T);

            if (Constant.IsConstantExpression(true))
            {
                // Burst
                UnsafeUtility.MemCpy(Ptr, other.Ptr, byteCount);
            }
            else
            {
                // No Burst
                SystemUnsafe.CopyBlock(Ptr, other.Ptr, (uint)byteCount);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckSliceArgs(int startIndex, int length)
        {
            if (startIndex < 0)
            {
                throw new ArgumentOutOfRangeException($"StartIndex {startIndex} must be non-negative.");
            }

            if (startIndex > Length)
            {
                throw new ArgumentOutOfRangeException($"StartIndex {startIndex} cannot be larger than length {Length}.");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException($"Length {length} must be non-negative.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<T>(UnsafeSpan<T> self)
        {
            return new Span<T>(self.Ptr, self.LengthField);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<T>(UnsafeSpan<T> self)
        {
            return new ReadOnlySpan<T>(self.Ptr, self.LengthField);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly T* ptr;
            private readonly int length;
            private int index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(T* ptr, int length)
            {
                this.ptr = ptr;
                this.length = length;
                index = -1;
            }

            public readonly void Dispose()
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return ++index != length;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                index = -1;
            }

            public readonly T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ptr[index];
            }

            readonly object IEnumerator.Current => Current;
        }
    }

    internal sealed class UnsafeSpanTDebugView<T>
        where T : unmanaged
    {
        private readonly UnsafeSpan<T> data;

        public UnsafeSpanTDebugView(UnsafeSpan<T> data)
        {
            this.data = data;
        }

        public T[] Items => data.ToArray();
    }
}
