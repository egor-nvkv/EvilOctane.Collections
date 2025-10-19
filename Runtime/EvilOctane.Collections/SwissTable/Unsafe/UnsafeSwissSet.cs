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
using static Unity.Collections.CollectionHelper;
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

        internal readonly TKey* GroupPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (TKey*)(buffer + GetGroupOffset(capacityCeilGroupSize));
        }

        public bool this[TKey key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => Exists(key);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _ = value ? Add(key) : Remove(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeSwissSet(AllocatorManager.AllocatorHandle allocator) : this(3 * GroupSize, allocator)
        {
        }

        public UnsafeSwissSet(int initialCapacity, AllocatorManager.AllocatorHandle allocator)
        {
            capacityCeilGroupSize = GetCapacityCeilGroupSize(initialCapacity);
            int groupCount = capacityCeilGroupSize / GroupSize;

            nint groupOffset = GetGroupOffset(capacityCeilGroupSize);
            nint totalSize = groupOffset + ((nint)groupCount * sizeof(TKey) * GroupSize);

            buffer = (byte*)MemoryExposed.Unmanaged.Allocate(totalSize, SwissSet<TKey>.BufferAlignment, allocator);
            this.allocator = allocator;

            SkipInit(out count);
            SkipInit(out occupiedCount);

            Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static nint GetGroupOffset(int capacity)
        {
            return Align(capacity * sizeof(byte), SwissSet<TKey>.KeyGroupAlignment);
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
            CheckIsAligned(buffer, sizeof(v128));

            count = 0;
            occupiedCount = 0;

            int groupCount = capacityCeilGroupSize / GroupSize;
            new UnsafeSpan<v128>((v128*)buffer, groupCount).Fill(new v128(ControlEmpty));
        }

        public readonly bool Exists(TKey key)
        {
            _ = SwissSet<TKey>.Find<THasher>(buffer, capacityCeilGroupSize, key, out _, out bool exists);
            return exists;
        }

        public bool Add(TKey key)
        {
            if (Hint.Unlikely(IsFull))
            {
                // Resize
                Grow(1, keepOldData: true);
            }

            return AddNoResize(key);
        }

        public bool AddNoResize(TKey key)
        {
            CheckAddNoResizeHasEnoughCapacity(occupiedCount, capacityCeilGroupSize, 1);

            int index = SwissSet<TKey>.Find<THasher>(buffer, capacityCeilGroupSize, key, out byte h2, out bool exists);

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
            int index = SwissSet<TKey>.Find<THasher>(buffer, capacityCeilGroupSize, key, out _, out bool exists);

            if (!exists)
            {
                // Not present
                return false;
            }

            // Delete
            Delete(buffer, index, ref occupiedCount);
            --count;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Grow(int slack, bool keepOldData)
        {
            Rehash(capacityCeilGroupSize + slack, keepOldData);
        }

        private void Rehash(int capacity, bool keepOldData)
        {
            byte* oldBuffer = buffer;
            int oldCapacity = capacityCeilGroupSize;

            capacity = math.max(capacity, oldCapacity + (oldCapacity / 2));
            this = new UnsafeSwissSet<TKey, THasher>(capacity, allocator);

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
                _ = AddNoResize(enumerator.Current.Ref);
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

        public byte[] Controls => new UnsafeSpan<byte>(target.buffer, target.count).ToArray();

        public TKey[] Keys
        {
            get
            {
                TKey[] result = new TKey[target.Count];
                int index = 0;

                foreach (Pointer<TKey> item in target)
                {
                    result[index++] = item.Ref;
                }

                return result;
            }
        }
    }
}
