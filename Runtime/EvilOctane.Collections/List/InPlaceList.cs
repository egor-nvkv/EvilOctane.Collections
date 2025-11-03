using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using static Unity.Collections.CollectionHelper;
using static Unity.Collections.CollectionHelper2;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;

namespace EvilOctane.Collections.LowLevel.Unsafe
{
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
    public unsafe struct InPlaceList<T>
        where T : unmanaged
    {
        public const int MaxCapacity = int.MaxValue;

        public static int BufferAlignment
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                int headerAlignment = AlignOf<InPlaceListHeader<T>>();
                int elementAlignment = AlignOf<T>();

                return math.max(headerAlignment, elementAlignment);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static nint GetAllocationSize(int capacity)
        {
            CheckContainerCapacity(capacity);

            nint elementOffset = Align(sizeof(InPlaceListHeader<T>), AlignOf<T>());
            return elementOffset + (capacity * sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Create(InPlaceListHeader<T>* header, int capacity)
        {
            int valueAlignment = AlignOf<T>();
            CheckIsAligned(header, valueAlignment);

            header->Length = 0;
            header->Capacity = capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* GetElementPointer(InPlaceListHeader<T>* header)
        {
            byte* afterHeaderPtr = (byte*)header + sizeof(InPlaceListHeader<T>);
            return (T*)AlignPointer(afterHeaderPtr, AlignOf<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddNoResize(InPlaceListHeader<T>* header, T item)
        {
            CheckAddNoResizeHasEnoughCapacity(header->Length, header->Capacity, 1);

            GetElementPointer(header)[header->Length] = item;
            ++header->Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddRangeNoResize(InPlaceListHeader<T>* header, UnsafeSpan<T> items)
        {
            CheckAddNoResizeHasEnoughCapacity(header->Length, header->Capacity, items.Length);

            T* ptr = GetElementPointer(header) + header->Length;
            header->Length += items.Length;

            new UnsafeSpan<T>(ptr, items.Length).CopyFrom(items);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveAtSwapBack(InPlaceListHeader<T>* header, int index)
        {
            CheckContainerIndexInRange(index, header->Length);

            T* ptr = GetElementPointer(header);
            ref int length = ref header->Length;

            ptr[index] = ptr[length - 1];
            --length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeSpan<T> AsSpan(InPlaceListHeader<T>* header)
        {
            return new UnsafeSpan<T>(GetElementPointer(header), header->Length);
        }
    }
}
