using System;
using System.Runtime.CompilerServices;

namespace Unity.Collections
{
    public static unsafe partial class CollectionUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindOrderedInsertionIndex<T>(T* ptr, int length, T value)
            where T : unmanaged, IComparable<T>
        {
            if (length == 0)
            {
                return 0;
            }

            int index = length - 1;

            for (; ; --index)
            {
                int cmp = value.CompareTo(ptr[index]);

                if (cmp >= 0)
                {
                    break;
                }
                else if (index == 0)
                {
                    return 0;
                }
            }

            return index + 1;
        }
    }
}
