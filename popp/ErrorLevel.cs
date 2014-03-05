namespace popp
{
    /// <summary>
    /// errorLevel Return values for Main()
    /// </summary>
    public enum ErrorLevel : int
    {
        Success                = 0,
        FatalError_InvalidArgs = 1,
        FatalError_Internal    = 2,
        NonFatalError          = 3
    }
}
