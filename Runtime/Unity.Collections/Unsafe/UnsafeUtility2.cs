using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using static System.Runtime.CompilerServices.Unsafe;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe partial class UnsafeUtility2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanBeReinterpretedExactly<TSource, TDestination>()
            where TSource : unmanaged
            where TDestination : unmanaged
        {
            return
                sizeof(TSource) == sizeof(TDestination) &&
                UnsafeUtility.AlignOf<TSource>() == UnsafeUtility.AlignOf<TDestination>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TDestination Reinterpret<TSource, TDestination>(ref TSource source)
            where TSource : unmanaged
            where TDestination : unmanaged
        {
            CheckReinterpretArgs<TSource, TDestination>(false, false);
            return ref As<TSource, TDestination>(ref source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TDestination* Reinterpret<TSource, TDestination>(TSource* source)
            where TSource : unmanaged
            where TDestination : unmanaged
        {
            CheckReinterpretArgs<TSource, TDestination>(false, false);
            return (TDestination*)source;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TDestination ReinterpretExact<TSource, TDestination>(ref TSource source)
            where TSource : unmanaged
            where TDestination : unmanaged
        {
            CheckReinterpretArgs<TSource, TDestination>(true, true);
            return ref As<TSource, TDestination>(ref source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TDestination* ReinterpretExact<TSource, TDestination>(TSource* source)
            where TSource : unmanaged
            where TDestination : unmanaged
        {
            CheckReinterpretArgs<TSource, TDestination>(true, true);
            return (TDestination*)source;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckReinterpretArgs<TSource, TDestination>(bool exactSize = true, bool exactAlignment = true)
            where TSource : unmanaged
            where TDestination : unmanaged
        {
            // Size
            {
                int sizeSource = sizeof(TSource);
                int sizeDestination = sizeof(TDestination);

                if (exactSize)
                {
                    if (sizeSource != sizeDestination)
                    {
                        throw new InvalidOperationException($"Cannot reinterpret between types of different sizes: Source = {sizeSource}, Destination = {sizeDestination}.");
                    }
                }
                else
                {
                    if (sizeDestination > sizeSource)
                    {
                        throw new InvalidOperationException($"Cannot reinterpret to a bigger type: Source = {sizeSource}, Destination = {sizeDestination}.");
                    }
                }
            }

            // Alignment
            {
                int alignSource = UnsafeUtility.AlignOf<TSource>();
                int alignDestination = UnsafeUtility.AlignOf<TDestination>();

                if (exactAlignment)
                {
                    if (alignSource != alignDestination)
                    {
                        throw new InvalidOperationException($"Cannot reinterpret between types of different alignments: Source = {alignSource}, Destination = {alignDestination}.");
                    }
                }
                else
                {
                    if (alignDestination > alignSource)
                    {
                        throw new InvalidOperationException($"Cannot reinterpret to an over-aligned type: Source = {alignSource}, Destination = {alignDestination}.");
                    }
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckPointerIsNotNull<T>(T* ptr)
            where T : unmanaged
        {
            if (Hint.Unlikely(ptr == null))
            {
                throw new NullReferenceException("Pointer is null.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckIsAligned<T>(void* ptr)
            where T : unmanaged
        {
            CheckIsAligned(ptr, UnsafeUtility.AlignOf<T>());
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckIsAligned(void* ptr, int alignment)
        {
            CollectionHelper.CheckIntPositivePowerOfTwo(alignment);

            if (Hint.Unlikely(!CollectionHelper.IsAligned(ptr, alignment)))
            {
                throw new InvalidOperationException("Pointer does not have required alignment.");
            }
        }
    }
}
