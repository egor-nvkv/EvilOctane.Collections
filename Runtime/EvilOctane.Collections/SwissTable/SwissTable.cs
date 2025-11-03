using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using static EvilOctane.Collections.SwissTable;
using static System.Runtime.CompilerServices.Unsafe;
using static Unity.Burst.Intrinsics.Arm.Neon;
using static Unity.Burst.Intrinsics.X86.Bmi1;
using static Unity.Burst.Intrinsics.X86.Sse2;
using static Unity.Burst.Intrinsics.X86.Sse4_2;
using static Unity.Collections.CollectionHelper;
using static Unity.Collections.CollectionHelper2;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;

namespace EvilOctane.Collections
{
    /// <summary>
    /// SIMD compatible hash table.
    /// </summary>
    /// <remarks>
    /// <see href="https://www.youtube.com/watch?v=ncHmEUmJZf4"/>
    /// </remarks>
    public unsafe struct SwissTable
    {
        public const int GroupSize = 16;

        public const float MaxLoadFactorRcp = (float)MaxLoadFactorDenominator / MaxLoadFactorNumerator;
        public const int MaxLoadFactorNumerator = 15;
        public const int MaxLoadFactorDenominator = 16;

        public const byte ControlEmpty = 0x80;
        public const byte ControlDeleted = 0xfe;
        public const byte ControlSentinel = 0xff;
        public const byte ControlFullMask = 0x7f;

        public static int MaxCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (int)(Align((long)int.MaxValue, GroupSize) - GroupSize);
        }

