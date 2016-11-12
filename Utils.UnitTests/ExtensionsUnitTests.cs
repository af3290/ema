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
            //var xxxx = new Class1();
            //xxxx.GetXX();

            var x = "12/1/2015";
            var dt = x.TryCastToDateTime();

            //not needed...
            //x = "20010130";
            //var dt2 = x.TryCastToDateTime();
        }
    }
}
