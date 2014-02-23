namespace popp
{
    public class LineInfo
    {
        public LineType Type;
        public int LineNumber;
        public string Line;

        public LineInfo(LineType aType, int aLineNumber, string aLine)
        {
            Type = aType;
            LineNumber = aLineNumber;
            Line = aLine;
        }
    }
}
