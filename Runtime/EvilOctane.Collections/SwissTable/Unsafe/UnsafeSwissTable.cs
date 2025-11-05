using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using static EvilOctane.Collections.SwissTable;
using static System.Runtime.CompilerServices.Unsafe;
using static Unity.Collections.CollectionHelper2;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;

namespace EvilOctane.Collections.LowLevel.Unsafe
{
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerTypeProxy(typeof(UnsafeSwissTableDebuggerTypeProxy<,,>))]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int), typeof(XXH3PodHasher<int>) })]
    public unsafe struct UnsafeSwissTable<TKey, TValue, THasher> : IEnumerable<KeyValueRef<TKey, TValue>>, INativeDisposable
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where THasher : unmanaged, IHasher64<TKey>
    {
        [NativeDisableUnsafePtrRestriction]
        internal byte* buffer;

        internal int count;
        internal int capacityCeilGroupSize;
        internal AllocatorManager.AllocatorHandle allocator;
        internal int occupiedCount;

        public static int MaxCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => SwissTable.MaxCapacity;
        }

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => buffer != null;
        }

        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => count == 0;
        }

        public readonly bool IsFull
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => SwissTable.IsFull(capacityCeilGroupSize, occupiedCount);
        }

        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => count;
        }

        public readonly int OccupiedCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => occupiedCount;
        }

        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => capacityCeilGroupSize;
        }

        public TValue this[TKey key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                Ref<TValue> item = TryGet(key, out bool exists);

                if (Hint.Unlikely(!exists))
                {
                    ThrowKeyNotPresent();
                }

                return item.RefRW;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => GetOrAdd(key, out _).RefRW = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeSwissTable(AllocatorManager.AllocatorHandle allocator) : this(2 * GroupSize, allocator)
        {
        }

        public UnsafeSwissTable(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            capacityCeilGroupSize = GetCapacityCeilGroupSize(initialCapacity, out _);
            int groupCount = capacityCeilGroupSize / GroupSize;

            nint groupOffset = SwissTable<TKey, TValue>.GetKeyValueGroupOffset(capacityCeilGroupSize);
            nint totalSize = groupOffset + ((nint)groupCount * sizeof(KeyValue<TKey, TValue>) * GroupSize);

            buffer = (byte*)MemoryExposed.Unmanaged.Allocate(totalSize, SwissTable<TKey, TValue>.BufferAlignment, allocator);
            this.allocator = allocator;

            SkipInit(out count);
            SkipInit(out occupiedCount);

            Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly SwissTable<TKey, TValue>.Enumerator GetEnumerator()
        {
            return new SwissTable<TKey, TValue>.Enumerator(buffer, capacityCeilGroupSize);
        }

        IEnumerator<KeyValueRef<TKey, TValue>> IEnumerable<KeyValueRef<TKey, TValue>>.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!IsCreated)
            {
                return;
            }

            MemoryExposed.Unmanaged.Free(buffer, allocator);
            buffer = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            JobHandle jobHandle = MemoryExposed.ScheduleDispose(buffer, allocator, inputDeps);
            buffer = null;
            return jobHandle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly UnsafeList<TKey> GetKeyList(AllocatorManager.AllocatorHandle allocator)
        {
            UnsafeList<TKey> keyList = UnsafeListExtensions2.Create<TKey>(count, allocator);
            keyList.m_length = count;

            SwissTable<TKey, TValue>.CopyKeysTo(buffer, capacityCeilGroupSize, keyList.Ptr);
            return keyList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly UnsafeList<TValue> GetValueList(AllocatorManager.AllocatorHandle allocator)
        {
            UnsafeList<TValue> valueList = UnsafeListExtensions2.Create<TValue>(count, allocator);
            valueList.m_length = count;

            SwissTable<TKey, TValue>.CopyValuesTo(buffer, capacityCeilGroupSize, valueList.Ptr);
            return valueList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly UnsafeList<TKey> GetKeyValueLists(AllocatorManager.AllocatorHandle allocator, out UnsafeList<TValue> valueList)
        {
            UnsafeList<TKey> keyList = UnsafeListExtensions2.Create<TKey>(count, allocator);
            keyList.m_length = count;

            valueList = UnsafeListExtensions2.Create<TValue>(count, allocator);
            valueList.m_length = count;

            SwissTable<TKey, TValue>.CopyKeysAndValuesTo(buffer, capacityCeilGroupSize, keyList.Ptr, valueList.Ptr);
            return keyList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacity(int capacity, bool keepOldData = true)
        {
            CheckContainerCapacity(capacity);

            int tombstoneCount = occupiedCount - count;
            int requiredCapacity = capacity + tombstoneCount;

            if (!SwissTable.IsFull(capacityCeilGroupSize, requiredCapacity))
            {
                // Enough capacity
                return;
            }

            Rehash(capacity, keepOldData: keepOldData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureSlack(int slack)
        {
            CheckContainerElementCount(slack);

            if (!SwissTable.IsFull(capacityCeilGroupSize, occupiedCount + slack))
            {
                // Enough slack
                return;
            }

            int requiredCapacity = count + slack;
            Rehash(requiredCapacity, keepOldData: true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            CheckIsAligned(buffer, sizeof(v128));

            count = 0;
            occupiedCount = 0;

            SwissTable.Clear(buffer, capacityCeilGroupSize);
        }

        public readonly Ref<TValue> TryGet(TKey key, out bool exists)
        {
            int index = SwissTable<TKey, TValue>.Find<THasher>(buffer, capacityCeilGroupSize, key, out byte h2, out _, out exists);
            return exists ? &SwissTable<TKey, TValue>.GetKeyValueGroupPtr(buffer, capacityCeilGroupSize)[index].Value : null;
        }

        public Ref<TValue> GetOrAdd(TKey key, out bool added)
        {
            // Preemptive resize
            // Better than calling Find twice
            EnsureSlack(1);

            return GetOrAddNoResize(key, out added);
        }

        public Ref<TValue> GetOrAddNoResize(TKey key, out bool added)
        {
            int index = SwissTable<TKey, TValue>.Find<THasher>(buffer, capacityCeilGroupSize, key, out byte h2, out _, out bool exists);

            if (exists)
            {
                // Exists
                added = false;
                return &SwissTable<TKey, TValue>.GetKeyValueGroupPtr(buffer, capacityCeilGroupSize)[index].Value;
            }

            // Add
            CheckAddNoResizeHasEnoughCapacity(occupiedCount, capacityCeilGroupSize, 1);
            ++count;
            ++occupiedCount;

            added = true;
            return SwissTable<TKey, TValue>.Insert(buffer, capacityCeilGroupSize, index, key, h2);
        }

        public Ref<TValue> Add(TKey key)
        {
            EnsureSlack(1);
            return AddNoResize(key);
        }

        public Ref<TValue> AddNoResize(TKey key)
        {
            CheckAddNoResizeHasEnoughCapacity(occupiedCount, capacityCeilGroupSize, 1);
            SwissTable<TKey, TValue>.CheckKeyNotAlreadyAdded<THasher>(buffer, capacityCeilGroupSize, key);

            ++count;
            ++occupiedCount;
            int index = SwissTable<TKey, TValue>.FindEmpty<THasher>(buffer, capacityCeilGroupSize, key, out byte h2, out _);
            return SwissTable<TKey, TValue>.Insert(buffer, capacityCeilGroupSize, index, key, h2);
        }

        public bool Remove(TKey key)
        {
            int index = SwissTable<TKey, TValue>.Find<THasher>(buffer, capacityCeilGroupSize, key, out _, out int groupOffset, out bool exists);

            if (!exists)
            {
                // Not present
                return false;
            }

            // Delete
            Delete(buffer, capacityCeilGroupSize, groupOffset, index, tryReclaim: false, out bool reclaimed);
            --count;

            if (reclaimed)
            {
                // Reclaimed
                --occupiedCount;
            }

            return true;
        }

        private void Rehash(int requiredCapacity, bool keepOldData)
        {
            byte* oldBuffer = buffer;
            int oldCapacity = capacityCeilGroupSize;

            int newCapacity;

            if (requiredCapacity <= oldCapacity)
            {
                // Shrink
                newCapacity = math.max(requiredCapacity, count);
            }
            else
            {
                // Grow
                newCapacity = math.max(requiredCapacity, oldCapacity + (oldCapacity / 2));
            }

            this = new UnsafeSwissTable<TKey, TValue, THasher>(newCapacity, allocator);

            if (keepOldData)
            {
                // Copy data
                CopyFrom(oldBuffer, oldCapacity);
            }

            MemoryExposed.Unmanaged.Free(oldBuffer, allocator);
        }

        private void CopyFrom(byte* buffer, int capacityCeilGroupSize)
        {
            SwissTable<TKey, TValue>.Enumerator enumerator = new(buffer, capacityCeilGroupSize);

            while (enumerator.MoveNext())
            {
                KeyValueRef<TKey, TValue> keyValue = enumerator.Current;
                AddNoResize(keyValue.KeyRefRO).RefRW = keyValue.ValueRef;
            }
        }
    }

    internal sealed unsafe class UnsafeSwissTableDebuggerTypeProxy<TKey, TValue, THasher>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
        where THasher : unmanaged, IHasher64<TKey>
    {
        private readonly UnsafeSwissTable<TKey, TValue, THasher> target;

        public UnsafeSwissTableDebuggerTypeProxy(UnsafeSwissTable<TKey, TValue, THasher> target)
        {
            this.target = target;
        }

        public byte[] Controls => new UnsafeSpan<byte>(target.buffer, target.capacityCeilGroupSize).ToArray();

        public KeyValue<TKey, TValue>[] Items
        {
            get
            {
                KeyValue<TKey, TValue>[] result = new KeyValue<TKey, TValue>[target.Count];
                int index = 0;

                foreach (KeyValueRef<TKey, TValue> item in target)
                {
                    result[index++] = item.Pointer.RefRW;
                }

                return result;
            }
        }
    }
}
