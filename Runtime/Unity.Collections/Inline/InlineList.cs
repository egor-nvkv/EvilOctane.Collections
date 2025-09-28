using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static Unity.Collections.CollectionHelper;
using static Unity.Collections.CollectionHelper2;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;

namespace Unity.Collections.LowLevel.Unsafe
{
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
    public unsafe struct InlineList<T>
        where T : unmanaged
    {
        public static int Alignment
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                int headerAlignment = AlignOf<InlineListHeader<T>>();
                int elementAlignment = AlignOf<T>();

                return math.max(headerAlignment, elementAlignment);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint GetTotalAllocationSize(int capacity)
        {
            CheckContainerCapacity(capacity);

            nint elementOffset = Align(sizeof(InlineListHeader<T>), (nint)AlignOf<T>());
            return elementOffset + (sizeof(T) * capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint GetActualAllocationSize(InlineListHeader<T>* header)
        {
            return GetTotalAllocationSize(header->Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Create(InlineListHeader<T>* header, int capacity)
        {
            int valueAlignment = AlignOf<T>();
            CheckIsAligned(header, valueAlignment);

            header->Length = 0;
            header->Capacity = capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* GetElementPointer(InlineListHeader<T>* header)
        {
            byte* afterHeaderPtr = (byte*)header + sizeof(InlineListHeader<T>);
            return (T*)AlignPointer(afterHeaderPtr, AlignOf<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddNoResize(InlineListHeader<T>* header, T item)
        {
            CheckAddNoResizeHasEnoughCapacity(header->Length, header->Capacity, 1);

            GetElementPointer(header)[header->Length] = item;
            ++header->Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddRangeNoResize(InlineListHeader<T>* header, UnsafeSpan<T> items)
        {
            CheckAddNoResizeHasEnoughCapacity(header->Length, header->Capacity, items.Length);

            T* ptr = GetElementPointer(header) + header->Length;
            header->Length += items.Length;

            new UnsafeSpan<T>(ptr, items.Length).CopyFrom(items);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveAtSwapBack(InlineListHeader<T>* header, int index)
        {
            CheckIndexInRange(index, header->Length);

            T* ptr = GetElementPointer(header);
            ref int length = ref header->Length;

            ptr[index] = ptr[length - 1];
            --length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeSpan<T> AsSpan(InlineListHeader<T>* header)
        {
            return new UnsafeSpan<T>(GetElementPointer(header), header->Length);
        }
    }
}
