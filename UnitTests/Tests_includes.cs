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


            int result = ShellExecute("popp.exe ../../testdata/includeTest_1.po ../../testdata/includeTest_1_result.po 2> ../../testdata/includeTest_1_errors.txt");

            Assert.AreEqual((int)ErrorLevel.FatalError_Internal, result, "includeTest_1.po should cause errors so grievious that popp aborts, but no FatalError_Internal happened");
            Assert.IsTrue(FileCompare("../../testdata/includeTest_1_errors.txt", "../../testdata/includeTest_1_expectedErrors.txt"), "../../testdata/includeTest_1_errors.txt does not match includeTest_1_expectedErrors.txt");
        }

        [TestMethod]
        public void Include_DirectoryArgs() {
            // test that --includedirectory works

            // Without providing an include directory, this should fail
            int result = Program.Main(new string[] { "../../testdata/includeTest_directories.po", "../../testdata/includeTest_directories_result.po" });
            Assert.AreEqual((int)ErrorLevel.FatalError_Internal, result, "includeTest_directories.po should fail to process unless the includedirectory is specified");

            // this time it should work
            result = Program.Main(new string[] { "--includeDirectory", "../../testdata/includePathTest", "../../testdata/includeTest_directories.po", "../../testdata/includeTest_directories_result.po" });
            Assert.AreEqual((int)ErrorLevel.Success, result, "The file should have processed, but didn't");
        }

        [TestMethod]
        public void Include_MissingFiles() {

            File.Delete("../../testdata/includeTest_missingresult.txt");

            // This should fail and not create a file
            int result = Program.Main(new string[] { "../../testdata/includeTest_missing.po", "../../testdata/includeTest_missingresult.po" });
            Assert.AreEqual((int)ErrorLevel.FatalError_Internal, result, "includeTest_missing.po should references a non-existant file, so popp should abort, but no FatalError_Internal happened");
            Assert.IsFalse(File.Exists("../../testdata/includeTest_missingresult.po"), "includeTest_missingresult.po should not have been written");
        }

        [TestMethod]
        public void Include_WorkingExample() {
            // Instead of testing edge cases and failure results, lets actually test everything going right

            File.Delete("../../testdata/includeTest_3_result.txt");

            int result = Program.Main(new string[] { "../../testdata/includeTest_3.po", "../../testdata/includeTest_3_result.po" });
            Assert.AreEqual((int)ErrorLevel.Success, result, "includeTest_3.po should work, so popp should return success, but didn't.");
            Assert.IsTrue(FileCompare("../../testdata/includeTest_3_result.po", "../../testdata/includeTest_3_expectedResult.po"), "includeTest_3_result.po does not match includeTest_3_expectedResult.po");
        }



    }
}
