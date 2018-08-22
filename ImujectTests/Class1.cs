using System;
using System.Diagnostics;
using System.IO;
using Bogus;
using NUnit.Framework;
using static Imuject.Database;

namespace ImujectTests
{
    [TestFixture]
    public class InsertTests
    {
        private Faker _faker;

        private Chain _chain;

        [SetUp]
        public void Setup()
        {
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "chain.data")))
            {
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "chain.data"));
            }
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "journal")))
            {
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "journal"));
            }

            _faker = new Faker();
        }

        [TestCase]
        public void InsertAllNew()
        {
            _chain = new Chain();
            for (int i = 0; i < 100; i++)
            {
                var obj = new ImmutableObject(_faker.Random.String());
                _chain.Insert(obj);
                Debug.WriteLine($"Added object {i} {obj.Hash}");
            }
        }

        [TestCase]
        public void InsertAndModify()
        {
            _chain = new Chain();
            for (int i = 0; i < 100; i++)
            {
                var obj = new ImmutableObject(_faker.Random.String());
                int originalId = _chain.Insert(obj);
                int originalIndex = obj.Index;
                Assert.AreEqual(0, obj.Version);
                Debug.WriteLine($"Added object {i} {obj.Hash}");

                obj.Json = _faker.Random.String();
                int newId = _chain.Insert(obj);
                Assert.AreEqual(originalId, newId);
                Assert.AreEqual(1, obj.Version);
                Assert.AreNotEqual(obj.Index, originalIndex);
            }

            Assert.AreEqual(101, _chain.UniqueObjectCount);
        }

        [TearDown]
        public void TearDown()
        {
            Assert.IsTrue(_chain.Validate());

            _chain.Dispose();
        }
    }
}
