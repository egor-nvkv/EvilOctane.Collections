using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.Unsafe;

namespace Unity.Collections.LowLevel.Unsafe
{
    public readonly unsafe ref struct HashMapHelperRef<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        internal readonly HashMapHelper<TKey>* ptr;

        public readonly bool IsCreated => ptr != null;
        public readonly bool IsEmpty => ptr->IsEmpty;

        public readonly int Count => ptr->Count;
        public readonly int Capacity => ptr->Capacity;

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
        public readonly void EnsureCapacity(int capacity)
        {
            if (ptr->Capacity < capacity)
            {
                ptr->Resize(capacity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Clear()
        {
            ptr->Clear();
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
        public readonly void AddUncheckedNoResize<TValue>(TKey key, TValue value)
            where TValue : unmanaged
        {
            int idx = AddUncheckedNoResize(key);
            SetValue(idx, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void AddRangeUncheckedNoResize(TKey* keys, int length)
        {
            for (int index = 0; index != length; ++index)
            {
                _ = AddUncheckedNoResize(keys[index]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void AddRangeUncheckedNoResize(UnsafeList<TKey> keys)
        {
            AddRangeUncheckedNoResize(keys.Ptr, keys.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void AddRangeUncheckedNoResize(NativeArray<TKey> keys)
        {
            AddRangeUncheckedNoResize((TKey*)keys.GetUnsafeReadOnlyPtr(), keys.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int TryAddNoResize(TKey key)
        {
            return ContainsKey(key) ? -1 : AddUncheckedNoResize(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryAddNoResize<TValue>(TKey key, TValue value)
            where TValue : unmanaged
        {
            int idx = TryAddNoResize(key);

            if (idx >= 0)
            {
                SetValue(idx, value);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Remove(TKey key)
        {
            return ptr->TryRemove(key) >= 0;
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
