namespace popp
{
    using System;

    /// <summary>
    /// Immutable information about a "{id:my msgid}" reference contained in a string
    /// </summary>
    public class ReferenceInfo
    {        
        /// <summary>
        /// 0-based index of the start of the reference
        /// </summary>
        public int StartIndex { get; private set; }

        /// <summary>
        /// length of the reference
        /// </summary>
        public int Length { get; private set; }

        /// <summary>
        /// WARNING - do not use this for comparisons, use UniqueID instead.
        /// msgid of the string that should replace the reference, optionally with a hyphen followed by
        /// the msgctxt
        /// </summary>
        public string Msgid { get; private set; }

        /// <summary>
        /// The PO format allows entries to have the same msgid if their context is different,
        /// so use UniqueID instead of msgid if you need a key.
        /// </summary>
        /// <returns></returns>
        public string UniqueID(bool CaseSensitiveIDs)
        {
            return CaseSensitiveIDs ? Msgid : Msgid.ToLower();
        }


        public ReferenceInfo(int aStartIndex, int aLength, string aMsgid)
        {
            StartIndex = aStartIndex;
            Length = aLength;
            Msgid = aMsgid;
        }
    }
}
