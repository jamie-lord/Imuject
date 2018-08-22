using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using static Imuject.Database;

namespace ImujectTests
{
    [TestFixture]
    public class Class1
    {
        [TestCase]
        public void Test()
        {
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "chain.data")))
            {
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "chain.data"));
            }
            if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "journal")))
            {
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "journal"));
            }

            Chain chain = new Chain();
            for (int i = 0; i < 10000; i++)
            {
                var obj = new ImmutableObject($"The value {i}");
                chain.Add(obj);
                Debug.WriteLine($"Added object {i} {obj.Hash}");
            }

            Assert.IsTrue(chain.Validate());

            chain.Dispose();
        }
    }
}
