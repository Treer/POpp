namespace popp
{
    public class LineInfo
    {
        public LineType Type;
        public int LineNumber;
        public string Line;

        /// <summary>
        /// An index into the list of Included files, to provide info on the source of the line
        /// If this is less than zero then the line came from the main file
        /// </summary>
        public int IncludeFileID;

        public LineInfo(LineType aType, int aLineNumber, int aIncludeFileID, string aLine)
        {
            Type = aType;
            LineNumber = aLineNumber;
            Line = aLine;
            IncludeFileID = aIncludeFileID;
        }

        public LineInfo(LineType aType, int aLineNumber, string aLine)
        {
            Type = aType;
            LineNumber = aLineNumber;
            Line = aLine;
            IncludeFileID = -1;
        }
    }
}
