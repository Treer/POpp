namespace popp
{
    using System;

    /// <summary>
    /// Information about a "{id:my msgid}" reference contained in a string
    /// </summary>
    public class ReferenceInfo
    {        
        /// <summary>
        /// 0-based index of the start of the reference
        /// </summary>
        public int StartIndex;

        /// <summary>
        /// length of the reference
        /// </summary>
        public int Length;

        /// <summary>
        /// msgid of the string that should replace the reference
        /// </summary>
        public string Msgid;

        public ReferenceInfo(int aStartIndex, int aLength, string aMsgid)
        {
            StartIndex = aStartIndex;
            Length = aLength;
            Msgid = aMsgid;
        }
    }
}
