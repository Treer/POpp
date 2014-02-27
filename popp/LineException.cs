using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace popp
{
    /// <summary>
    /// A fatal error condition was encountered on a specific line. Stop the program.
    /// </summary>
    class LineException : Exception
    {
        readonly LineInfo _lineInfo;

        public LineInfo LineInfo { get { return _lineInfo; } }

        public LineException(LineInfo lineInfo, string message)
            : base(message)
        {
            _lineInfo = lineInfo;
        }
    }
}
