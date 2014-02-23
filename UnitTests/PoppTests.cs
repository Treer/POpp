namespace UnitTests
{
    using System;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using popp;


    [TestClass]
    public class PoppTests
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

        // This method accepts two strings the represent two files to 
        // compare. A return value of 0 indicates that the contents of the files
        // are the same. A return value of any other value indicates that the 
        // files are not the same.
        bool FileCompare(string file1, string file2)
        {
            int file1byte;
            int file2byte;
            FileStream fs1;
            FileStream fs2;

            // Determine if the same file was referenced two times.
            if (file1 == file2) {
                // Return true to indicate that the files are the same.
                return true;
            }

            // Open the two files.
            fs1 = new FileStream(file1, FileMode.Open, FileAccess.Read);
            fs2 = new FileStream(file2, FileMode.Open, FileAccess.Read);

            // Check the file sizes. If they are not the same, the files 
            // are not the same.
            if (fs1.Length != fs2.Length) {
                // Close the file
                fs1.Close();
                fs2.Close();

                // Return false to indicate files are different
                return false;
            }

            // Read and compare a byte from each file until either a
            // non-matching set of bytes is found or until the end of
            // file1 is reached.
            do {
                // Read one byte from each file.
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            // Close the files.
            fs1.Close();
            fs2.Close();

            // Return the success of the comparison. "file1byte" is 
            // equal to "file2byte" at this point only if the files are 
            // the same.
            return ((file1byte - file2byte) == 0);
        }
    }
}
