using System;
using NUnit;
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
            Chain chain = new Chain();
            var obj = new ImmutableObject("Amazing json!");

            chain.Add(obj);
        }
    }
}
