using System;
using System.Runtime.CompilerServices;
using static Unity.Collections.CollectionHelper2;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe partial class UnsafeListComparableExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int InsertedOrderedNoResize<T>(this ref UnsafeList<T> self, T value)
            where T : unmanaged, IComparable<T>
        {
            CheckAddNoResizeHasEnoughCapacity(self.Length, self.Capacity, 1);

            int oldLength = self.m_length;
            ++self.m_length;

            int index = CollectionUtility.FindOrderedInsertionIndex(self.Ptr, oldLength, value);
            int countToMove = oldLength - index;

            if (countToMove != 0)
            {
                UnsafeUtility.MemMove(self.Ptr + index + 1, self.Ptr + index, countToMove * sizeof(T));
            }

            self.Ptr[index] = value;
            return index;
        }
    }
}
