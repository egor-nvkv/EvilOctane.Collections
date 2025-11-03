using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Assertions;
using static System.Runtime.CompilerServices.Unsafe;

namespace Unity.Collections.LowLevel.Unsafe
{
    public readonly unsafe ref struct HashMapHelperRef<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        // Ideally, this would be a ref field
        internal readonly HashMapHelper<TKey>* ptr;

        public readonly bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ptr != null;
        }

        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ptr->IsEmpty;
        }

        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ptr->Count;
        }

        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ptr->Capacity;
        }

        public readonly AllocatorManager.AllocatorHandle Allocator
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ptr->Allocator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private HashMapHelperRef(HashMapHelper<TKey>* ptr)
        {
            this.ptr = ptr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashMapHelperRef<TKey> CreateFor<TValue>(ref UnsafeHashMap<TKey, TValue> hashMap)
            where TValue : unmanaged
        {
            return new HashMapHelperRef<TKey>((HashMapHelper<TKey>*)AsPointer(ref hashMap.m_Data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashMapHelperRef<TKey> CreateFor<TValue>(NativeHashMap<TKey, TValue> hashMap)
            where TValue : unmanaged
        {
            return new HashMapHelperRef<TKey>(hashMap.m_Data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashMapHelperRef<TKey> CreateFor(ref UnsafeHashSet<TKey> hashSet)
        {
            return new HashMapHelperRef<TKey>((HashMapHelper<TKey>*)AsPointer(ref hashSet.m_Data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashMapHelperRef<TKey> CreateFor(NativeHashSet<TKey> hashSet)
        {
            return new HashMapHelperRef<TKey>(hashSet.m_Data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void EnsureCapacity(int capacity, bool keepOldData = true)
        {
            CollectionHelper2.CheckContainerCapacity(capacity);

            if (ptr->Capacity < capacity)
            {
                Hint.Assume(capacity >= ptr->Count);
                Resize(capacity, keepOldData);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void EnsureSlack(int slack)
        {
            CollectionHelper2.CheckContainerCapacity(slack);
            EnsureCapacity(ptr->Count + slack);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Resize(int newCapacity, bool keepOldData = true)
        {
            newCapacity = math.max(newCapacity, ptr->Count);
            int newBucketCapacity = math.ceilpow2(HashMapHelper<TKey>.GetBucketSize(newCapacity));

            if (ptr->Capacity == newCapacity && ptr->BucketCapacity == newBucketCapacity)
            {
                return;
            }

            if (keepOldData)
            {
                ResizeExactKeepOldData(newCapacity, newBucketCapacity);
            }
            else
            {
                ResizeExactTrashOldData(newCapacity, newBucketCapacity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Clear()
        {
            ptr->Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void DisposeMap()
        {
            ptr->Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void DisposeMap(AllocatorManager.AllocatorHandle allocator)
        {
            Assert.AreEqual(ptr->Allocator, allocator);

            Memory.Unmanaged.Free(ptr->Ptr, allocator);
            ptr->Ptr = null;
            ptr->Keys = null;
            ptr->Next = null;
            ptr->Buckets = null;
            ptr->Count = 0;
            ptr->BucketCapacity = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool ContainsKey(TKey key)
        {
            return ptr->Find(key) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int Find(TKey key)
        {
            return ptr->Find(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int FindOrAddNoResize(TKey key)
        {
            int idx = ptr->Find(key);

            if (idx < 0)
            {
                idx = AddUncheckedNoResize(key);
            }

            return idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly TValue GetValue<TValue>(int idx)
            where TValue : unmanaged
        {
            CheckValueSize<TValue>();
            CollectionHelper.CheckIndexInRange(idx, ptr->Capacity);
            return UnsafeUtility.ReadArrayElement<TValue>(ptr->Ptr, idx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void SetValue<TValue>(int idx, TValue value)
            where TValue : unmanaged
        {
            CheckValueSize<TValue>();
            CollectionHelper.CheckIndexInRange(idx, ptr->Capacity);
            UnsafeUtility.WriteArrayElement(ptr->Ptr, idx, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref TValue GetValueRef<TValue>(int idx)
            where TValue : unmanaged
        {
            CheckValueSize<TValue>();
            UnsafeUtility2.CheckIsAligned<TValue>(ptr->Ptr);
            CollectionHelper.CheckIndexInRange(idx, ptr->Capacity);
            return ref UnsafeUtility.ArrayElementAsRef<TValue>(ptr->Ptr, idx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref TValue TryGetValueRef<TValue>(TKey key, out bool exists)
            where TValue : unmanaged
        {
            int idx = Find(key);

            exists = idx >= 0;
            return ref exists ? ref GetValueRef<TValue>(idx) : ref NullRef<TValue>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int AddUnchecked(TKey key)
        {
            if (ptr->AllocatedIndex >= Capacity && ptr->FirstFreeIdx < 0)
            {
                int newCap = ptr->CalcCapacityCeilPow2(Capacity + (1 << ptr->Log2MinGrowth));
                Resize(newCap);
            }

            // Allocate an entry from the free list
            int idx = ptr->FirstFreeIdx;

            if (idx >= 0)
            {
                ptr->FirstFreeIdx = ptr->Next[idx];
            }
            else
            {
                idx = ptr->AllocatedIndex++;
            }

            CheckIndexOutOfBounds(idx);

            UnsafeUtility.WriteArrayElement(ptr->Keys, idx, key);
            int bucket = GetBucket(key);

            // Add the index to the hashCode-map
            int* next = ptr->Next;
            next[idx] = ptr->Buckets[bucket];
            ptr->Buckets[bucket] = idx;
            ptr->Count++;

            return idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int AddUncheckedNoResize(TKey key)
        {
            // Allocate an entry from the free list
            int idx = ptr->FirstFreeIdx;

            if (idx >= 0)
            {
                ptr->FirstFreeIdx = ptr->Next[idx];
            }
            else
            {
                idx = ptr->AllocatedIndex++;
            }

            CheckIndexOutOfBounds(idx);

            UnsafeUtility.WriteArrayElement(ptr->Keys, idx, key);
            int bucket = GetBucket(key);

            // Add the index to the hashCode-map
            int* next = ptr->Next;
            next[idx] = ptr->Buckets[bucket];
            ptr->Buckets[bucket] = idx;
            ptr->Count++;

            return idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void AddUnchecked<TValue>(TKey key, TValue value)
            where TValue : unmanaged
        {
            int idx = AddUnchecked(key);
            SetValue(idx, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void AddUncheckedNoResize<TValue>(TKey key, TValue value)
            where TValue : unmanaged
        {
            int idx = AddUncheckedNoResize(key);
            SetValue(idx, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int TryAdd(TKey key, out bool added)
        {
            int idx = Find(key);

            if (idx >= 0)
            {
                added = false;
                return idx;
            }

            added = true;
            return AddUnchecked(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryAdd<TValue>(TKey key, TValue value)
            where TValue : unmanaged
        {
            int idx = TryAdd(key, out bool added);

            if (added)
            {
                SetValue(idx, value);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int TryAddNoResize(TKey key, out bool added)
        {
            int idx = Find(key);

            if (idx >= 0)
            {
                added = false;
                return idx;
            }

            added = true;
            return AddUncheckedNoResize(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryAddNoResize<TValue>(TKey key, TValue value)
            where TValue : unmanaged
        {
            int idx = TryAddNoResize(key, out bool added);

            if (added)
            {
                SetValue(idx, value);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref TValue GetOrAddValue<TValue>(TKey key, out bool added)
            where TValue : unmanaged
        {
            int idx = TryAdd(key, out added);
            return ref GetValueRef<TValue>(idx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref TValue GetOrAddValueNoResize<TValue>(TKey key, out bool added)
            where TValue : unmanaged
        {
            int idx = TryAddNoResize(key, out added);
            return ref GetValueRef<TValue>(idx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Remove(TKey key)
        {
            return ptr->TryRemove(key) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly UnsafeSpan<TKey> GetKeySpanRO()
        {
            return new UnsafeSpan<TKey>(ptr->Keys, ptr->Capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly UnsafeSpan<TValue> GetValueSpan<TValue>()
            where TValue : unmanaged
        {
            CheckValueSize<TValue>();
            UnsafeUtility2.CheckIsAligned<TValue>(ptr->Ptr);
            return new UnsafeSpan<TValue>((TValue*)ptr->Ptr, ptr->Capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            return ptr->GetKeyArray(allocator);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly int GetBucket(uint hashCode)
        {
            return (int)(hashCode & (ptr->BucketCapacity - 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly int GetBucket(TKey key)
        {
            uint hashCode = (uint)key.GetHashCode();
            return GetBucket(hashCode);
        }

        internal readonly void ResizeExactKeepOldData(int newCapacity, int newBucketCapacity)
        {
            ResizeExact(newCapacity, newBucketCapacity, keepOldData: true);
        }

        internal readonly void ResizeExactTrashOldData(int newCapacity, int newBucketCapacity)
        {
            ResizeExact(newCapacity, newBucketCapacity, keepOldData: false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void ResizeExact(int newCapacity, int newBucketCapacity, bool keepOldData)
        {
            long totalSize = HashMapHelper<TKey>.CalculateDataSize(newCapacity, newBucketCapacity, ptr->SizeOfTValue, out long keyOffset, out long nextOffset, out long bucketOffset);

            byte* oldPtr = ptr->Ptr;
            TKey* oldKeys = ptr->Keys;
            int* oldNext = ptr->Next;
            int* oldBuckets = ptr->Buckets;
            int oldBucketCapacity = ptr->BucketCapacity;

            ptr->Ptr = (byte*)Memory.Unmanaged.Allocate(totalSize, JobsUtility.CacheLineSize, Allocator);
            ptr->Keys = (TKey*)(ptr->Ptr + keyOffset);
            ptr->Next = (int*)(ptr->Ptr + nextOffset);
            ptr->Buckets = (int*)(ptr->Ptr + bucketOffset);
            ptr->Capacity = newCapacity;
            ptr->BucketCapacity = newBucketCapacity;

            ptr->Clear();

            if (keepOldData)
            {
                for (int i = 0, num = oldBucketCapacity; i < num; ++i)
                {
                    for (int idx = oldBuckets[i]; idx != -1; idx = oldNext[idx])
                    {
                        int newIdx = AddUncheckedNoResize(oldKeys[idx]);
                        UnsafeUtility.MemCpy(ptr->Ptr + (ptr->SizeOfTValue * newIdx), oldPtr + (ptr->SizeOfTValue * idx), ptr->SizeOfTValue);
                    }
                }
            }

            Memory.Unmanaged.Free(oldPtr, Allocator);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckIndexOutOfBounds(int idx)
        {
            if ((uint)idx >= (uint)ptr->Capacity)
            {
                throw new InvalidOperationException($"Internal HashMap error. idx {idx}");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckValueSize<TValue>()
            where TValue : unmanaged
        {
            if (sizeof(TValue) != ptr->SizeOfTValue)
            {
                throw new InvalidOperationException($"Invalid value size: {sizeof(TValue)} ({ptr->SizeOfTValue} expected).");
            }
        }
    }

    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(FixedString32Bytes) })]
    public static unsafe class HashMapHelperExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsNormalizedStringKey<TKey>(this HashMapHelperRef<TKey> self, ByteSpan key)
            where TKey : unmanaged, IEquatable<TKey>, INativeList<byte>, IUTF8Bytes
        {
            return FindNormalizedStringKeyIndex(self, key) >= 0;
        }

        public static int FindNormalizedStringKeyIndex<TKey>(this HashMapHelperRef<TKey> self, ByteSpan key)
            where TKey : unmanaged, IEquatable<TKey>, INativeList<byte>, IUTF8Bytes
        {
            CheckIsNotByteSpan<TKey>();

            if (self.ptr->AllocatedIndex > 0)
            {
                int hashCode = FixedStringMethods.ComputeHashCode(ref key);

                // First find the slot based on the hashCode
                int bucket = self.GetBucket((uint)hashCode);
                int entryIdx = self.ptr->Buckets[bucket];

                if ((uint)entryIdx < (uint)self.ptr->Capacity)
                {
                    int* nextPtrs = self.ptr->Next;

                    for (; ; )
                    {
                        ref TKey hashMapKey = ref self.ptr->Keys[entryIdx];
                        ByteSpan hashMapKeySpan = hashMapKey.AsByteSpan();

                        if (hashMapKeySpan.Equals(key))
                        {
                            break;
                        }

                        entryIdx = nextPtrs[entryIdx];

                        if ((uint)entryIdx >= (uint)self.ptr->Capacity)
                        {
                            return -1;
                        }
                    }

                    return entryIdx;
                }
            }

            return -1;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckIsNotByteSpan<T>()
            where T : unmanaged
        {
            if (BurstRuntime.GetHashCode64<T>() == BurstRuntime.GetHashCode64<ByteSpan>())
            {
                throw new NotSupportedException("Use regular ContainsKey.");
            }
        }
    }
}
