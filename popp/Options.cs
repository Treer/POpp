namespace popp
{
    public enum NewLineOptions
    {
        SameAsSource, // Use the same newline characters the source file uses
        CRLF,         // Force newlines to be CRLF
        LF            // Force newlines to be LF
    }


    public struct Options
    {
        public NewLineOptions NewlinePreference;
        public bool Quiet;
        public bool CountReferences;
        public bool CaseSensitiveIDs;
    }
}
