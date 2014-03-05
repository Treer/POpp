namespace UnitTests {

    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using popp;
    using System.Diagnostics;

    [TestClass]
    public class Tests_includes : TestBase {

        [TestMethod]
        public void Include_Test1_results() {
            File.Delete("../../testdata/includeTest_1_result.po");

            // This should fail and not create a file
            int result = Program.Main(new string[] { "../../testdata/includeTest_1.po", "../../testdata/includeTest_1_result.po" });
            Assert.AreEqual((int)ErrorLevel.FatalError_Internal, result, "includeTest_1.po should cause errors so grievious that popp aborts, but no FatalError_Internal happened");
            Assert.IsFalse(File.Exists("../../testdata/includeTest_1_result.po"), "includeTest_1_result.po should not have been written");
        }

        [TestMethod]
        public void Include_Test1_errors() {

            File.Delete("../../testdata/includeTest_1_errors.txt");


            int result = ShellExecute("popp.exe ../../testdata/includeTest_1.po ../../testdata/includeTest_1_result.po > ../../testdata/includeTest_1_errors.txt");

            Assert.AreEqual((int)ErrorLevel.FatalError_Internal, result, "includeTest_1.po should cause errors so grievious that popp aborts, but no FatalError_Internal happened");
            Assert.IsTrue(FileCompare("../../testdata/includeTest_1_errors.txt", "../../testdata/includeTest_1_expectedErrors.txt"), "../../testdata/includeTest_1_errors.txt does not match includeTest_1_expectedErrors.txt");
        }

        [TestMethod]
        public void Include_DirectoryArgs() {
            // test that --includedirectory works

        }
    }
}
