using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using static EvilOctane.Collections.SwissTable;
using static System.Runtime.CompilerServices.Unsafe;
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
        public const int MaxLoadFactorNumerator = 15;
        public const int MaxLoadFactorDenominator = 16;

        public const byte ControlEmpty = 0x80;
        public const byte ControlDeleted = 0xfe;
        public const byte ControlSentinel = 0xff;
        public const byte ControlFullMask = 0x7f;

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
            return (uint)Reinterpret<ulong, uint2>(ref h1).GetHashCode();
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

            if (IsSse42Supported && IsBmi1Supported)
            {
                v128 control = load_si128(controlPtr);
                uint notFullMask = (uint)movemask_epi8(control);
                return notFullMask ^ 0xffff;
            }
            else
            {
                uint mask = 0x0;

                for (int i = 0; i != GroupSize; ++i)
                {
                    bool isFull = (controlPtr[i] & ~ControlFullMask) == 0;
                    mask |= isFull ? ((uint)(1 << i)) : 0x0;
                }

                return mask;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetH2Mask(byte* buffer, int index, byte h2)
        {
            CheckIsAligned(buffer, ControlAlignment);
            Hint.Assume(IsAligned(buffer, GroupSize));

            byte* controlPtr = buffer + index;

            if (IsSse42Supported && IsBmi1Supported)
            {
                v128 control = load_si128(controlPtr);
                v128 eqH2 = cmpeq_epi8(control, new v128(h2));
                return (uint)movemask_epi8(eqH2);
            }
            else
            {
                uint mask = 0x0;

                for (int i = 0; i != GroupSize; ++i)
                {
                    bool eqH2 = controlPtr[i] == h2;
                    mask |= eqH2 ? ((uint)(1 << i)) : 0x0;
                }

                return mask;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetCapacityCeilGroupSize(int capacity)
        {
            CheckContainerCapacity(capacity);

            capacity = math.max(capacity, 1);
            int capacityAdjusted = (int)math.ceil(capacity * (float)MaxLoadFactorDenominator / MaxLoadFactorNumerator);

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

        /// <summary>
        /// Fast modulo reduction.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        /// <remarks>
        /// <seealso href="https://lemire.me/blog/2016/06/27/a-fast-alternative-to-the-modulo-reduction"/>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint Reduce(uint x, uint n)
        {
            return x % n;
            // TODO: Reduce(x + 1, n) becomes ++x for some reason
            return (uint)((x * (ulong)n) >> 32);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Find<THasher>(byte* buffer, int capacityCeilGroupSize, TKey key, bool checkExists, out byte h2, out bool exists)
            where THasher : unmanaged, IHasher64<TKey>
        {
            CheckIsAligned(buffer, ControlAlignment);
            CheckCapacity(capacityCeilGroupSize);

            ulong hash = default(THasher).CalculateHash(key);

            ulong h1 = H1(hash);
            h2 = H2(hash);

            int groupCount = capacityCeilGroupSize / GroupSize;
            int groupIndex = (int)Reduce(H1Compressed(h1), (uint)groupCount);

            KeyValue<TKey, TValue>* groupPtr = GetKeyValueGroupPtr(buffer, capacityCeilGroupSize);

            for (; ; groupIndex = (int)Reduce((uint)groupIndex + 1, (uint)groupCount))
            {
                int startIndex = groupIndex * GroupSize;

                if (checkExists)
                {
                    uint h2Mask = GetH2Mask(buffer, startIndex, h2);

                    // Probe for h2 matches

                    while (h2Mask != 0x0)
                    {
                        int index = startIndex + math.tzcnt(h2Mask);
                        ref TKey keyRO = ref (groupPtr + index)->Key;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyKeysTo(byte* buffer, int capacityCeilGroupSize, TKey* keyPtr)
        {
            UnsafeEnumerator enumerator = new(buffer, capacityCeilGroupSize);
            int index = 0;

            while (enumerator.MoveNext())
            {
                keyPtr[index++] = enumerator.Current.KeyRefRO;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyValuesTo(byte* buffer, int capacityCeilGroupSize, TValue* valuePtr)
        {
            UnsafeEnumerator enumerator = new(buffer, capacityCeilGroupSize);
            int index = 0;

            while (enumerator.MoveNext())
            {
                valuePtr[index++] = enumerator.Current.ValueRef;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowKeyNotPresent(TKey key)
        {
            throw new ArgumentException($"Key: {key} is not present.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowKeyAlreadyAdded(TKey key)
        {
            throw new ArgumentException($"An item with the same key has already been added: {key}");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckKeyNotAlreadyAdded<THasher>(byte* buffer, int capacityCeilGroupSize, TKey key)
            where THasher : unmanaged, IHasher64<TKey>
        {
            _ = Find<THasher>(buffer, capacityCeilGroupSize, key, true, out _, out bool exists);

            if (Hint.Unlikely(exists))
            {
                ThrowKeyAlreadyAdded(key);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct UnsafeEnumerator : IEnumerator<KeyValueRef<TKey, TValue>>
        {
            [NativeDisableUnsafePtrRestriction]
            internal readonly byte* buffer;
            [NativeDisableUnsafePtrRestriction]
            internal readonly KeyValue<TKey, TValue>* groupPtr;

            internal readonly int capacityCeilGroupSize;
            internal int index;

            internal uint fullMaskCache;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal UnsafeEnumerator(byte* buffer, int capacityCeilGroupSize)
            {
                CheckIsAligned(buffer, ControlAlignment);
                CheckCapacity(capacityCeilGroupSize);

                this.buffer = buffer;
                groupPtr = GetKeyValueGroupPtr(buffer, capacityCeilGroupSize);

                this.capacityCeilGroupSize = capacityCeilGroupSize;
                index = -1;

                SkipInit(out fullMaskCache);
            }

            public readonly KeyValueRef<TKey, TValue> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    CheckContainerIndexInRange(index, capacityCeilGroupSize);
                    return new KeyValueRef<TKey, TValue>(groupPtr + index);
                }
            }

            readonly object IEnumerator.Current => throw new NotSupportedException();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                for (; ; )
                {
                    ++index;

                    if (index >= capacityCeilGroupSize)
                    {
                        // Finished
                        return false;
                    }

                    int groupIndex = index / GroupSize;

                    if (index % GroupSize == 0)
                    {
                        // First time visiting this group
                        fullMaskCache = GetFullMask(buffer, index);
                    }
                    else
                    {
                        // Find next Full

                        if (IsBmi1Supported)
                        {
                            fullMaskCache = blsr_u32(fullMaskCache);
                        }
                        else
                        {
                            // blsr
                            fullMaskCache = (fullMaskCache - 1) & fullMaskCache;
                        }
                    }

                    if (fullMaskCache == 0x0)
                    {
                        // To next group
                        index = ((groupIndex + 1) * GroupSize) - 1;
                        continue;
                    }

                    int groupOffset = math.tzcnt(fullMaskCache);
                    index = (groupIndex * GroupSize) + groupOffset;

                    return true;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                index = -1;
            }

            public readonly void Dispose()
            {
            }
        }
    }
}
