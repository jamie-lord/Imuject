using Imuject;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImujectTests
{
    [TestFixture]
    public class LinkedIndexTests
    {
        [TestCase]
        public void Add()
        {
            LinkedIndex<int> linkedIndex = new LinkedIndex<int>(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Guid.NewGuid().ToString());

            linkedIndex.Add(0, 999);

            List<int> result = linkedIndex.Get(0).ToList();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(999, result[0]);
        }
    }
}
