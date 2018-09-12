using Imuject;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImujectTests
{
    [TestFixture]
    public class LinkedIndexTests
    {
        private string _name = Guid.NewGuid().ToString();

        private LinkedIndex<int> _linkedIndex;

        [SetUp]
        public void SetUp()
        {
            _linkedIndex = new LinkedIndex<int>(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), _name);
        }

        [TestCase]
        public void Add()
        {
            _linkedIndex.Add(0, 999);

            List<int> result = _linkedIndex.Get(0).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(999, result[0]);

            _linkedIndex.Add(1, 998);

            result = _linkedIndex.Get(1).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(998, result[0]);

            _linkedIndex.Add(0, 997);

            result = _linkedIndex.Get(0).ToList();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(997, result[0]);
            Assert.AreEqual(999, result[1]);
        }

        [TearDown]
        public void TearDown()
        {
            _linkedIndex.Dispose();
            File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{_name}.loi"));
            File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), $"{_name}.oi"));
        }
    }
}