        public static int ControlAlignment
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GroupSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong H1(ulong hash)
        {
            return hash >> 7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte H2(ulong hash)
        {
            return (byte)(hash & ControlFullMask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint H1Compressed(ulong h1)
        {
            uint hash = (uint)(h1 >> 32) ^ (uint)h1;
            return hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetEmptyMask(byte* buffer, int index)
        {
            return GetH2Mask(buffer, index, ControlEmpty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetFullMask(byte* buffer, int index)
        {
            CheckIsAligned(buffer, ControlAlignment);
            Hint.Assume(IsAligned(buffer, GroupSize));

            byte* controlPtr = buffer + index;

            if (IsSse42Supported)
            {
                v128 control = load_si128(controlPtr);
                uint notFullMask = (uint)movemask_epi8(control);
                return notFullMask ^ 0xffff;
            }
            else if (IsNeonSupported)
            {
                v128 control = vld1q_u8(controlPtr);
                uint notFullMask = (uint)neon_movemask_epi8(control);
                return notFullMask ^ 0xffff;
            }
            else
            {
                return (uint)(
                    ((controlPtr[0] & ~ControlFullMask) == 0 ? (0x1 << 0) : 0x0) |
                    ((controlPtr[1] & ~ControlFullMask) == 0 ? (0x1 << 1) : 0x0) |
                    ((controlPtr[2] & ~ControlFullMask) == 0 ? (0x1 << 2) : 0x0) |
                    ((controlPtr[3] & ~ControlFullMask) == 0 ? (0x1 << 3) : 0x0) |
                    ((controlPtr[4] & ~ControlFullMask) == 0 ? (0x1 << 4) : 0x0) |
                    ((controlPtr[5] & ~ControlFullMask) == 0 ? (0x1 << 5) : 0x0) |
                    ((controlPtr[6] & ~ControlFullMask) == 0 ? (0x1 << 6) : 0x0) |
                    ((controlPtr[7] & ~ControlFullMask) == 0 ? (0x1 << 7) : 0x0) |
                    ((controlPtr[8] & ~ControlFullMask) == 0 ? (0x1 << 8) : 0x0) |
                    ((controlPtr[9] & ~ControlFullMask) == 0 ? (0x1 << 9) : 0x0) |
                    ((controlPtr[10] & ~ControlFullMask) == 0 ? (0x1 << 10) : 0x0) |
                    ((controlPtr[11] & ~ControlFullMask) == 0 ? (0x1 << 11) : 0x0) |
                    ((controlPtr[12] & ~ControlFullMask) == 0 ? (0x1 << 12) : 0x0) |
                    ((controlPtr[13] & ~ControlFullMask) == 0 ? (0x1 << 13) : 0x0) |
                    ((controlPtr[14] & ~ControlFullMask) == 0 ? (0x1 << 14) : 0x0) |
                    ((controlPtr[15] & ~ControlFullMask) == 0 ? (0x1 << 15) : 0x0));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetH2Mask(byte* buffer, int index, byte h2)
        {
            CheckIsAligned(buffer, ControlAlignment);
            Hint.Assume(IsAligned(buffer, GroupSize));

            byte* controlPtr = buffer + index;

            if (IsSse42Supported)
            {
                v128 control = load_si128(controlPtr);
                v128 eqH2 = cmpeq_epi8(control, new v128(h2));
                return (uint)movemask_epi8(eqH2);
            }
            else if (IsNeonSupported)
            {
                v128 control = vld1q_u8(controlPtr);
                v128 eqH2 = vceqq_u8(control, new v128(h2));
                return (uint)neon_movemask_epi8(eqH2);
            }
            else
            {
                return (uint)(
                    (controlPtr[0] == h2 ? (0x1 << 0) : 0x0) |
                    (controlPtr[1] == h2 ? (0x1 << 1) : 0x0) |
                    (controlPtr[2] == h2 ? (0x1 << 2) : 0x0) |
                    (controlPtr[3] == h2 ? (0x1 << 3) : 0x0) |
                    (controlPtr[4] == h2 ? (0x1 << 4) : 0x0) |
                    (controlPtr[5] == h2 ? (0x1 << 5) : 0x0) |
                    (controlPtr[6] == h2 ? (0x1 << 6) : 0x0) |
                    (controlPtr[7] == h2 ? (0x1 << 7) : 0x0) |
                    (controlPtr[8] == h2 ? (0x1 << 8) : 0x0) |
                    (controlPtr[9] == h2 ? (0x1 << 9) : 0x0) |
                    (controlPtr[10] == h2 ? (0x1 << 10) : 0x0) |
                    (controlPtr[11] == h2 ? (0x1 << 11) : 0x0) |
                    (controlPtr[12] == h2 ? (0x1 << 12) : 0x0) |
                    (controlPtr[13] == h2 ? (0x1 << 13) : 0x0) |
                    (controlPtr[14] == h2 ? (0x1 << 14) : 0x0) |
                    (controlPtr[15] == h2 ? (0x1 << 15) : 0x0));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCapacityCeilGroupSize(int capacity)
        {
            CheckContainerCapacity(capacity);

            capacity = math.max(capacity, 1);
            int capacityAdjusted = (int)math.ceil(capacity * MaxLoadFactorRcp);

            int capacityCeilGroupSize = Align(capacityAdjusted, GroupSize);
            CheckCapacity(capacityCeilGroupSize);

            return capacityCeilGroupSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsFull(int capacityCeilGroupSize, int occupiedCount)
        {
            CheckCapacity(capacityCeilGroupSize);
            CheckContainerElementCount(occupiedCount);

            int allowedCount = capacityCeilGroupSize / MaxLoadFactorDenominator * MaxLoadFactorNumerator;
            return occupiedCount >= allowedCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Find<TKey, THasher>(int keyStride, byte* buffer, byte* groupPtr, int capacityCeilGroupSize, TKey key, bool checkExists, out byte h2, out bool exists)
            where TKey : unmanaged, IEquatable<TKey>
            where THasher : unmanaged, IHasher64<TKey>
        {
            CheckIsAligned(buffer, ControlAlignment);
            CheckCapacity(capacityCeilGroupSize);

            ulong hash = default(THasher).CalculateHash(in key);

            ulong h1 = H1(hash);
            h2 = H2(hash);

            int groupCount = capacityCeilGroupSize / GroupSize;
            int groupIndex = (int)(H1Compressed(h1) % (uint)groupCount);

            for (; ; groupIndex = (groupIndex + 1) % groupCount)
            {
                int startIndex = groupIndex * GroupSize;

                if (checkExists)
                {
                    uint h2Mask = GetH2Mask(buffer, startIndex, h2);

                    // Probe for h2 matches

                    while (h2Mask != 0x0)
                    {
                        int index = startIndex + math.tzcnt(h2Mask);
                        ref TKey keyRO = ref *(TKey*)(groupPtr + (index * keyStride));

                        if (Hint.Likely(key.Equals(keyRO)))
                        {
                            // Found
                            exists = true;
                            return index;
                        }

                        if (IsBmi1Supported)
                        {
                            h2Mask = blsr_u32(h2Mask);
                        }
                        else
                        {
                            // blsr
                            h2Mask = (h2Mask - 1) & h2Mask;
                        }
                    }
                }

                // Probe for empty

                uint emptyMask = GetEmptyMask(buffer, startIndex);

                if (Hint.Likely(emptyMask != 0x0))
                {
                    // Empty
                    exists = false;
                    return startIndex + math.tzcnt(emptyMask);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Delete(byte* buffer, int index, ref int occupiedCount)
        {
            CheckIsAligned(buffer, ControlAlignment);
            CheckContainerIndexInRange(index, int.MaxValue);

            int groupIndex = index / GroupSize;
            int startIndex = groupIndex * GroupSize;

            uint emptyMask = GetEmptyMask(buffer, startIndex);

            if (Hint.Likely(emptyMask != 0x0))
            {
                // Reclaim
                buffer[index] = ControlEmpty;
                --occupiedCount;
            }
            else
            {
                // Put tombstone to prevent probe termination
                buffer[index] = ControlDeleted;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckCapacity(int capacityCeilGroupSize)
        {
            CheckContainerCapacity(capacityCeilGroupSize);

            if (Hint.Unlikely(capacityCeilGroupSize / GroupSize < 1))
            {
                throw new ArgumentOutOfRangeException($"Capacity cannot be smaller than group size ({GroupSize}).");
            }

            if (Hint.Unlikely(capacityCeilGroupSize % GroupSize != 0))
            {
                throw new ArgumentException($"Capacity must be a multiple of group size ({GroupSize}).");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowKeyNotPresent()
        {
            throw new InvalidOperationException("Key is not present.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowKeyAlreadyAdded()
        {
            throw new InvalidOperationException("Key has already been added.");
        }

        /// <summary>
        /// <inheritdoc cref="movemask_epi8(v128)"/>
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        /// <remarks>
        /// <seealso href="https://github.com/DLTcollab/sse2neon/blob/159be6a2d08663a8063d259230d1cb3572fd6026/sse2neon.h#L4761"/>
        /// </remarks>
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Intrinsic-like")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int neon_movemask_epi8(v128 a)
        {
            if (IsNeonSupported)
            {
                v128 high_bits = vshrq_n_u8(a, 7);
                v128 paired16 = vsraq_n_u16(high_bits, high_bits, 7);
                v128 paired32 = vsraq_n_u32(paired16, paired16, 14);
                v128 paired64 = vsraq_n_u64(paired32, paired32, 28);
                return vgetq_lane_u8(paired64, 0) | (vgetq_lane_u8(paired64, 8) << 8);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Enumerator
        {
            [NativeDisableUnsafePtrRestriction]
            public readonly byte* Buffer;
            [NativeDisableUnsafePtrRestriction]
            public readonly byte* GroupPtr;

            public readonly int CapacityCeilGroupSize;
            public int Index;

            public uint FullMaskCache;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(byte* buffer, byte* groupPtr, int capacityCeilGroupSize)
            {
                CheckIsAligned(buffer, ControlAlignment);
                CheckCapacity(capacityCeilGroupSize);

                Buffer = buffer;
                GroupPtr = groupPtr;

                CapacityCeilGroupSize = capacityCeilGroupSize;
                Index = -1;

                SkipInit(out FullMaskCache);
            }

            public bool MoveNext()
            {
                for (; ; )
                {
                    ++Index;

                    if (Index >= CapacityCeilGroupSize)
                    {
                        // Finished
                        return false;
                    }

                    int groupIndex = Index / GroupSize;

                    if (Index % GroupSize == 0)
                    {
                        // First time visiting this group
                        FullMaskCache = GetFullMask(Buffer, Index);
                    }
                    else
                    {
                        // Find next Full

                        if (IsBmi1Supported)
                        {
                            FullMaskCache = blsr_u32(FullMaskCache);
                        }
                        else
                        {
                            // blsr
                            FullMaskCache = (FullMaskCache - 1) & FullMaskCache;
                        }
                    }

                    if (FullMaskCache == 0x0)
                    {
                        // To next group
                        Index = ((groupIndex + 1) * GroupSize) - 1;
                        continue;
                    }

                    int groupOffset = math.tzcnt(FullMaskCache);
                    Index = (groupIndex * GroupSize) + groupOffset;

                    return true;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                Index = -1;
            }
        }
    }

    /// <summary>
    /// <inheritdoc cref="SwissTable"/>
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public unsafe struct SwissTable<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        public static int BufferAlignment
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => math.max(ControlAlignment, KeyValueGroupAlignment);
        }

        public static int KeyValueGroupAlignment
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                const int halfCacheLine = CacheLineSize / 2;

                int groupAlign = AlignOf<KeyValue<TKey, TValue>>();
                int groupSize = sizeof(KeyValue<TKey, TValue>) * GroupSize;

                if (groupSize <= halfCacheLine)
                {
                    // Align 32
                    return math.max(groupAlign, halfCacheLine);
                }
                else
                {
                    // Align 64
                    return math.max(groupAlign, CacheLineSize);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static KeyValue<TKey, TValue>* GetKeyValueGroupPtr(byte* buffer, int capacityCeilGroupSize)
        {
            CheckCapacity(capacityCeilGroupSize);

            byte* ptr = buffer + capacityCeilGroupSize;
            return (KeyValue<TKey, TValue>*)AlignPointer(ptr, KeyValueGroupAlignment);
        }

        public static int Find<THasher>(byte* buffer, int capacityCeilGroupSize, TKey key, out byte h2, out bool exists)
            where THasher : unmanaged, IHasher64<TKey>
        {
            KeyValue<TKey, TValue>* groupPtr = GetKeyValueGroupPtr(buffer, capacityCeilGroupSize);
            return Find<TKey, THasher>(sizeof(KeyValue<TKey, TValue>), buffer, (byte*)groupPtr, capacityCeilGroupSize, key, true, out h2, out exists);
        }

        public static int FindEmpty<THasher>(byte* buffer, int capacityCeilGroupSize, TKey key, out byte h2)
            where THasher : unmanaged, IHasher64<TKey>
        {
            KeyValue<TKey, TValue>* groupPtr = GetKeyValueGroupPtr(buffer, capacityCeilGroupSize);
            return Find<TKey, THasher>(sizeof(KeyValue<TKey, TValue>), buffer, (byte*)groupPtr, capacityCeilGroupSize, key, false, out h2, out _);
        }

        public static ref TValue Insert(byte* buffer, int capacityCeilGroupSize, int index, TKey key, byte h2)
        {
            CheckIsAligned(buffer, ControlAlignment);
            CheckCapacity(capacityCeilGroupSize);
            CheckContainerIndexInRange(index, capacityCeilGroupSize);

            // Control
            buffer[index] = h2;

            // Key Value
            KeyValue<TKey, TValue>* groupPtr = GetKeyValueGroupPtr(buffer, capacityCeilGroupSize);
            ref KeyValue<TKey, TValue> keyValue = ref groupPtr[index];

            // Key
            keyValue.Key = key;

            // Value
            return ref keyValue.Value;
        }

        public static void CopyKeysTo(byte* buffer, int capacityCeilGroupSize, TKey* keyPtr)
        {
            Enumerator enumerator = new(buffer, capacityCeilGroupSize);
            int index = 0;

            while (enumerator.MoveNext())
            {
                keyPtr[index++] = enumerator.Current.KeyRefRO;
            }
        }

        public static void CopyValuesTo(byte* buffer, int capacityCeilGroupSize, TValue* valuePtr)
        {
            Enumerator enumerator = new(buffer, capacityCeilGroupSize);
            int index = 0;

            while (enumerator.MoveNext())
            {
                valuePtr[index++] = enumerator.Current.ValueRef;
            }
        }

        public static void CopyKeysAndValuesTo(byte* buffer, int capacityCeilGroupSize, TKey* keyPtr, TValue* valuePtr)
        {
            Enumerator enumerator = new(buffer, capacityCeilGroupSize);
            int index = 0;

            while (enumerator.MoveNext())
            {
                keyPtr[index] = enumerator.Current.KeyRefRO;
                valuePtr[index] = enumerator.Current.ValueRef;
                ++index;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckKeyNotAlreadyAdded<THasher>(byte* buffer, int capacityCeilGroupSize, TKey key)
            where THasher : unmanaged, IHasher64<TKey>
        {
            _ = Find<THasher>(buffer, capacityCeilGroupSize, key, out _, out bool exists);

            if (Hint.Unlikely(exists))
            {
                ThrowKeyAlreadyAdded();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Enumerator : IEnumerator<KeyValueRef<TKey, TValue>>
        {
            internal SwissTable.Enumerator enumerator;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(byte* buffer, int capacityCeilGroupSize)
            {
                enumerator = new SwissTable.Enumerator(buffer, (byte*)GetKeyValueGroupPtr(buffer, capacityCeilGroupSize), capacityCeilGroupSize);
            }

            public readonly KeyValueRef<TKey, TValue> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    CheckContainerIndexInRange(enumerator.Index, enumerator.CapacityCeilGroupSize);
                    return new KeyValueRef<TKey, TValue>((KeyValue<TKey, TValue>*)enumerator.GroupPtr + enumerator.Index);
                }
            }

            readonly object IEnumerator.Current => throw new NotSupportedException();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                enumerator.Reset();
            }

            public readonly void Dispose()
            {
            }
        }
    }
}
