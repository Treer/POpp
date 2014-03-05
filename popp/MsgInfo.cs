namespace popp
{
    using System;

    /// <summary>
    /// Encapsulates a language entry from the .PO file,
    /// (the msgid and corresponding msgstr etc.)
    /// 
    /// NOT immutable.
    /// </summary>
    public class MsgInfo
    {
        public string   Msgctxt_Value                       { get; set; }
        public string   Msgid_Value                         { get; set; }

        public string   Msgstr_Value                        { get; set; }
        public int      Msgstr_LineCount                    { get; set; }
        public LineInfo Msgstr_Info                         { get; set; }
        public bool     Msgstr_ContainsUnexpandedReferences { get; set; }
        public bool     Msgstr_ContainsChanges              { get; set; }


        public bool IsValid()
        {
            return Msgid_Value != null && Msgstr_Value != null && Msgstr_Info.LineNumber > 0;
        }

        /// <summary>
        /// The PO format allows entries to have the same msgid if their context is different,
        /// so use UniqueID instead of msgid if you need a key.
        /// </summary>
        public string UniqueID(bool caseSensitiveIDs)
        {
            string result = Msgid_Value;
            if (Msgctxt_Value != null) result += '-' + Msgctxt_Value;

            if (caseSensitiveIDs == false) result = result.ToLower();

            return result;
        }
    }
}
