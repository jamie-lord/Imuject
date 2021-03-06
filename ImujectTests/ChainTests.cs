﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Bogus;
using Imuject;
using NUnit.Framework;

namespace ImujectTests
{
    [TestFixture]
    public class ChainTests
    {
        private Faker _faker;

        private Chain _chain;

        private string _dbName = Guid.NewGuid().ToString();

        [SetUp]
        public void Setup()
        {
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{_dbName}.data")))
            {
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{_dbName}.data"));
            }
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{_dbName}.index")))
            {
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{_dbName}.index"));
            }
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{_dbName}.version.index")))
            {
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{_dbName}.version.index"));
            }
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{_dbName}.latestVersion.loi")))
            {
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{_dbName}.latestVersion.loi"));
            }
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{_dbName}.latestVersion.oi")))
            {
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{_dbName}.latestVersion.oi"));
            }

            _faker = new Faker();
            _chain = new Chain(_dbName);
        }

        [TestCase]
        public void Insert()
        {
            for (int i = 0; i < 1000; i++)
            {
                var obj = new ImmutableObject() { Json = _faker.Random.String() };
                _chain.Insert(obj);
                Debug.WriteLine($"Added object {i} {obj.Hash}");
            }
        }

        [TestCase]
        public void ModifyExisting()
        {
            for (int i = 0; i < 1000; i++)
            {
                var obj = new ImmutableObject() { Json = _faker.Random.String() };
                int originalId = _chain.Insert(obj);
                int originalIndex = obj.Index;
                Assert.AreEqual(0, obj.Version);

                obj.Json = _faker.Random.String();
                int newId = _chain.Insert(obj);
                Assert.AreEqual(originalId, newId);
                Assert.AreEqual(1, obj.Version);
                Assert.AreNotEqual(obj.Index, originalIndex);
            }

            Assert.AreEqual(1000, _chain.UniqueObjectCount);
        }

        [TestCase]
        public void LatestObjectVersion()
        {
            var obj = new ImmutableObject() { Json = _faker.Random.String() };
            int id = _chain.Insert(obj);
            Assert.AreEqual(1, id);
            Assert.AreEqual(0, obj.Version);
            obj.Json = _faker.Random.String();
            int id1 = _chain.Insert(obj);
            Assert.AreEqual(1, id1);
            Assert.AreEqual(1, obj.Version);
            obj.Json = _faker.Random.String();
            int id2 = _chain.Insert(obj);
            Assert.AreEqual(1, id2);
            Assert.AreEqual(2, obj.Version);
            var result = _chain.LatestVersion(1);
            Assert.AreEqual(1, result.Id);
            Assert.AreEqual(2, result.Version);
            Assert.AreEqual(3, result.Index);
        }

        [TestCase]
        public void PreviousObjectVersions()
        {
            var obj = new ImmutableObject() { Json = _faker.Random.String() };
            int id = _chain.Insert(obj);
            Assert.AreEqual(1, id);
            Assert.AreEqual(0, obj.Version);
            obj.Json = _faker.Random.String();
            _chain.Insert(new ImmutableObject
            {
                Json = _faker.Random.String()
            });
            int id1 = _chain.Insert(obj);
            Assert.AreEqual(1, id1);
            Assert.AreEqual(1, obj.Version);
            _chain.Insert(new ImmutableObject
            {
                Json = _faker.Random.String()
            });
            obj.Json = _faker.Random.String();
            int id2 = _chain.Insert(obj);
            Assert.AreEqual(1, id2);
            Assert.AreEqual(2, obj.Version);
            _chain.Insert(new ImmutableObject
            {
                Json = _faker.Random.String()
            });
            var allVersions = _chain.PreviouVersions(id2).ToList();
            Assert.AreEqual(3, allVersions.Count);
            Assert.AreEqual(1, allVersions[0].Id);
            Assert.AreEqual(1, allVersions[1].Id);
            Assert.AreEqual(1, allVersions[2].Id);
            Assert.AreEqual(2, allVersions[0].Version);
            Assert.AreEqual(1, allVersions[1].Version);
            Assert.AreEqual(0, allVersions[2].Version);
            Assert.AreEqual(4, _chain.UniqueObjectCount);
        }

        [TestCase]
        public void UniqueObjectCount()
        {
            var obj = new ImmutableObject() { Json = _faker.Random.String() };
            _chain.Insert(obj);
            Assert.AreEqual(1, _chain.UniqueObjectCount);
            obj.Json = _faker.Random.String();
            Assert.AreEqual(1, _chain.UniqueObjectCount);
            var obj2 = new ImmutableObject() { Json = _faker.Random.String() };
            _chain.Insert(obj2);
            Assert.AreEqual(2, _chain.UniqueObjectCount);
            obj2.Json = _faker.Random.String();
            _chain.Insert(obj2);
            Assert.AreEqual(2, _chain.UniqueObjectCount);
        }

        [TearDown]
        public void TearDown()
        {
            Assert.IsTrue(_chain.Validate());
            _chain.Dispose();
            File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{_dbName}.data"));
            File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{_dbName}.index"));
            File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{_dbName}.version.index"));
            File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{_dbName}.latestVersion.loi"));
            File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{_dbName}.latestVersion.oi"));
        }
    }
}
