using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using IntIntIdentityTable = EvilOctane.Collections.LowLevel.Unsafe.UnsafeSwissTable<int, int, EvilOctane.Collections.Tests.IdentityHasher>;
using IntIntXXH3Table = EvilOctane.Collections.LowLevel.Unsafe.UnsafeSwissTable<int, int, EvilOctane.Collections.XXH3PodHasher<int>>;

namespace EvilOctane.Collections.Tests
{
    public class UnsafeSwissTableTests
    {
        private const int elementCount = 100000;

        [Test]
        public void TestLifetime()
        {
            IntIntXXH3Table table = new(Allocator.TempJob);
            Assert.IsTrue(table.IsCreated);

            table.Dispose();
            Assert.IsFalse(table.IsCreated);
        }

        [Test]
        public void TestJobLifetime()
        {
            IntIntXXH3Table table = new(Allocator.TempJob);
            Assert.IsTrue(table.IsCreated);

            table.Dispose(new JobHandle()).Complete();
            Assert.IsFalse(table.IsCreated);
        }

        [Test]
        public void TestAdd()
        {
            using IntIntXXH3Table table = new(Allocator.TempJob);

            for (int i = 0; i != elementCount; ++i)
            {
                int value = int.MaxValue - i;
                table.GetOrAdd(i, out bool added) = value;
                Assert.IsTrue(added);
            }

            Assert.AreEqual(table.Count, elementCount);

            for (int i = 0; i != elementCount; ++i)
            {
                int value = table.TryGet(i, out bool exists);
                Assert.IsTrue(exists);
                Assert.AreEqual(int.MaxValue - i, value);
            }
        }

        [Test]
        public void TestRemove()
        {
            using IntIntXXH3Table table = new(Allocator.TempJob);

            for (int i = 0; i != elementCount; ++i)
            {
                int value = int.MaxValue - i;
                table.GetOrAdd(i, out bool added) = value;
                Assert.IsTrue(added);
            }

            Assert.AreEqual(table.Count, elementCount);

            for (int i = 0; i != elementCount; ++i)
            {
                bool removed = table.Remove(i);
                Assert.IsTrue(removed);
            }

            Assert.IsTrue(table.IsEmpty);
        }

        [Test]
        public void TestAddRemoveBadHash()
        {
            using IntIntIdentityTable table = new(Allocator.TempJob);

            for (int i = 0; i != elementCount; ++i)
            {
                int value = int.MaxValue - i;
                table.GetOrAdd(i, out bool added) = value;
                Assert.IsTrue(added);
            }

            Assert.AreEqual(table.Count, elementCount);

            for (int i = 0; i != elementCount; ++i)
            {
                bool removed = table.Remove(i);
                Assert.IsTrue(removed);
            }

            Assert.IsTrue(table.IsEmpty);
        }
    }

    public struct IdentityHasher : IHasher64<int>
    {
        public readonly ulong CalculateHash(int value)
        {
            return (ulong)value;
        }
    }
}
