using System;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
    public static unsafe partial class CollectionUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindOrderedInsertionIndex<T>(T* ptr, int length, T value, out bool exists)
            where T : unmanaged, IComparable<T>
        {
            if (length == 0)
            {
                // Empty
                exists = false;
                return 0;
            }

            int index = length - 1;

            for (; ; --index)
            {
                int cmp = value.CompareTo(ptr[index]);

                if (cmp == 0)
                {
                    // Exists
                    exists = true;
                    break;
                }
                else if (cmp > 0)
                {
                    // Does not exist
                    exists = false;
                    break;
                }
                else if (index == 0)
                {
                    // Add at the beginning
                    exists = false;
                    return 0;
                }
            }

            return index + 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindOrderedInsertionIndex<T>(UnsafeSpan<T> span, T value, out bool exists)
            where T : unmanaged, IComparable<T>
        {
            return FindOrderedInsertionIndex(span.Ptr, span.Length, value, out exists);
        }
    }
}
