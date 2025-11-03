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
            CheckReinterpretArgs<TSource, TDestination>();
            return ref As<TSource, TDestination>(ref source);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TDestination* Reinterpret<TSource, TDestination>(TSource* source)
            where TSource : unmanaged
            where TDestination : unmanaged
        {
            CheckReinterpretArgs<TSource, TDestination>();
            return (TDestination*)source;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckReinterpretArgs<TSource, TDestination>(bool requireExactAlignment = false)
            where TSource : unmanaged
            where TDestination : unmanaged
        {
            // Size
            {
                int sizeSource = sizeof(TSource);
                int sizeDestination = sizeof(TDestination);

                if (sizeSource != sizeDestination)
                {
                    throw new InvalidOperationException($"Cannot reinterpret between types of different sizes: source = {sizeSource}, destination = {sizeDestination}.");
                }
            }

            // Alignment
            {
                int alignSource = UnsafeUtility.AlignOf<TSource>();
                int alignDestination = UnsafeUtility.AlignOf<TDestination>();

                if (requireExactAlignment)
                {
                    if (alignSource != alignDestination)
                    {
                        throw new InvalidOperationException($"Cannot reinterpret between types of different alignments: source = {alignSource}, destination = {alignDestination}.");
                    }
                }
                else
                {
                    if (alignDestination > alignSource)
                    {
                        throw new InvalidOperationException($"Cannot reinterpret to an over-aligned type: source = {alignSource}, destination = {alignDestination}.");
                    }
                }
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
