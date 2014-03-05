namespace popp
{
    /// <summary>
    /// An immutable set of information about a line and the source
    /// file it came from.
    /// </summary>
    public class LineInfo
    {
        public LineType Type          { get; private set; }
        public int      LineNumber    { get; private set; }
        public string   Line          { get; private set; }

        /// <summary>
        /// An index into the list of Included files, to provide info on the source of the line
        /// If this is less than zero then the line came from the main file
        /// </summary>
        public int      IncludeFileID { get; private set; }

        public LineInfo(LineType aType, int aLineNumber, int aIncludeFileID, string aLine)
        {
            Type          = aType;
            LineNumber    = aLineNumber;
            Line          = aLine;
            IncludeFileID = aIncludeFileID;
        }

        public LineInfo(LineType aType, int aLineNumber, string aLine) : this(aType, aLineNumber, -1, aLine) { }
    }
}
