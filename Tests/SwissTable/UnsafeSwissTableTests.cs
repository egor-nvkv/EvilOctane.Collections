using AOT;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using IntIntIdentityTable = EvilOctane.Collections.LowLevel.Unsafe.UnsafeSwissTable<int, int, EvilOctane.Collections.Tests.IdentityHasher>;
using IntIntXXH3Table = EvilOctane.Collections.LowLevel.Unsafe.UnsafeSwissTable<int, int, EvilOctane.Collections.XXH3PodHasher<int>>;

namespace EvilOctane.Collections.Tests
{
    [BurstCompile(CompileSynchronously = true)]
    public class UnsafeSwissTableTests
    {
        private delegate void XXH3Delegate(ref IntIntXXH3Table arg0);
        private delegate void TryGetDelegate(ref IntIntXXH3Table arg0, int arg1, out int arg2, out int arg3);
        private delegate void GetOrAddXXH3Delegate(ref IntIntXXH3Table arg0, int arg1, int arg2, out int arg3);
        private delegate void RemoveXXH3Delegate(ref IntIntXXH3Table arg0, int arg1, out int arg2);

        private delegate void GetOrAddIdentityDelegate(ref IntIntIdentityTable arg0, int arg1, int arg2, out int arg3);
        private delegate void RemoveIdentityDelegate(ref IntIntIdentityTable arg0, int arg1, out int arg2);

        private const int elementCount = 100000;

        // XXH3

        [BurstCompile]
        [MonoPInvokeCallback(typeof(XXH3Delegate))]
        private static void Create_Burst(ref IntIntXXH3Table table)
        {
            table = new(elementCount / 4, Allocator.TempJob);
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(XXH3Delegate))]
        private static void Dispose_Burst(ref IntIntXXH3Table table)
        {
            table.Dispose();
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(XXH3Delegate))]
        private static void JobDispose_Burst(ref IntIntXXH3Table table)
        {
            table.Dispose(new JobHandle()).Complete();
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(TryGetDelegate))]
        private static void TryGet_Burst(ref IntIntXXH3Table table, int key, out int value, out int exists)
        {
            Pointer<int> valuePtr = table.TryGet(key, out bool existsBool);
            value = existsBool ? valuePtr.AsRef : default;
            exists = existsBool ? 1 : 0;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GetOrAddXXH3Delegate))]
        private static void GetOrAdd_Burst(ref IntIntXXH3Table table, int key, int value, out int added)
        {
            table.GetOrAdd(key, out bool addedBool).AsRef = value;
            added = addedBool ? 1 : 0;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(RemoveXXH3Delegate))]
        private static void Remove_Burst(ref IntIntXXH3Table table, int key, out int removed)
        {
            removed = table.Remove(key) ? 1 : 0;
        }

        // Identity

        [BurstCompile]
        [MonoPInvokeCallback(typeof(GetOrAddIdentityDelegate))]
        private static void GetOrAdd_Burst(ref IntIntIdentityTable table, int key, int value, out int added)
        {
            table.GetOrAdd(key, out bool addedBool).AsRef = value;
            added = addedBool ? 1 : 0;
        }

        [BurstCompile]
        [MonoPInvokeCallback(typeof(RemoveIdentityDelegate))]
        private static void Remove_Burst(ref IntIntIdentityTable table, int key, out int removed)
        {
            removed = table.Remove(key) ? 1 : 0;
        }

        [Test]
        public void TestLifetime()
        {
            IntIntXXH3Table table = default;
            Create_Burst(ref table);

            Assert.IsTrue(table.IsCreated);

            Dispose_Burst(ref table);
            Assert.IsFalse(table.IsCreated);
        }

        [Test]
        public void TestDefaultInitialized()
        {
            IntIntXXH3Table table = new();

            Assert.IsTrue(table.IsEmpty);
            Assert.IsTrue(table.IsFull);

            SwissTable<int, int>.Enumerator enumerator = table.GetEnumerator();
            Assert.IsFalse(enumerator.MoveNext());

            Assert.DoesNotThrow(() => table.Dispose());
        }

        [Test]
        public void TestJobLifetime()
        {
            IntIntXXH3Table table = default;
            Create_Burst(ref table);

            Assert.IsTrue(table.IsCreated);

            JobDispose_Burst(ref table);
            Assert.IsFalse(table.IsCreated);
        }

        [Test]
        public void TestAdd()
        {
            IntIntXXH3Table table = default;
            Create_Burst(ref table);

            for (int i = 0; i != elementCount; ++i)
            {
                int value = int.MaxValue - i;
                GetOrAdd_Burst(ref table, i, value, out int added);
                Assert.AreEqual(1, added);
            }

            Assert.AreEqual(table.Count, elementCount);

            for (int i = 0; i != elementCount; ++i)
            {
                TryGet_Burst(ref table, i, out int value, out int exists);
                Assert.AreEqual(1, exists);
                Assert.AreEqual(int.MaxValue - i, value);
            }

            Dispose_Burst(ref table);
        }

        [Test]
        public void TestRemove()
        {
            IntIntXXH3Table table = default;
            Create_Burst(ref table);

            for (int i = 0; i != elementCount; ++i)
            {
                int value = int.MaxValue - i;
                GetOrAdd_Burst(ref table, i, value, out _);
            }

            for (int i = 0; i != elementCount; ++i)
            {
                Remove_Burst(ref table, i, out int removed);
                Assert.AreEqual(1, removed);
            }

            Assert.IsTrue(table.IsEmpty);
            Dispose_Burst(ref table);

            Debug.Log($"Add/Remove {elementCount} items (good hasher): {table.OccupiedCount} ({(float)table.OccupiedCount / table.Capacity * 100}%) tombstones.");
        }

        [Test]
        public void TestAddRemoveBadHash()
        {
            IntIntIdentityTable table = new(elementCount, Allocator.TempJob);

            for (int i = 0; i != elementCount; ++i)
            {
                int value = int.MaxValue - i;
                GetOrAdd_Burst(ref table, i, value, out _);
            }

            for (int i = 0; i != elementCount; ++i)
            {
                Remove_Burst(ref table, i, out int removed);
                Assert.AreEqual(1, removed);
            }

            Assert.IsTrue(table.IsEmpty);
            table.Dispose();
        }
    }

    public struct IdentityHasher : IHasher64<int>
    {
        public readonly ulong CalculateHash(in int value)
        {
            return (ulong)value;
        }
    }
}
