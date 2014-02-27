namespace popp
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Provides some file related extension methods for List<>
    /// </summary>
    public static class FileListExtensions
    {
        static bool? _fileSystemIsCaseSensitive = null;

        /// <summary>
        /// Returns the index of filename in list, or -1 if list does not contain filename.
        /// If the file system is case-sensitive, then a case-sensitive search will be performed
        /// </summary>
        public static int IndexOfFileInList(this List<string> list, string filename)
        {
            int result = -1;

            // No special search optimization is needed, the list will normally be 2 or fewer entries
            int index = 0;
            foreach (string item in list) {

                bool match = FileSystemIsCaseSensitive ?
                    (filename == item) :
                    (filename.ToLowerInvariant() == item.ToLowerInvariant());

                if (match) {
                    result = index;
                    break;
                }
                index++;
            }

            return result;
        }



        static bool FileSystemIsCaseSensitive
        {
            get
            {
                if (_fileSystemIsCaseSensitive == null) {

                    _fileSystemIsCaseSensitive = false;
                    try {
                        string filebase = Path.GetTempPath() + Guid.NewGuid().ToString();
                        string file1 = filebase + "a";
                        string file2 = filebase + "A";

                        File.CreateText(file1).Close();
                        _fileSystemIsCaseSensitive = !File.Exists(file2);
                        File.Delete(file1);
                    } catch {
                        // Don't care, I tried.
                    }
                }
                return _fileSystemIsCaseSensitive.Value;
            }
        }

    }
}
