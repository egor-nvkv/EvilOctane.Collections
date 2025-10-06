using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static System.Runtime.CompilerServices.Unsafe;
using static Unity.Collections.CollectionHelper;
using static Unity.Collections.CollectionHelper2;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;

namespace Unity.Collections.LowLevel.Unsafe
{
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
    public unsafe struct InlineHashMap<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        public static int Alignment
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                int alignment = math.max(AlignOf<InlineHashMapHeader<TKey>>(), AlignOf<TValue>());
                alignment = math.max(alignment, AlignOf<TKey>());
                alignment = math.max(alignment, AlignOf<int>());

                return alignment;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint GetTotalAllocationSize(int capacity)
        {
            CheckContainerCapacity(capacity);

            int bucketCapacity = GetBucketCapacity(capacity);
            nint bucketTotalSize = bucketCapacity * sizeof(int);

            return GetBucketOffset(capacity) + bucketTotalSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Create(InlineHashMapHeader<TKey>* header, int capacity)
        {
            CheckIsAligned(header, AlignOf<TValue>());
            CheckIsAligned(header, AlignOf<TKey>());
            CheckIsAligned(header, AlignOf<int>());

            header->Count = 0;
            header->Capacity = capacity;
            header->BucketCapacity = GetBucketCapacity(capacity);

            Clear(header);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear(InlineHashMapHeader<TKey>* header)
        {
            int* nextPtr = GetNextPtr(header);

            int bucketSize = header->BucketCapacity * sizeof(int);
            int* bucketEndPtr = GetBucketPtr(header) + bucketSize;

            for (int index = 0, length = (int)(bucketEndPtr - nextPtr); index != length; ++index)
            {
                *(nextPtr + index) = -1;
            }

            header->Count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsKey(InlineHashMapHeader<TKey>* header, TKey key)
        {
            return Find(header, key) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Find(InlineHashMapHeader<TKey>* header, TKey key)
        {
            if (header->Count == 0)
            {
                return -1;
            }

            // First find the slot based on the hash
            int bucket = GetBucket(header, key);
            int entryIdx = GetBucketPtr(header)[bucket];

            if ((uint)entryIdx >= (uint)header->Capacity)
            {
                return -1;
            }

            TKey* keyPtr = GetKeyPtr(header);
            int* nextPtr = GetNextPtr(header);

            while (!keyPtr[entryIdx].Equals(key))
            {
                entryIdx = nextPtr[entryIdx];

                if ((uint)entryIdx >= (uint)header->Capacity)
                {
                    return -1;
                }
            }

            return entryIdx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindOrAddNoResize(InlineHashMapHeader<TKey>* header, TKey key)
        {
            int idx = Find(header, key);

            if (idx < 0)
            {
                idx = AddUncheckedNoResize(header, key);
            }

            return idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue GetValue(InlineHashMapHeader<TKey>* header, int idx)
        {
            return GetValueRef(header, idx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetValue(InlineHashMapHeader<TKey>* header, int idx, TValue value)
        {
            CheckIndexInRange(idx, header->Capacity);
            GetValuePtr(header)[idx] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetValueRef(InlineHashMapHeader<TKey>* header, int idx)
        {
            CheckIndexInRange(idx, header->Capacity);
            return ref GetValuePtr(header)[idx];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValue(InlineHashMapHeader<TKey>* header, TKey key, out TValue value)
        {
            ref TValue valueRef = ref TryGetValueRef(header, key, out bool exists);

            if (!exists)
            {
                SkipInit(out value);
                return false;
            }

            value = valueRef;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue TryGetValueRef(InlineHashMapHeader<TKey>* header, TKey key, out bool exists)
        {
            int idx = Find(header, key);

            exists = idx >= 0;
            return ref exists ? ref GetValueRef(header, idx) : ref NullRef<TValue>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AddUncheckedNoResize(InlineHashMapHeader<TKey>* header, TKey key)
        {
            CheckAddNoResizeHasEnoughCapacity(header->Count, header->Capacity, 1);
            int idx = header->Count++;

            GetKeyPtr(header)[idx] = key;
            int bucket = GetBucket(header, key);

            // Add the index to the hashCode-map
            int* nextPtr = GetNextPtr(header);
            int* next = nextPtr;

            int* bucketPtr = GetBucketPtr(header);
            next[idx] = bucketPtr[bucket];

            bucketPtr[bucket] = idx;
            return idx;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddUncheckedNoResize(InlineHashMapHeader<TKey>* header, TKey key, TValue value)
        {
            int idx = AddUncheckedNoResize(header, key);
            SetValue(header, idx, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TryAddNoResize(InlineHashMapHeader<TKey>* header, TKey key, out bool added)
        {
            int idx = Find(header, key);

            if (idx >= 0)
            {
                added = false;
                return idx;
            }

            added = true;
            return AddUncheckedNoResize(header, key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAddNoResize(InlineHashMapHeader<TKey>* header, TKey key, TValue value)
        {
            int idx = TryAddNoResize(header, key, out bool added);

            if (added)
            {
                SetValue(header, idx, value);
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TValue GetOrAddValueNoResize(InlineHashMapHeader<TKey>* header, TKey key, out bool added)
        {
            int idx = TryAddNoResize(header, key, out added);
            return ref GetValueRef(header, idx);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Enumerator GetEnumerator(InlineHashMapHeader<TKey>* header)
        {
            return new Enumerator(header);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetBucketCapacity(int capacity)
        {
            return math.ceilpow2(capacity) * 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetBucket(InlineHashMapHeader<TKey>* header, uint hashCode)
        {
            int mask = header->BucketCapacity - 1;
            return (int)(hashCode & mask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetBucket(InlineHashMapHeader<TKey>* header, TKey key)
        {
            uint hashCode = (uint)key.GetHashCode();
            return GetBucket(header, hashCode);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static nint GetValueOffset()
        {
            return Align((nint)sizeof(InlineHashMapHeader<TKey>), AlignOf<TValue>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static nint GetKeyOffset(int capacity)
        {
            return Align(GetValueOffset() + (capacity * sizeof(TValue)), AlignOf<TKey>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static nint GetNextOffset(int capacity)
        {
            return Align(GetKeyOffset(capacity) + (capacity * sizeof(TKey)), AlignOf<int>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static nint GetBucketOffset(int capacity)
        {
            return Align(GetNextOffset(capacity) + (capacity * sizeof(int)), AlignOf<int>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static TValue* GetValuePtr(InlineHashMapHeader<TKey>* header)
        {
            return (TValue*)((byte*)header + GetValueOffset());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static TKey* GetKeyPtr(InlineHashMapHeader<TKey>* header)
        {
            return (TKey*)((byte*)header + GetKeyOffset(header->Capacity));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int* GetNextPtr(InlineHashMapHeader<TKey>* header)
        {
            return (int*)((byte*)header + GetNextOffset(header->Capacity));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int* GetBucketPtr(InlineHashMapHeader<TKey>* header)
        {
            return (int*)((byte*)header + GetBucketOffset(header->Capacity));
        }

        public struct Enumerator : IEnumerator<InlineHashMapKVPair<TKey, TValue>>
        {
            [NativeDisableUnsafePtrRestriction]
            internal InlineHashMapHeader<TKey>* header;

            internal int index;
            internal int bucketIndex;
            internal int nextIndex;

            public InlineHashMapKVPair<TKey, TValue> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => new()
                {
                    header = header,
                    index = index
                };
            }

            object IEnumerator.Current => Current;

            internal Enumerator(InlineHashMapHeader<TKey>* header)
            {
                this.header = header;
                index = -1;
                bucketIndex = 0;
                nextIndex = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (nextIndex != -1)
                {
                    index = nextIndex;
                    nextIndex = GetNextPtr(header)[nextIndex];
                    return true;
                }

                return MoveNextSearch();
            }

            public void Reset()
            {
                index = -1;
                bucketIndex = 0;
                nextIndex = -1;
            }

            public void Dispose()
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal bool MoveNextSearch()
            {
                for (int index = bucketIndex; index != header->BucketCapacity; ++index)
                {
                    int idx = GetBucketPtr(header)[index];

                    if (idx != -1)
                    {
                        this.index = idx;
                        bucketIndex = index + 1;
                        nextIndex = GetNextPtr(header)[idx];

                        return true;
                    }
                }

                index = -1;
                bucketIndex = header->BucketCapacity;
                nextIndex = -1;
                return false;
            }
        }
    }
}
