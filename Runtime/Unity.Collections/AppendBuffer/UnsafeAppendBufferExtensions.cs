using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using UnityEngine.Assertions;
using static System.Runtime.CompilerServices.Unsafe;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe class UnsafeAppendBufferExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureCapacity(this ref UnsafeAppendBuffer self, int capacity)
        {
            if (capacity > self.Capacity)
            {
                ref UntypedUnsafeListMutable casted = ref As<UnsafeAppendBuffer, UntypedUnsafeListMutable>(ref self);
                MemoryExposed.IncreaseListCapacityNoInline(ref casted, elementSize: sizeof(byte), elementAlignment: self.Alignment, capacity: capacity);
            }

            Assert.IsTrue(self.Capacity >= capacity);
            Hint.Assume(self.Capacity >= capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureSlack(this ref UnsafeAppendBuffer self, int slack)
        {
            self.EnsureCapacity(self.Length + slack);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddNoResize<T>(this ref UnsafeAppendBuffer self, T value)
            where T : unmanaged
        {
            int oldLength = self.Length;
            int newLength = oldLength + sizeof(T);

            CheckCreatedAndHasEnoughCapacity(self, newLength);

            self.Length = newLength;
            Override(self, oldLength, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddNoResize<T0, T1>(this ref UnsafeAppendBuffer self, T0 value0, T1 value1)
            where T0 : unmanaged
            where T1 : unmanaged
        {
            int oldLength = self.Length;
            int newLength = oldLength + sizeof(T0) + sizeof(T1);

            CheckCreatedAndHasEnoughCapacity(self, newLength);

            self.Length = newLength;
            Override(self, oldLength, value0);
            Override(self, oldLength + sizeof(T0), value1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Override<T>(this UnsafeAppendBuffer self, int offset, T value)
            where T : unmanaged
        {
            CheckCreatedAndHasEnoughCapacity(self, offset + sizeof(T));
            CollectionHelper.CheckIndexInRange(offset + sizeof(T) - 1, self.Length);

            byte* destination = self.Ptr + offset;
            UnsafeUtility.MemCpy(destination, &value, sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Peek<T>(this ref UnsafeAppendBuffer.Reader self)
            where T : unmanaged
        {
            T res = self.ReadNextFast<T>();
            self.Offset -= sizeof(T);
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Skip<T>(this ref UnsafeAppendBuffer.Reader self)
            where T : unmanaged
        {
            Skip(ref self, sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Skip(this ref UnsafeAppendBuffer.Reader self, int byteCount)
        {
            CheckBounds(self.Offset, self.Size, byteCount);
            self.Offset += byteCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Backup<T>(this ref UnsafeAppendBuffer.Reader self)
            where T : unmanaged
        {
            Backup(ref self, sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Backup(this ref UnsafeAppendBuffer.Reader self, int byteCount)
        {
            CheckBounds(self.Offset, self.Size, -byteCount);
            self.Offset -= byteCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadNextFast<T>(this ref UnsafeAppendBuffer.Reader self)
            where T : unmanaged
        {
            CheckBounds(self.Offset, self.Size, sizeof(T));

            T value;
            void* ptr = self.Ptr + self.Offset;
            UnsafeUtility.MemCpy(&value, ptr, sizeof(T));

            self.Offset += sizeof(T);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadNext<T>(this ref UnsafeAppendBuffer.Reader self, out T value)
            where T : unmanaged
        {
            if (self.Offset > self.Size - sizeof(T))
            {
                SkipInit(out value);
                return false;
            }

            value = self.ReadNextFast<T>();
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadAddToNoResize<T>(this ref UnsafeAppendBuffer.Reader self, ref UnsafeAppendBuffer destination)
            where T : unmanaged
        {
            ReadAddToNoResizeUnchecked(ref self, ref destination, sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadAddToNoResizeUnchecked(this ref UnsafeAppendBuffer.Reader self, ref UnsafeAppendBuffer destination, int size)
        {
            void* ptr = self.ReadNext(size);
            AddNoResize(ref destination, ptr, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddNoResize(ref UnsafeAppendBuffer unsafeAppendBuffer, void* ptr, int size)
        {
            int oldLength = unsafeAppendBuffer.Length;
            int newLength = oldLength + size;

            CheckCreatedAndHasEnoughCapacity(unsafeAppendBuffer, newLength);

            unsafeAppendBuffer.Length = newLength;
            Override(unsafeAppendBuffer, oldLength, ptr, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Override(UnsafeAppendBuffer unsafeAppendBuffer, int offset, void* ptr, int size)
        {
            CollectionHelper.CheckIndexInRange(offset + size - 1, unsafeAppendBuffer.Length);
            UnsafeUtility.MemCpy(unsafeAppendBuffer.Ptr + offset, ptr, size);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckCreatedAndHasEnoughCapacity(UnsafeAppendBuffer buffer, int length)
        {
            if (!buffer.IsCreated)
            {
                throw new InvalidOperationException("UnsafeAppendBuffer is not created.");
            }

            int capacity = buffer.Capacity;

            if (capacity < length)
            {
                throw new InvalidOperationException($"UnsafeAppendBuffer does not have enough capacity. Requested: {length}, available: {capacity}.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckBounds(int offset, int size, int byteCount)
        {
            if (offset + byteCount > size)
            {
                throw new ArgumentException($"Requested value outside bounds of UnsafeAppendOnlyBuffer. Remaining bytes: {size - offset} Requested: {byteCount}");
            }
        }
    }
}
