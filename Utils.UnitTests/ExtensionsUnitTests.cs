using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Utils.UnitTests
{
    [TestClass]
    public class ExtensionsUnitTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var x = "12/1/2015";
            var dt = x.TryCastToDateTime();

        }
    }
}
