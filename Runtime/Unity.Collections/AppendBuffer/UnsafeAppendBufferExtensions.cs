using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.Unsafe;
using static Unity.Collections.CollectionHelper;
using static Unity.Collections.CollectionHelper2;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe partial class UnsafeAppendBufferExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureCapacity(this ref UnsafeAppendBuffer self, int capacity, bool keepOldData = true)
        {
            CheckContainerCapacity(capacity);

            if (capacity > self.Capacity)
            {
                ref UntypedUnsafeListMutable casted = ref ReinterpretExact<UnsafeAppendBuffer, UntypedUnsafeListMutable>(ref self);

                if (keepOldData)
                {
                    MemoryExposed.IncreaseListCapacityKeepOldData(ref casted, elementSize: sizeof(byte), elementAlignment: self.Alignment, capacity: capacity);
                }
                else
                {
                    MemoryExposed.IncreaseListCapacityTrashOldData(ref casted, elementSize: sizeof(byte), elementAlignment: self.Alignment, capacity: capacity);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureSlack(this ref UnsafeAppendBuffer self, int slack)
        {
            CheckContainerElementCount(slack);
            self.EnsureCapacity(self.Length + slack);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddNoResize<T>(this ref UnsafeAppendBuffer self, T value)
            where T : unmanaged
        {
            CheckAddNoResizeHasEnoughCapacity(self.Length, self.Capacity, sizeof(T));

            int oldLength = self.Length;
            self.Length = oldLength + sizeof(T);

            Overwrite(self, oldLength, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddNoResize<T0, T1>(this ref UnsafeAppendBuffer self, T0 value0, T1 value1)
            where T0 : unmanaged
            where T1 : unmanaged
        {
            CheckAddNoResizeHasEnoughCapacity(self.Length, self.Capacity, sizeof(T0) + sizeof(T1));

            int oldLength = self.Length;
            self.Length = oldLength + sizeof(T0) + sizeof(T1);

            Overwrite(self, oldLength, value0);
            Overwrite(self, oldLength + sizeof(T0), value1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Overwrite<T>(this UnsafeAppendBuffer self, int offset, T value)
            where T : unmanaged
        {
            CheckContainerElementCount(offset);
            CheckContainerIndexInRange(offset + sizeof(T) - 1, self.Length);

            byte* destination = self.Ptr + offset;
            WriteUnaligned(destination, value);
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
            CheckContainerElementCount(byteCount);
            CheckContainerIndexInRange(self.Offset + byteCount - 1, self.Size);
            self.Offset += byteCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadNextFast<T>(this ref UnsafeAppendBuffer.Reader self)
            where T : unmanaged
        {
            CheckIndexInRange(self.Offset + sizeof(T) - 1, self.Size);

            void* ptr = self.Ptr + self.Offset;
            T value = ReadUnaligned<T>(ptr);

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
        public static void ReadAddToNoResizeUnchecked(this ref UnsafeAppendBuffer.Reader self, ref UnsafeAppendBuffer destination, int size)
        {
            CheckContainerElementSize(size);
            CheckContainerIndexInRange(self.Offset + size - 1, self.Size);

            void* ptr = self.Ptr + self.Offset;
            self.Offset += size;

            AddNoResize(ref destination, ptr, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddNoResize(ref UnsafeAppendBuffer unsafeAppendBuffer, void* ptr, int size)
        {
            CheckContainerElementSize(size);
            CheckAddNoResizeHasEnoughCapacity(unsafeAppendBuffer.Length, unsafeAppendBuffer.Capacity, size);

            int oldLength = unsafeAppendBuffer.Length;
            unsafeAppendBuffer.Length = oldLength + size;

            Overwrite(unsafeAppendBuffer, oldLength, ptr, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Overwrite(UnsafeAppendBuffer unsafeAppendBuffer, int offset, void* ptr, int size)
        {
            CheckContainerElementSize(size);
            CheckContainerIndexInRange(offset + size - 1, unsafeAppendBuffer.Length);
            UnsafeUtility.MemCpy(unsafeAppendBuffer.Ptr + offset, ptr, size);
        }
    }
}
