namespace UnitTests
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using popp;
    using System.Diagnostics;

    [TestClass]
    public class Tests_general : TestBase
    {
        [TestMethod]
        public void Basics()
        {
            // Test the basics
            int result = Program.Main(new string[] { "../../testdata/test.po", "../../testdata/test_result.po" });
            Assert.AreEqual(-5, result, "There are 5 unexpandable references in the test file, but popp returned: " + result);
            Assert.IsTrue(FileCompare("../../testdata/test_result.po", "../../testdata/test_expectedresult.po"), "test_result.po does not match test_expectedresult.po");
        }

        [TestMethod]
        public void CaseSensitivity()
        {
            // Test the case-sensitive option
            int result = Program.Main(new string[] {"--sensitive", "../../testdata/test.po", "../../testdata/test_result.po" });
            Assert.AreEqual(-6, result, "There are 6 unexpandable references in the test file (one due to mismatched case), but popp returned: " + result);
        }

        [TestMethod]
        public void PluralFormsIgnoredGracefully()
        {
            // Test that plural forms are ignored gracefully
            int result = Program.Main(new string[] { "../../testdata/test_plurals.po", "../../testdata/test_plurals_result.po" });
            Assert.AreEqual(3, result, "Purals should cause non-fatal errors to be reported (errorLevel 3), but popp returned: " + result);
            Assert.IsTrue(FileCompare("../../testdata/test_plurals_result.po", "../../testdata/test_plurals_expectedresult.po"), "test_plurals_result.po does not match test_plurals_expectedresult.po");
        }

        [TestMethod]
        public void ForceCRLF()
        {
            // Test -nCRLF
            int result = Program.Main(new string[] { "-nCRLF", "../../testdata/test.po", "../../testdata/test_forcecrlf_result.po" });
            Assert.AreEqual(-5, result, "There are 5 unexpandable references in the test file, but popp returned: " + result);
            Assert.IsTrue(FileCompare("../../testdata/test_forcecrlf_result.po", "../../testdata/test_crlf_expectedresult.po"), "test_crlf_result.po does not match test_crlf_expectedresult.po");
        }

        [TestMethod]
        public void DetectCRLF()
        {
            // Test that CRLFs are detected and used in the output by default
            int result = Program.Main(new string[] { "../../testdata/test_crlf.po", "../../testdata/test_detectcrlf_result.po" });
            Assert.AreEqual(-5, result, "There are 5 unexpandable references in the test file, but popp returned: " + result);
            Assert.IsTrue(FileCompare("../../testdata/test_detectcrlf_result.po", "../../testdata/test_crlf_expectedresult.po"), "test_detectcrlf_result.po does not match test_crlf_expectedresult.po");
        }

        [TestMethod]
        public void CountReferences()
        {
            // Test that the --count option correctly counts the number of references in the file
            int result = Program.Main(new string[] { "--count", "../../testdata/test.po" });
            Assert.AreEqual(15, result, "There are 15 references in the test file, but popp returned: " + result);

            // --count will fail on plural forms, because the program ignores them, but test the failure anyway.
            // The test_plurals.po file contains two more references than the test.po file
            result = Program.Main(new string[] { "--count", "../../testdata/test_plurals.po" });
            Assert.AreEqual(17, result, "There are 19 references in the test file, but popp will only see 17 of them, it returned: " + result);
        }

        [TestMethod]
        public void StdinAndStdout()
        {
            File.Delete("../../testdata/test_result.po");
            File.Delete("../../testdata/test_crlf_result.po");

            // newline-autodetection will fail on stdin, and default to CRLF
            int result = ShellExecute("popp < ../../testdata/test.po > ../../testdata/test_crlf_result.po");

            Assert.AreEqual(-5, result, "There are 5 unexpandable references in the test file, but popp returned: " + result);
            Assert.IsTrue(FileCompare("../../testdata/test_crlf_result.po", "../../testdata/test_crlf_expectedresult.po"), "test_crlf_result.po does not match test_crlf_expectedresult.po");

            /* This test fails on the unicode chars because the console has limited 
             * characters so the encoding conversion is lossy - not sure what to do 
             * about it, warn people away from pipes I guess
            // newline-autodetection will not fail on a file
            strCmdText = "popp ../../testdata/test.po - > ../../testdata/test_result.po";
            shellCommand = Process.Start("CMD.exe", "/C " + strCmdText);
            shellCommand.WaitForExit();
            Assert.IsTrue(FileCompare("../../testdata/test_result.po", "../../testdata/test_expectedresult.po"), "test_result.po does not match test_expectedresult.po");
            */
        }
    }
}
