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
using static EvilOctane.Collections.SwissTable;
using static System.Runtime.CompilerServices.Unsafe;
using static Unity.Collections.CollectionHelper;
using static Unity.Collections.CollectionHelper2;

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
        public const int MinGrowthCount = 4 * GroupSize;

        [NativeDisableUnsafePtrRestriction]
        internal byte* buffer;

        internal int count;
        internal int capacityCeilGroupSize;
        internal AllocatorManager.AllocatorHandle allocator;
        internal int occupiedCount;

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

        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => capacityCeilGroupSize;
        }

        internal readonly KeyValue<TKey, TValue>* GroupPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (KeyValue<TKey, TValue>*)(buffer + GetGroupOffset(capacityCeilGroupSize));
        }

        public TValue this[TKey key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                ref TValue item = ref TryGet(key, out bool exists);

                if (Hint.Unlikely(!exists))
                {
                    SwissTable<TKey, TValue>.ThrowKeyNotPresent(key);
                }

                return item;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => GetOrAdd(key, out _) = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeSwissTable(AllocatorManager.AllocatorHandle allocator) : this(4 * GroupSize, allocator)
        {
        }

        public UnsafeSwissTable(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            capacityCeilGroupSize = GetCapacityCeilGroupSize(initialCapacity);
            int groupCount = capacityCeilGroupSize / GroupSize;

            nint groupOffset = GetGroupOffset(capacityCeilGroupSize);
            nint totalSize = groupOffset + ((nint)groupCount * sizeof(KeyValue<TKey, TValue>) * GroupSize);

            buffer = (byte*)MemoryExposed.Unmanaged.Allocate(totalSize, SwissTable<TKey, TValue>.BufferAlignment, allocator);
            this.allocator = allocator;

            SkipInit(out count);
            SkipInit(out occupiedCount);

            Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static nint GetGroupOffset(int capacity)
        {
            return Align(capacity * sizeof(byte), SwissTable<TKey, TValue>.KeyValueGroupAlignment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly SwissTable<TKey, TValue>.UnsafeEnumerator GetEnumerator()
        {
            return new SwissTable<TKey, TValue>.UnsafeEnumerator(buffer, capacityCeilGroupSize);
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
        public void EnsureCapacity(int capacity, bool keepOldData = true)
        {
            CheckContainerCapacity(capacity);

            int slack = capacity - capacityCeilGroupSize;

            if (slack <= 0)
            {
                // Enough capacity
                return;
            }

            Grow(slack, keepOldData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            count = 0;
            occupiedCount = 0;

            int groupCount = capacityCeilGroupSize / GroupSize;
            new UnsafeSpan<v128>((v128*)buffer, groupCount).Fill(new v128(ControlEmpty));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref TValue TryGet(TKey key, out bool exists)
        {
            int index = SwissTable<TKey, TValue>.Find<THasher>(buffer, capacityCeilGroupSize, key, true, out _, out exists);
            return ref exists ? ref GroupPtr[index].Value : ref NullRef<TValue>();
        }

        public ref TValue GetOrAdd(TKey key, out bool added)
        {
            if (Hint.Unlikely(IsFull))
            {
                // Preemptive resize
                // Better than calling Find twice
                Grow(1, keepOldData: true);
            }

            return ref GetOrAddNoResize(key, out added);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetOrAddNoResize(TKey key, out bool added)
        {
            int index = SwissTable<TKey, TValue>.Find<THasher>(buffer, capacityCeilGroupSize, key, true, out byte h2, out bool exists);

            if (exists)
            {
                // Exists
                added = false;
                return ref GroupPtr[index].Value;
            }

            // Add
            CheckAddNoResizeHasEnoughCapacity(occupiedCount, capacityCeilGroupSize, 1);
            ++count;
            ++occupiedCount;

            added = true;
            return ref SwissTable<TKey, TValue>.Insert(buffer, capacityCeilGroupSize, index, key, h2);
        }

        public ref TValue Add(TKey key)
        {
            if (Hint.Unlikely(IsFull))
            {
                // Resize
                Grow(1, keepOldData: true);
            }

            return ref AddNoResize(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue AddNoResize(TKey key)
        {
            CheckAddNoResizeHasEnoughCapacity(occupiedCount, capacityCeilGroupSize, 1);
            SwissTable<TKey, TValue>.CheckKeyNotAlreadyAdded<THasher>(buffer, capacityCeilGroupSize, key);

            ++count;
            ++occupiedCount;

            int index = SwissTable<TKey, TValue>.Find<THasher>(buffer, capacityCeilGroupSize, key, false, out byte h2, out _);
            return ref SwissTable<TKey, TValue>.Insert(buffer, capacityCeilGroupSize, index, key, h2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(TKey key)
        {
            int index = SwissTable<TKey, TValue>.Find<THasher>(buffer, capacityCeilGroupSize, key, true, out _, out bool exists);

            if (!exists)
            {
                // Not present
                return false;
            }

            // Delete
            SwissTable<TKey, TValue>.Delete(buffer, index, ref occupiedCount);
            --count;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Grow(int slack, bool keepOldData)
        {
            Rehash(capacityCeilGroupSize + slack, keepOldData);
        }

        private void Rehash(int newCapacity, bool keepOldData)
        {
            byte* oldBuffer = buffer;
            int oldCapacity = capacityCeilGroupSize;

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
            SwissTable<TKey, TValue>.UnsafeEnumerator enumerator = new(buffer, capacityCeilGroupSize);

            while (enumerator.MoveNext())
            {
                KeyValueRef<TKey, TValue> keyValue = enumerator.Current;
                AddNoResize(keyValue.KeyRefRO) = keyValue.ValueRef;
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

        public byte[] Controls => new UnsafeSpan<byte>(target.buffer, target.count).ToArray();

        public KeyValue<TKey, TValue>[] Items
        {
            get
            {
                KeyValue<TKey, TValue>[] result = new KeyValue<TKey, TValue>[target.Count];
                int index = 0;

                foreach (KeyValueRef<TKey, TValue> item in target)
                {
                    result[index++] = item.Pointer.Ref;
                }

                return result;
            }
        }
    }
}
