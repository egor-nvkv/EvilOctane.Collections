using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using static EvilOctane.Collections.SwissTable;
using static Unity.Collections.CollectionHelper;
using static Unity.Collections.CollectionHelper2;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility;
using static Unity.Collections.LowLevel.Unsafe.UnsafeUtility2;

namespace EvilOctane.Collections
{
    /// <summary>
    /// SIMD compatible hash set.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <remarks>
    /// <see href="https://www.youtube.com/watch?v=ncHmEUmJZf4"/>
    /// </remarks>
    public unsafe struct SwissSet<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        public static int BufferAlignment
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => math.max(ControlAlignment, KeyGroupAlignment);
        }

        public static int KeyGroupAlignment
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                const int halfCacheLine = CacheLineSize / 2;

                int groupAlign = AlignOf<TKey>();
                int groupSize = sizeof(TKey) * GroupSize;

                if (groupSize <= halfCacheLine)
                {
                    // Align 32
                    return math.max(groupAlign, halfCacheLine);
                }
                else
                {
                    // Align 64
                    return math.max(groupAlign, CacheLineSize);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TKey* GetKeyGroupPtr(byte* buffer, int capacityCeilGroupSize)
        {
            CheckCapacity(capacityCeilGroupSize);

            byte* ptr = buffer + capacityCeilGroupSize;
            return (TKey*)AlignPointer(ptr, KeyGroupAlignment);
        }

        public static int Find<THasher>(byte* buffer, int capacityCeilGroupSize, TKey key, out byte h2, out bool exists)
            where THasher : unmanaged, IHasher64<TKey>
        {
            TKey* groupPtr = GetKeyGroupPtr(buffer, capacityCeilGroupSize);
            return Find<TKey, THasher>(sizeof(TKey), buffer, (byte*)groupPtr, capacityCeilGroupSize, key, true, out h2, out exists);
        }

        public static int FindEmpty<THasher>(byte* buffer, int capacityCeilGroupSize, TKey key, out byte h2)
            where THasher : unmanaged, IHasher64<TKey>
        {
            TKey* groupPtr = GetKeyGroupPtr(buffer, capacityCeilGroupSize);
            return Find<TKey, THasher>(sizeof(TKey), buffer, (byte*)groupPtr, capacityCeilGroupSize, key, false, out h2, out _);
        }

        public static void Insert(byte* buffer, int capacityCeilGroupSize, int index, TKey key, byte h2)
        {
            CheckIsAligned(buffer, ControlAlignment);
            CheckCapacity(capacityCeilGroupSize);
            CheckContainerIndexInRange(index, capacityCeilGroupSize);

            // Control
            buffer[index] = h2;

            // Key
            TKey* groupPtr = GetKeyGroupPtr(buffer, capacityCeilGroupSize);
            groupPtr[index] = key;
        }

        public static void CopyKeysTo(byte* buffer, int capacityCeilGroupSize, TKey* keyPtr)
        {
            Enumerator enumerator = new(buffer, capacityCeilGroupSize);
            int index = 0;

            while (enumerator.MoveNext())
            {
                keyPtr[index++] = enumerator.Current.Ref;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckKeyNotAlreadyAdded<THasher>(byte* buffer, int capacityCeilGroupSize, TKey key)
            where THasher : unmanaged, IHasher64<TKey>
        {
            _ = Find<THasher>(buffer, capacityCeilGroupSize, key, out _, out bool exists);

            if (Hint.Unlikely(exists))
            {
                ThrowKeyAlreadyAdded();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Enumerator : IEnumerator<Pointer<TKey>>
        {
            internal SwissTable.Enumerator enumerator;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(byte* buffer, int capacityCeilGroupSize)
            {
                enumerator = new SwissTable.Enumerator(buffer, (byte*)GetKeyGroupPtr(buffer, capacityCeilGroupSize), capacityCeilGroupSize);
            }

            public readonly Pointer<TKey> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    CheckContainerIndexInRange(enumerator.Index, enumerator.CapacityCeilGroupSize);
                    return (TKey*)enumerator.GroupPtr + enumerator.Index;
                }
            }

            readonly object IEnumerator.Current => throw new NotSupportedException();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                enumerator.Reset();
            }

            public readonly void Dispose()
            {
            }
        }
    }
}
