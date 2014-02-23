namespace UnitTests
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using popp;


    [TestClass]
    public class PoppTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            int result = Program.Main(new string[] { "../../testdata/test.po", "../../testdata/test_result.po" });
            Assert.AreEqual(-5, result, "There are 5 unexpandable references in the test file, but popp returned: " + result);

            result = Program.Main(new string[] { "../../testdata/test_plurals.po", "../../testdata/test_plurals_result.po" });
            Assert.AreEqual(3, result, "Purals should cause non-fatal errors to be reported (errorLevel 3), but popp returned: " + result);

        }
    }
}
