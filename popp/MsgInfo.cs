namespace popp
{
    using System;

    /// <summary>
    /// Encapsulates a language entry from the .PO file,
    /// (the msgid and corresponding msgstr etc.)
    /// </summary>
    public class MsgInfo
    {
        public string msgid;
        public string msgstr;
        public int msgstr_linenumber;
        public int msgstr_lineCount;
        public string msgctxt;

        public bool msgstr_containsUnexpandedReferences;
        public bool msgstr_containsChanges;

        public bool IsValid()
        {
            return msgid != null && msgstr != null && msgstr_linenumber > 0;
        }

        /// <summary>
        /// The PO format allows entries to have the same msgid if their context is different,
        /// so use UniqueID instead of msgid if you need a key.
        /// </summary>
        public string UniqueID(bool caseSensitiveIDs)
        {
            string result = msgid;
            if (msgctxt != null) result += '-' + msgctxt;

            if (caseSensitiveIDs == false) result = result.ToLower();

            return result;
        }
    }
}
