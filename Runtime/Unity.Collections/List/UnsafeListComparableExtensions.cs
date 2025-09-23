using System;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe class UnsafeListComparableExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int InsertedOrderedNoResize<T>(this ref UnsafeList<T> self, T value)
            where T : unmanaged, IComparable<T>
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (self.Capacity <= self.Length)
            {
                // Throws
                self.AddNoResize(default);
            }
#endif

            Assert.IsTrue(self.Capacity > self.Length);

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
