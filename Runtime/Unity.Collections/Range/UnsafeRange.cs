using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static Unity.Collections.CollectionHelper2;

namespace Unity.Collections.LowLevel.Unsafe
{
    [GenerateTestsForBurstCompatibility]
    public readonly unsafe struct UnsafeRange : IEquatable<UnsafeRange>
    {
        public readonly int StartIndex;
        public readonly int Length;

        public static UnsafeRange Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(0, 0);
        }

        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Length == 0;
        }

        public readonly int EndIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => StartIndex + Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeRange(int startIndex, int length)
        {
            CheckContainerStartIndex(startIndex);
            CheckContainerLength(length);

            StartIndex = startIndex;
            Length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(UnsafeRange other)
        {
            return StartIndex == other.StartIndex & Length == other.Length;
        }

        [ExcludeFromBurstCompatTesting("Takes managed object")]
        public override readonly bool Equals(object obj)
        {
            return obj is UnsafeRange other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(StartIndex, Length);
        }

        [ExcludeFromBurstCompatTesting("Returns managed string")]
        public override readonly string ToString()
        {
            return $"(StartIndex = {StartIndex}, Length = {Length})";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly UnsafeRange Slice(int startIndex)
        {
            return Slice(startIndex, Length - startIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly UnsafeRange Slice(int startIndex, int length)
        {
            CheckSliceArgs(Length, startIndex, length);

            int resultLength = math.min(length, Length - startIndex);
            return new UnsafeRange(StartIndex + startIndex, resultLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly FixedString64Bytes ToFixedString()
        {
            FixedString64Bytes result = "(StartIndex = ";
            _ = result.Append(StartIndex);
            _ = result.Append((FixedString32Bytes)", Length = ");
            _ = result.Append(Length);
            _ = result.AppendRawByte((byte)')');
            return result;
        }
    }
}
