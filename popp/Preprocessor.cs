namespace popp
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    class Preprocessor
    {
        internal const string cNewline_LF   = "\n";
        internal const string cNewline_CRLF = "\r\n";
        internal const string cNewline_Default = cNewline_CRLF;

        internal const string cReferenceSignature_Start = "{id:";
        internal const string cReferenceSignature_End = "}";

        readonly Options _options;
        int errorLevel = 0;

        /// <summary>
        /// Gets the number of references contained in the input file, regardless
        /// of whether the references can be successfully expanded.
        /// </summary>
        /// <returns>the number of references, or -1 if error</returns>
        public int CountReferences(Stream input)
        {
            int result = 0;

            try {
                StreamReader inputReader = new StreamReader(input);

                // Build a list of information about each line
                IEnumerable<LineInfo> lines = BuildListOfLines(inputReader);

                // Build a dictionary of translation items
                Dictionary<string/*MsgInfo.UniqueID*/, MsgInfo> keyValues = BuildMsgInfoDictionary(lines);

                // Count the references in the list of msgstrs
                foreach (MsgInfo msgInfo in keyValues.Values) {

                    ReferenceInfo reference;
                    int nextRefSearchPos = 0;
                    while ((reference = GetFirstReference(msgInfo.msgstr, nextRefSearchPos)) != null) {
                        result++;
                        nextRefSearchPos = reference.StartIndex + reference.Length;
                    }
                }

                DisplayInfo("");
                DisplayInfo(result + " references were found.");
                DisplayInfo("");

            } catch (Exception ex) {

                ErrorEncountered("Unexpected internal error - " + ex);
                result = -1;
            }

            return result;
        }


        /// <summary>
        /// Run the preprocessor
        /// </summary>
        /// <returns>errorLevel, 0 for success</returns>
        public int Process(Stream input, Stream output) 
        {
            int unexpandableReferenceCount = 0;
            errorLevel = 0;

            try {
                StreamReader inputReader = new StreamReader(input);

                // Unicode BOM causes syntax errors in the gettext utils
                Encoding utf8WithoutBom = new UTF8Encoding(false);
                TextWriter outputWriter = new StreamWriter(output, utf8WithoutBom);

                // determine which newline character to use.
                string newline = cNewline_Default;
                switch (_options.NewlinePreference) {
                    case NewLineOptions.SameAsSource:
                        // lookahead in inputReader to see whether the line is broken with LF or CR
                        if (input.CanSeek) {
                            long startPosition = input.Position;
                            int peekedChar;
                            while ((peekedChar = input.ReadByte()) != -1) {
                                if (peekedChar == (int)('\n')) {
                                    // We encountered a LF
                                    newline = cNewline_LF;
                                    break;
                                } else if (peekedChar == (int)('\r')) {
                                    // We encountered a CR
                                    newline = cNewline_CRLF;
                                    break;
                                }
                            }
                            input.Seek(startPosition, SeekOrigin.Begin);
                        }
                        break;
                    case NewLineOptions.LF:
                        newline = cNewline_LF;
                        break;
                    case NewLineOptions.CRLF:
                        newline = cNewline_CRLF;
                        break;
                }
                outputWriter.NewLine = newline;


                // Build a list of information about each line
                IEnumerable<LineInfo> lines = BuildListOfLines(inputReader);

                // Todo:
                // This is where we'll apply the $if $else $include etc directives
                // lines = ApplyDirectives(lines);

                // Build a dictionary of translation items
                Dictionary<string/*MsgInfo.UniqueID*/, MsgInfo> keyValues = BuildMsgInfoDictionary(lines);

                int expandedReferenceCount;
                unexpandableReferenceCount = ExpandMsgstrs(keyValues, out expandedReferenceCount);

                // Write out the file with any adjusted msgstr entries...

                // build a second dictionary of the msgstrs we are changing, indexed by the linenumber
                Dictionary<int/*msgstr_lineNumber*/, MsgInfo> alteredMsgstrLines = new Dictionary<int, MsgInfo>();
                foreach (MsgInfo msgInfo in keyValues.Values) {
                    if (msgInfo.msgstr_containsChanges) alteredMsgstrLines.Add(msgInfo.msgstr_linenumber, msgInfo);
                }

                // Write the lines to the output file, with any adjusted msgstr entries
                int linesToSkip = 0;
                foreach (LineInfo lineInfo in lines) {

                    if (linesToSkip > 0) {
                        // skip over the multiline source strings if we have already written out our own version
                        linesToSkip--;

                    } else {

                        bool replaceLineWithAdjustedMsgstr = false;
                        MsgInfo msgInfo = null;
                        if (alteredMsgstrLines.TryGetValue(lineInfo.LineNumber, out msgInfo)) {
                            // We have our own version of this line
                            if (msgInfo.msgstr_containsChanges) replaceLineWithAdjustedMsgstr = true;
                        }

                        // Only change the lines in the file that we need to, so all whitespace and weird user 
                        // formatting etc will be preserved.
                        if (replaceLineWithAdjustedMsgstr) {
                            outputWriter.WriteLine("msgstr \"" + msgInfo.msgstr + "\"");
                            linesToSkip = msgInfo.msgstr_lineCount;

                        } else {
                            outputWriter.WriteLine(lineInfo.Line);
                        }
                    }
                }
                outputWriter.Close();

                DisplayInfo("Done - Expanded " + expandedReferenceCount + " references");
                DisplayInfo("       Failed to expand " + unexpandableReferenceCount + " references");

            } catch (Exception ex) {

                ErrorEncountered("Unexpected internal error - " + ex);
                errorLevel = (int)ErrorLevel.FatalError_Internal;
            }

            // If the preprocessing was successful, then return the number of references
            // we found which could not be expanded, as a negative number (to prevent 
            // confusion with the error codes)
            if (errorLevel == 0) errorLevel = -unexpandableReferenceCount;

            return errorLevel;
        }

        LineType DetermineLineType(string line, int lineNumber)
        {
            LineType result;

            if (String.IsNullOrEmpty(line.Trim())) {
                result = LineType.Whitespace;

            } else if (line[0] == '#') {
                result = LineType.Comment;

            } else if (line[0] == '"') {
                result = LineType.StrContinuation;

            } else if (line.StartsWith("msgstr", true, CultureInfo.InvariantCulture)) {
                result = LineType.Msgstr;

            } else if (line.StartsWith("msgid", true, CultureInfo.InvariantCulture)) {
                result = LineType.Msgid;

            } else if (line.StartsWith("msgctxt", true, CultureInfo.InvariantCulture)) {
                result = LineType.Msgctxt;

            } else {
                // Can't parse it, say that it's a comment so that it will be preserved
                result = LineType.Comment;
                ErrorEncountered(lineNumber, "Could not determine the line type");
            }
            return result;
        }


        IEnumerable<LineInfo> BuildListOfLines(StreamReader inputReader)
        {
            int line_num = 0;
            string line;
            List<LineInfo> result = new List<LineInfo>();
            while ((line = inputReader.ReadLine()) != null) {
                line_num++;
                LineType lineType = DetermineLineType(line, line_num);
                result.Add(new LineInfo(lineType, line_num, line));
            }

            return result;
        }


        enum LineInfoState
        {
            FinishedEntry, // looking for the next entry
            AddingMsgid,   // have found the msgid, but it might be split over multiple lines
            AddingMsgstr,  // have found the msgstr, but it might be split over multiple lines
            AddingMsgctxt  // have found a msgctxt, but it might be split over multiple lines
        }

        /// <summary>
        /// Parses the PO format into a dictionary of entries.
        /// </summary>
        Dictionary<string/*MsgInfo.UniqueID*/, MsgInfo> BuildMsgInfoDictionary(IEnumerable<LineInfo> lines)
        {
            Dictionary<string/*MsgInfo.UniqueID*/, MsgInfo> result = new Dictionary<string, MsgInfo>();

            // Add a whitespace entry to the end of the list, so our state machine can rely on
            // whitespace as an end-of-entry marker.
            IEnumerable<LineInfo> linesPlusWhitespace = lines.Concat(new LineInfo[]{new LineInfo(LineType.Whitespace, -1, "")});

            LineInfoState state = LineInfoState.FinishedEntry;
            MsgInfo newEntry = new MsgInfo();

            foreach (LineInfo line in linesPlusWhitespace) {

                // The state machine is small enough to do with a switch.
                //
                // The .PO formats is roughly like this:
                //
                //    Whitespace, followed by
                //    [Optional]# Comments, followed by
                //    [Optional]msgctxt, optionally followed by multi-line context string, followed by
                //    msgid, optionally followed by multi-line id string, followed by
                //    [Optional]msgid_plural - I'm not supporting plural forms, followed by
                //    msgstr, optionally followed by multi-line msg string, followed by
                //    [Optional]msgstr[x] - I'm not supporting plural forms, followed by
                //    EOF or Whitespace
                switch (state) {
                    case LineInfoState.FinishedEntry:
                        // We're looking for the start of the next entry

                        if (line.Type == LineType.Msgid) {
                            state = LineInfoState.AddingMsgid;
                            newEntry.msgid = ExtractString(line.Line, line.LineNumber);

                        } else if (line.Type == LineType.Msgctxt) {
                            // msgctxt is optional, but if present it appears before the msgid
                            state = LineInfoState.AddingMsgctxt;
                            newEntry.msgctxt = ExtractString(line.Line, line.LineNumber); 

                        } else if (line.Type == LineType.Msgstr || line.Type == LineType.StrContinuation) {
                            // An entry can't start with a msgstr or string-continuation
                            ErrorEncountered(line.LineNumber, "Unexpected string or msgstr");
                        }
                        break;

                    case LineInfoState.AddingMsgctxt:                        

                        if (line.Type == LineType.StrContinuation) {
                            newEntry.msgctxt += ExtractString(line.Line, line.LineNumber);

                        } else if (line.Type == LineType.Msgid) {
                            state = LineInfoState.AddingMsgid;
                            newEntry.msgid = ExtractString(line.Line, line.LineNumber);
                        
                        } else {
                            // msgctxt is optional, but if present it appears before the msgid
                            ErrorEncountered(line.LineNumber, "msgid not found after msgctxt");
                        }
                        break;

                    case LineInfoState.AddingMsgid:

                        if (line.Type == LineType.StrContinuation) {
                            newEntry.msgid += ExtractString(line.Line, line.LineNumber);

                        } else if (line.Type == LineType.Msgstr) {
                            state = LineInfoState.AddingMsgstr;
                            newEntry.msgstr = ExtractString(line.Line, line.LineNumber);
                            newEntry.msgstr_linenumber = line.LineNumber;
                        
                        } else if (line.Type == LineType.Msgid) {
                            // We can't have two msgids in a row!
                            if (line.Line.StartsWith("msgid_plural", true, CultureInfo.InvariantCulture)) {
                                ErrorEncountered(line.LineNumber, "Multiple msgids encountered, PO plural-forms are not currently supported :(");
                            } else {
                                ErrorEncountered(line.LineNumber, "Unexpected msgid");
                            }
                        }
                        break;

                    case LineInfoState.AddingMsgstr:

                        if (line.Type == LineType.StrContinuation) {
                            newEntry.msgstr += ExtractString(line.Line, line.LineNumber);
                            newEntry.msgstr_lineCount++;

                        } else if (line.Type == LineType.Whitespace) {
                            // We've found the end of the entry
                            state = LineInfoState.FinishedEntry;
                            if (newEntry.IsValid()) {
                                result.Add(newEntry.UniqueID, newEntry);
                            } else {
                                ErrorEncountered(line.LineNumber, "[End found of] invalid entry");
                            }
                            newEntry = new MsgInfo();

                        } else if (line.Type == LineType.Msgstr) {
                            ErrorEncountered(line.LineNumber, "Multiple msgstrs encountered, PO plural-forms are not currently supported :( - skipping line ");

                        } else {
                            ErrorEncountered(line.LineNumber, "Unexpected line encountered at end of entry");
                        }
                        break;
                }
            }

            return result;
        }


        /// <summary>
        /// Extracts the string specified in the line, but does not unencode any escaped characters
        /// </summary>
        string ExtractString(string encodedLine, int lineNumber)
        {
            string result = String.Empty;

            // find the characters between the left-most " and the right-most "
            int quotePosLeft = encodedLine.IndexOf('"');
            int quotePosRight = encodedLine.LastIndexOf('"');
            
            if (quotePosLeft < 0 || quotePosRight <= quotePosLeft) {
                ErrorEncountered(lineNumber, "Missing quotemarks - very bad - line will be missing from output");

            } else {
                result = encodedLine.Substring(quotePosLeft + 1, quotePosRight - quotePosLeft - 1);
            }

            return result;
        }

        /// <summary>
        /// Expands references in all the MsgInfo.msgstrs in the msgInfoDictionary
        /// </summary>
        /// <param name="totalExpansions">Gives the total successful expansions performed</param>
        /// <returns>Returns how many references were recognised, but left unexpanded</returns>
        int ExpandMsgstrs(IDictionary<string/*msgid*/, MsgInfo>  msgInfoDictionary, out int totalExpansions)
        {
            // mark all MsgInfos that don't need to be expanded
            foreach (MsgInfo msgInfo in msgInfoDictionary.Values) {
                msgInfo.msgstr_containsUnexpandedReferences = ContainsReference(msgInfo.msgstr, msgInfoDictionary);
            }

            int successfulExpansionsTotal = 0;
            int successfulExpansions = 0;
            do {
                successfulExpansionsTotal += successfulExpansions;
                successfulExpansions = 0;                

                // Expand any references to Msgstrs that don't themselves still need expanding
                foreach (MsgInfo msgInfo in msgInfoDictionary.Values) {
                    if (msgInfo.msgstr_containsUnexpandedReferences) {

                        ReferenceInfo reference;
                        int nextRefSearchPos = 0;
                        while ((reference = GetFirstReference(msgInfo.msgstr, nextRefSearchPos)) != null) {

                            nextRefSearchPos = reference.StartIndex + reference.Length;

                            MsgInfo referredMsgInfo;
                            if (msgInfoDictionary.TryGetValue(reference.Msgid, out referredMsgInfo)) {
                                // We have a msgstr with a msgid that matches the reference
                                if (referredMsgInfo.msgstr_containsUnexpandedReferences) {
                                    // Not going to expand it unless/until the referred msgstr has been expanded. This prevents
                                    // indirect self-referential loops etc from being expanded
                                } else {
                                    // Replace the reference with the referred msgstr
                                    msgInfo.msgstr =
                                        msgInfo.msgstr.Substring(0, reference.StartIndex) +
                                        referredMsgInfo.msgstr +
                                        msgInfo.msgstr.Substring(reference.StartIndex + reference.Length, msgInfo.msgstr.Length - (reference.StartIndex + reference.Length));

                                    // If that was the last reference in the msgstr then we must un-mark it as containing 
                                    // unexpanded references.
                                    msgInfo.msgstr_containsUnexpandedReferences = ContainsReference(msgInfo.msgstr, msgInfoDictionary);

                                    msgInfo.msgstr_containsChanges = true;
                                    successfulExpansions++;
                                }
                            }
                        }
                    }
                }

            } while (successfulExpansions > 0);

            totalExpansions = successfulExpansionsTotal;

            return CountUnexpandedReferences(msgInfoDictionary.Values);
        }

        /// <summary>
        /// Returns true if the line contains a reference AND the reference exists in the msgidDictionary
        /// </summary>
        bool ContainsReference(string line, IDictionary<string/*msgid*/, MsgInfo>  msgidDictionary) 
        {
            bool result = false;

            ReferenceInfo reference;
            int nextRefSearchPos = 0;
            while ((reference = GetFirstReference(line, nextRefSearchPos)) != null) {

                nextRefSearchPos = reference.StartIndex + reference.Length;

                if (reference != null && msgidDictionary.ContainsKey(reference.Msgid)) {
                    result = true;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the number of references contained msgidDictionary, regardless of whether or not
        /// they refer to msgids which exist.
        /// </summary>
        int CountUnexpandedReferences(IEnumerable<MsgInfo> MsgInfoList)
        {
            int result = 0;

            foreach (MsgInfo msgInfo in MsgInfoList) {

                ReferenceInfo reference;
                int nextRefSearchPos = 0;
                while ((reference = GetFirstReference(msgInfo.msgstr, nextRefSearchPos)) != null) {
                    result++;
                    nextRefSearchPos = reference.StartIndex + reference.Length;

                    // Use DisplayInfo to avoid setting the errorLevel, since the negative unexpandedReferenceCount
                    // will be assigned to the errorLevel anyway if there are no other errors.
                    //ErrorEncountered(msgInfo.msgstr_linenumber, "Could not resolve reference \"" + reference.Msgid + "\"");
                    DisplayInfo("Warning on line " + msgInfo.msgstr_linenumber + ": Could not resolve reference \"" + reference.Msgid + "\"");
                }
            }
            return result;
        }


        /// <summary>
        /// Returns information about the first reference found, starting for the 0-based
        /// startIndex position in the line.
        /// </summary>
        ReferenceInfo GetFirstReference(string line, int startIndex)
        {
            ReferenceInfo result = null;

            if (startIndex <= line.Length) {

                int openingRefPos = line.IndexOf(cReferenceSignature_Start, startIndex);
                if (openingRefPos >= 0) {
                    // check the reference isn't escaped
                    if (openingRefPos == 0 || line[openingRefPos - 1] != '\\') {
                        // check the reference has a closing "}"
                        int closingRefPos = line.IndexOf(cReferenceSignature_End, openingRefPos + cReferenceSignature_Start.Length);

                        if (closingRefPos > openingRefPos) {
                            result = new ReferenceInfo(
                                openingRefPos, // Start of the reference
                                (closingRefPos - openingRefPos) + cReferenceSignature_End.Length, // characters occupied by the reference
                                line.Substring(openingRefPos + cReferenceSignature_Start.Length, closingRefPos - (openingRefPos + cReferenceSignature_Start.Length)) // referred msgid
                            );
                        }
                    }
                }
            }
            return result;
        }


        /// <summary>
        /// Sends an error message to the console, if errors are not suppressed
        /// and sets the return value to indicate a non-fatal error.
        /// </summary>
        /// <param name="lineNumber">0 if line number is not known, otherwise provide the line of the source file the error was encountered at</param>
        /// <seealso cref="DisplayInfo"/>
        void ErrorEncountered(int lineNumber, string message)
        {
            if (!_options.Silent) {
                if (lineNumber < 1) {
                    Console.WriteLine("Error: " + message);
                } else {
                    Console.WriteLine("Error on line " + lineNumber + ": " + message);
                }
            }
            if (errorLevel == 0) errorLevel = (int)ErrorLevel.NonFatalError;
        }

        /// <summary>
        /// Use this form of ErrorEncountered only if the line number is not known.
        /// </summary>
        /// <seealso cref="DisplayInfo"/>
        void ErrorEncountered(string message)
        {
            ErrorEncountered(0, message);
        }

        /// <summary>
        /// Sends an info message to the console, if info is not suppressed
        /// </summary>
        void DisplayInfo(string message)
        {
            if (!_options.Silent) Console.WriteLine(message);
        }


        public Preprocessor(Options options)
        {
            _options = options;
        }
    }
}
