using System;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;
using static EvilOctane.Collections.SwissTable;
using static System.Runtime.CompilerServices.Unsafe;
using static Unity.Burst.Intrinsics.X86.Bmi1;
using static Unity.Burst.Intrinsics.X86.Sse2;
using static Unity.Burst.Intrinsics.X86.Sse4_2;
using static Unity.Collections.CollectionHelper;
using static Unity.Collections.CollectionHelper2;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;

namespace EvilOctane.Collections.LowLevel.Unsafe
{
    public unsafe struct InPlaceSwissTable<TKey, TValue, THasher>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where THasher : unmanaged, IHasher64<TKey>
    {
        public static int BufferAlignment
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GroupAlignment;
        }

        internal static int GroupAlignment
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                int groupAlign = AlignOf<KeyValue<TKey, TValue>>();
                return math.max(groupAlign, CacheLineSize);
            }
        }

        internal static nint ControlOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Align(sizeof(InPlaceSwissTableHeader<TKey, TValue>), sizeof(v128));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint GetAllocationSize(int capacity, out int capacityCeilGroupSize)
        {
            CheckContainerCapacity(capacity);

            int capacityWithSlack = (int)math.ceil(capacity * (1 / MaxLoadFactor));
            capacityCeilGroupSize = Align(capacityWithSlack, GroupSize);

            int groupCount = capacityCeilGroupSize / GroupSize;
            nint groupOffset = GetGroupOffset(capacityCeilGroupSize);
            return groupOffset + ((nint)groupCount * sizeof(KeyValue<TKey, TValue>) * GroupSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Initialize(InPlaceSwissTableHeader<TKey, TValue>* header, int capacityCeilGroupSize)
        {
            CheckContainerCapacity(capacityCeilGroupSize);

            Assert.AreEqual(0, capacityCeilGroupSize % GroupSize);
            Assert.IsTrue(capacityCeilGroupSize / GroupSize >= 1);

            header->Count = 0;
            header->CapacityCeilGroupSize = capacityCeilGroupSize;

            byte* controlPtr = (byte*)header + ControlOffset;
            int groupCount = capacityCeilGroupSize / GroupSize;

            new UnsafeSpan<v128>((v128*)controlPtr, groupCount).Fill(new v128(ControlEmpty));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue TryGet(InPlaceSwissTableHeader<TKey, TValue>* header, TKey key, out bool exists)
        {
            int index = Find(header, key, out byte h2, out exists);

            KeyValue<TKey, TValue>* groupPtr = (KeyValue<TKey, TValue>*)((byte*)header + GetGroupOffset(header->CapacityCeilGroupSize));
            return ref exists ? ref groupPtr[index].Value : ref NullRef<TValue>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetOrAddNoResize(InPlaceSwissTableHeader<TKey, TValue>* header, TKey key, out bool added)
        {
            int index = Find(header, key, out byte h2, out bool exists);

            if (exists)
            {
                // Exists
                added = false;

                KeyValue<TKey, TValue>* groupPtr = (KeyValue<TKey, TValue>*)((byte*)header + GetGroupOffset(header->CapacityCeilGroupSize));
                return ref groupPtr[index].Value;
            }

            // Added
            added = true;
            return ref Insert(header, key, h2, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue AddUncheckedNoResize(InPlaceSwissTableHeader<TKey, TValue>* header, TKey key)
        {
            CheckAddNoResizeHasEnoughCapacity(header->Count, header->CapacityCeilGroupSize, 1);

            int index = Find(header, key, out byte h2, out bool exists);
            Assert.IsFalse(exists);

            return ref Insert(header, key, h2, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeSwissTableEnumerator<TKey, TValue> GetEnumerator(InPlaceSwissTableHeader<TKey, TValue>* header)
        {
            byte* controlPtr = (byte*)header + ControlOffset;
            KeyValue<TKey, TValue>* groupPtr = (KeyValue<TKey, TValue>*)((byte*)header + GetGroupOffset(header->CapacityCeilGroupSize));
            return new UnsafeSwissTableEnumerator<TKey, TValue>(controlPtr, groupPtr, header->CapacityCeilGroupSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static nint GetGroupOffset(int capacity)
        {
            return Align(ControlOffset + (capacity * sizeof(byte)), GroupAlignment);
        }

        internal static int Find(InPlaceSwissTableHeader<TKey, TValue>* header, TKey key, out byte h2, out bool exists)
        {
            ulong hash = default(THasher).CalculateHash(key);
            ulong h1 = SwissHash.H1(hash);

            h2 = SwissHash.H2(hash);
            v128 h2Vec = new(h2);

            byte* controlPtr = (byte*)header + ControlOffset;
            int groupIndex = (int)Reduce(SwissHash.H1Compressed(h1), (uint)header->GroupCount);

            KeyValue<TKey, TValue>* groupPtr = (KeyValue<TKey, TValue>*)((byte*)header + GetGroupOffset(header->CapacityCeilGroupSize));

            for (; ; ++groupIndex)
            {
                int startIndex = groupIndex * GroupSize;

                if (IsSse42Supported)
                {
                    v128 cntrl = load_si128(controlPtr + (startIndex * sizeof(byte)));

                    v128 eqH2 = cmpeq_epi8(cntrl, h2Vec);
                    uint maskH2 = (uint)movemask_epi8(eqH2);

                    // Probe for h2 matches

                    for (uint groupOffset = tzcnt_u32(maskH2); maskH2 != 0x0; groupOffset = tzcnt_u32(maskH2))
                    {
                        int index = startIndex + (int)groupOffset;
                        ref TKey keyRO = ref (groupPtr + index)->Key;

                        if (Hint.Likely(key.Equals(keyRO)))
                        {
                            // Found
                            exists = true;
                            return index;
                        }

                        maskH2 = blsr_u32(maskH2);
                    }

                    // Probe for empty

                    v128 eqEmpty = cmpeq_epi8(cntrl, new v128(ControlEmpty));
                    uint maskEmpty = (uint)movemask_epi8(eqEmpty);

                    if (Hint.Likely(maskEmpty != 0x0))
                    {
                        // Empty
                        exists = false;
                        return startIndex + math.tzcnt(maskEmpty);
                    }
                }
                else
                {
                    // Probe for h2 matches

                    for (int groupOffset = 0; groupOffset != GroupSize; ++groupOffset)
                    {
                        int index = startIndex + groupOffset;

                        if (controlPtr[index] != h2)
                        {
                            continue;
                        }

                        ref TKey keyRO = ref (groupPtr + index)->Key;

                        if (key.Equals(keyRO))
                        {
                            // Found
                            exists = true;
                            return index;
                        }
                    }

                    // Probe for empty

                    for (int groupOffset = 0; groupOffset != GroupSize; ++groupOffset)
                    {
                        int index = startIndex + groupOffset;

                        if (controlPtr[index] == ControlEmpty)
                        {
                            // Empty
                            exists = false;
                            return startIndex + groupOffset;
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue Insert(InPlaceSwissTableHeader<TKey, TValue>* header, TKey key, byte h2, int index)
        {
            // Count
            ++header->Count;

            // Control
            byte* controlPtr = (byte*)header + ControlOffset;
            controlPtr[index] = h2;

            // Key Value
            KeyValue<TKey, TValue>* groupPtr = (KeyValue<TKey, TValue>*)((byte*)header + GetGroupOffset(header->CapacityCeilGroupSize));
            ref KeyValue<TKey, TValue> keyValue = ref groupPtr[index];

            // Key
            keyValue.Key = key;

            // Value
            return ref keyValue.Value;
        }
    }
}
