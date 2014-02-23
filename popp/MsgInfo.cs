namespace popp
{
    using System;

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
        /// <returns></returns>
        public string UniqueID {
            get
            {
                if (msgctxt == null) {
                    return msgid;
                } else {
                    return msgid + "-" + msgctxt;
                }
            }
        }
    }
}
