using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using static EvilOctane.Collections.SwissTable;
using static Unity.Collections.CollectionHelper;
using static Unity.Collections.CollectionHelper2;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;

namespace EvilOctane.Collections.LowLevel.Unsafe
{
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int), typeof(XXH3PodHasher<int>) })]
    public unsafe struct InPlaceSwissTable<TKey, TValue, THasher>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where THasher : unmanaged, IHasher64<TKey>
    {
        public static int MaxCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => SwissTable.MaxCapacity;
        }

        public static int BufferAlignment
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => math.max(AlignOf<InPlaceSwissTableHeader<TKey, TValue>>(), SwissTable<TKey, TValue>.BufferAlignment);
        }

        internal static int ControlOffset
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Align(sizeof(InPlaceSwissTableHeader<TKey, TValue>), ControlAlignment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint GetAllocationSize(int capacity, out int capacityCeilGroupSize)
        {
            capacityCeilGroupSize = GetCapacityCeilGroupSize(capacity, out _);
            int groupCount = capacityCeilGroupSize / GroupSize;

            nint groupOffset = (nint)SwissTable<TKey, TValue>.GetKeyValueGroupPtr((byte*)ControlOffset, capacityCeilGroupSize);
            return groupOffset + ((nint)groupCount * sizeof(KeyValue<TKey, TValue>) * GroupSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Initialize(InPlaceSwissTableHeader<TKey, TValue>* header, int capacityCeilGroupSize)
        {
            CheckCapacity(capacityCeilGroupSize);

            header->Count = 0;
            header->CapacityCeilGroupSize = capacityCeilGroupSize;

            byte* buffer = (byte*)header + ControlOffset;
            Clear(buffer, capacityCeilGroupSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pointer<TValue> TryGet(InPlaceSwissTableHeader<TKey, TValue>* header, TKey key, out bool exists)
        {
            byte* buffer = (byte*)header + ControlOffset;
            int index = SwissTable<TKey, TValue>.Find<THasher>(buffer, header->CapacityCeilGroupSize, key, out byte h2, out int groupOffset, out exists);

            KeyValue<TKey, TValue>* groupPtr = SwissTable<TKey, TValue>.GetKeyValueGroupPtr(buffer, header->CapacityCeilGroupSize);
            return exists ? &groupPtr[index].Value : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pointer<TValue> GetOrAddNoResize(InPlaceSwissTableHeader<TKey, TValue>* header, TKey key, out bool added)
        {
            byte* buffer = (byte*)header + ControlOffset;
            int index = SwissTable<TKey, TValue>.Find<THasher>(buffer, header->CapacityCeilGroupSize, key, out byte h2, out int groupOffset, out bool exists);

            if (exists)
            {
                // Exists
                added = false;

                KeyValue<TKey, TValue>* groupPtr = SwissTable<TKey, TValue>.GetKeyValueGroupPtr(buffer, header->CapacityCeilGroupSize);
                return &groupPtr[index].Value;
            }

            // Add
            CheckAddNoResizeHasEnoughCapacity(header->Count, header->CapacityCeilGroupSize, 1);
            ++header->Count;

            added = true;
            return SwissTable<TKey, TValue>.Insert(buffer, header->CapacityCeilGroupSize, index, key, h2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pointer<TValue> AddNoResize(InPlaceSwissTableHeader<TKey, TValue>* header, TKey key)
        {
            CheckAddNoResizeHasEnoughCapacity(header->Count, header->CapacityCeilGroupSize, 1);

            byte* buffer = (byte*)header + ControlOffset;
            SwissTable<TKey, TValue>.CheckKeyNotAlreadyAdded<THasher>(buffer, header->CapacityCeilGroupSize, key);

            ++header->Count;

            int index = SwissTable<TKey, TValue>.FindEmpty<THasher>(buffer, header->CapacityCeilGroupSize, key, out byte h2, out int groupOffset);
            return SwissTable<TKey, TValue>.Insert(buffer, header->CapacityCeilGroupSize, index, key, h2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SwissTable<TKey, TValue>.Enumerator GetEnumerator(InPlaceSwissTableHeader<TKey, TValue>* header)
        {
            byte* buffer = (byte*)header + ControlOffset;
            return new SwissTable<TKey, TValue>.Enumerator(buffer, header->CapacityCeilGroupSize);
        }
    }
}
