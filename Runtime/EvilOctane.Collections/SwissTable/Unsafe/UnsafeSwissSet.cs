using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    [DebuggerTypeProxy(typeof(UnsafeSwissSetDebuggerTypeProxy<,>))]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(XXH3PodHasher<int>) })]
    public unsafe struct UnsafeSwissSet<TKey, THasher> : IEnumerable<Pointer<TKey>>, INativeDisposable
    where TKey : unmanaged, IEquatable<TKey>
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

        public readonly AllocatorManager.AllocatorHandle Allocator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => allocator;
        }

        public bool this[TKey key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => Exists(key);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _ = value ? Add(key) : Remove(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeSwissSet(AllocatorManager.AllocatorHandle allocator) : this(2 * GroupSize, allocator)
        {
        }

        public UnsafeSwissSet(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            capacityCeilGroupSize = GetCapacityCeilGroupSize(initialCapacity, out _);
            int groupCount = capacityCeilGroupSize / GroupSize;

            nint groupOffset = SwissSet<TKey>.GetKeyGroupOffset(capacityCeilGroupSize);
            nint totalSize = groupOffset + ((nint)groupCount * sizeof(TKey) * GroupSize);

            buffer = (byte*)MemoryExposed.Unmanaged.Allocate(totalSize, SwissSet<TKey>.BufferAlignment, allocator);
            this.allocator = allocator;

            SkipInit(out count);
            SkipInit(out occupiedCount);

            Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly SwissSet<TKey>.Enumerator GetEnumerator()
        {
            return new SwissSet<TKey>.Enumerator(buffer, capacityCeilGroupSize);
        }

        IEnumerator<Pointer<TKey>> IEnumerable<Pointer<TKey>>.GetEnumerator()
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

            SwissSet<TKey>.CopyKeysTo(buffer, capacityCeilGroupSize, keyList.Ptr);
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

        public readonly bool Exists(TKey key)
        {
            _ = SwissSet<TKey>.Find<THasher>(buffer, capacityCeilGroupSize, key, out _, out _, out bool exists);
            return exists;
        }

        public bool Add(TKey key)
        {
            EnsureSlack(1);
            return AddNoResize(key);
        }

        public bool AddNoResize(TKey key)
        {
            CheckAddNoResizeHasEnoughCapacity(occupiedCount, capacityCeilGroupSize, 1);
            int index = SwissSet<TKey>.Find<THasher>(buffer, capacityCeilGroupSize, key, out byte h2, out _, out bool exists);

            if (exists)
            {
                // Already present
                return false;
            }

            // Add

            ++count;
            ++occupiedCount;

            SwissSet<TKey>.Insert(buffer, capacityCeilGroupSize, index, key, h2);
            return true;
        }

        public bool Remove(TKey key)
        {
            int index = SwissSet<TKey>.Find<THasher>(buffer, capacityCeilGroupSize, key, out _, out int groupOffset, out bool exists);

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

            this = new UnsafeSwissSet<TKey, THasher>(newCapacity, allocator);

            if (keepOldData)
            {
                // Copy data
                CopyFrom(oldBuffer, oldCapacity);
            }

            MemoryExposed.Unmanaged.Free(oldBuffer, allocator);
        }

        private void CopyFrom(byte* buffer, int capacityCeilGroupSize)
        {
            SwissSet<TKey>.Enumerator enumerator = new(buffer, capacityCeilGroupSize);

            while (enumerator.MoveNext())
            {
                _ = AddNoResize(enumerator.Current.AsRef);
            }
        }
    }

    internal sealed unsafe class UnsafeSwissSetDebuggerTypeProxy<TKey, THasher>
        where TKey : unmanaged, IEquatable<TKey>
        where THasher : unmanaged, IHasher64<TKey>
    {
        private readonly UnsafeSwissSet<TKey, THasher> target;

        public UnsafeSwissSetDebuggerTypeProxy(UnsafeSwissSet<TKey, THasher> target)
        {
            this.target = target;
        }

        public byte[] Controls => new UnsafeSpan<byte>(target.buffer, target.capacityCeilGroupSize).ToArray();

        public TKey[] Keys
        {
            get
            {
                TKey[] result = new TKey[target.Count];
                int index = 0;

                foreach (Pointer<TKey> item in target)
                {
                    result[index++] = item.AsRef;
                }

                return result;
            }
        }
    }
}
