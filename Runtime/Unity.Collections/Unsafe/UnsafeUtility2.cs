using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static System.Runtime.CompilerServices.Unsafe;

namespace Unity.Collections.LowLevel.Unsafe
{
    [GenerateTestsForBurstCompatibility]
    public static unsafe partial class UnsafeUtility2
    {
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(float) })]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TDestination Reinterpret<TSource, TDestination>(ref TSource source)
            where TSource : unmanaged
            where TDestination : unmanaged
        {
            CheckReinterpretArgs<TSource, TDestination>();
            return ref As<TSource, TDestination>(ref source);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckReinterpretArgs<TSource, TDestination>()
            where TSource : unmanaged
            where TDestination : unmanaged
        {
            int sizeSource = sizeof(TSource);
            int sizeDestination = sizeof(TDestination);

            if (sizeSource != sizeDestination)
            {
                throw new InvalidOperationException($"Cannot reinterpret between types of different sizes: source = {sizeSource}, destination = {sizeDestination}.");
            }

            int alignSource = UnsafeUtility.AlignOf<TSource>();
            int alignDestination = UnsafeUtility.AlignOf<TDestination>();

            if (alignDestination > alignSource)
            {
                throw new InvalidOperationException($"Cannot reinterpret to over-aligned type: source = {alignSource}, destination = {alignDestination}.");
            }
        }
    }
}
