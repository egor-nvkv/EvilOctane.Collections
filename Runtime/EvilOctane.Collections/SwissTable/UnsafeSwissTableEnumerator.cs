using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst.Intrinsics;
using Unity.Collections.LowLevel.Unsafe;
using static EvilOctane.Collections.SwissTable;
using static System.Runtime.CompilerServices.Unsafe;
using static Unity.Burst.Intrinsics.X86.Bmi1;
using static Unity.Burst.Intrinsics.X86.Sse2;
using static Unity.Burst.Intrinsics.X86.Sse4_2;
using static Unity.Collections.CollectionHelper2;

namespace EvilOctane.Collections.LowLevel.Unsafe
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct UnsafeSwissTableEnumerator<TKey, TValue> : IEnumerator<Pointer<KeyValue<TKey, TValue>>>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        internal readonly byte* controlPtr;
        [NativeDisableUnsafePtrRestriction]
        internal readonly KeyValue<TKey, TValue>* groupPtr;

        internal readonly int capacity;
        internal int index;

        internal uint fullMaskCache;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal UnsafeSwissTableEnumerator(byte* controlPtr, KeyValue<TKey, TValue>* groupPtr, int capacity)
        {
            this.controlPtr = controlPtr;
            this.groupPtr = groupPtr;
            this.capacity = capacity;
            index = -1;
            SkipInit(out fullMaskCache);
        }

        public readonly Pointer<KeyValue<TKey, TValue>> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                CheckContainerIndexInRange(index, capacity);
                return groupPtr + index;
            }
        }

        readonly object IEnumerator.Current => throw new NotSupportedException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            for (; ; )
            {
                ++index;

                if (index >= capacity)
                {
                    // Finished
                    return false;
                }

                if (IsSse42Supported)
                {
                    int groupIndex = index / GroupSize;

                    if (index % GroupSize == 0)
                    {
                        // First time visiting this group
                        v128 cntrl = load_si128(controlPtr + (index * sizeof(byte)));
                        fullMaskCache = (uint)(~movemask_epi8(cntrl) & 0xffff);
                    }
                    else
                    {
                        // Find next Full
                        fullMaskCache = blsr_u32(fullMaskCache);
                    }

                    if (fullMaskCache == 0x0)
                    {
                        // To next group
                        index = ((groupIndex + 1) * GroupSize) - 1;
                        continue;
                    }

                    int groupOffset = (int)tzcnt_u32(fullMaskCache);
                    index = (groupIndex * GroupSize) + groupOffset;

                    return true;
                }
                else
                {
                    bool isFull = (controlPtr[index] & ~ControlFullMask) == 0x0;

                    if (isFull)
                    {
                        return true;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            index = -1;
        }

        public readonly void Dispose()
        {
        }
    }
}
