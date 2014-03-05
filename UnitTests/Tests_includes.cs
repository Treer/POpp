namespace UnitTests {

    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using popp;
    using System.Diagnostics;

    [TestClass]
    public class Tests_includes : TestBase {

        [TestMethod]
        public void Include() {
            // Test the basics
            int result = Program.Main(new string[] { "../../testdata/includeTest_1.po", "../../testdata/includeTest_1_result.po" });
            //Assert.AreEqual(-5, result, "There are 5 unexpandable references in the test file, but popp returned: " + result);
            //Assert.IsTrue(FileCompare("../../testdata/test_result.po", "../../testdata/test_expectedresult.po"), "test_result.po does not match test_expectedresult.po");
        }

        [TestMethod]
        public void Include_TestErrors() {

            File.Delete("../../testdata/includeTest_1_errors.txt");


            int result = ShellExecute("popp.exe ../../testdata/includeTest_1.po ../../testdata/includeTest_1_result.po > ../../testdata/includeTest_1_errors.txt");

            Assert.IsTrue(FileCompare("../../testdata/includeTest_1_errors.txt", "../../testdata/includeTest_1_expectedErrors.txt"), "../../testdata/includeTest_1_errors.txt does not match includeTest_1_expectedErrors.txt");
        }
    }
}
