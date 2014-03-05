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
        internal const string cReferenceSignature_Start = "{id:";
        internal const string cReferenceSignature_End   = "}";
        internal const int    cMaxIncludeDepth          = 30; // This limit shouldn't be hit/needed, as we already detect loops

        readonly Options _options;
        int _errorLevel = 0;

        /// <summary>
        /// Provides a list of included files for LineInfo.IncludeFileID to
        /// reference, so that error messages can provide the file the error occured in.
        /// </summary>
        List<string> _includeFileNames = new List<string>();

        /// <summary>
        /// Gets the number of references contained in the input file, regardless
        /// of whether the references can be successfully expanded.
        /// </summary>
        /// <returns>the number of references, or -1 if error</returns>
        /// <param name="inputDirectory">The directory that contained the source file that was supplied to popp, or null (e.g. if stdin was the source)</param>
        public int CountReferences(TextReader inputReader, string inputDirectory)
        {
            int result = 0;

            try {
                // Build a list of information about each line
                IEnumerable<LineInfo> lines = BuildListOfLines(inputReader, inputDirectory, true);

                // Build a dictionary of translation items
                Dictionary<string/*MsgInfo.UniqueID*/, MsgInfo> keyValues = BuildMsgInfoDictionary(lines);

                // Count the references in the list of msgstrs
                foreach (MsgInfo msgInfo in keyValues.Values) {

                    ReferenceInfo reference;
                    int nextRefSearchPos = 0;
                    while ((reference = GetFirstReference(msgInfo.Msgstr_Value, nextRefSearchPos)) != null) {
                        result++;
                        nextRefSearchPos = reference.StartIndex + reference.Length;
                    }
                }

                DisplayInfo("");
                DisplayInfo(result + " references were found.");
                DisplayInfo("");

            } catch (LineException ex) {

                ErrorEncountered(ex.LineInfo, ex.Message);
                _errorLevel = (int)ErrorLevel.FatalError_Internal;

            } catch (Exception ex) {

                ErrorEncountered("Unexpected internal error - " + ex);
                result = -1;
            }

            return result;
        }


        /// <summary>
        /// Run the preprocessor
        /// </summary>
        /// <param name="inputDirectory">Note - can be null</param>
        /// <returns>errorLevel, or 0 for success</returns>
        /// <param name="inputDirectory">The directory that contained the source file that was supplied to popp, or null (e.g. if stdin was the source)</param>
        public int Process(TextReader inputReader, string inputDirectory, TextWriter outputWriter) 
        {
            int unexpandableReferenceCount = 0;
            _errorLevel = 0;

            try {
                // Build a list of information about each line, expanding any $include statements
                IEnumerable<LineInfo> lines = BuildListOfLines(inputReader, inputDirectory, true);

                // Todo:
                // This is where we'll apply the $if $else etc conditional directives
                // lines = ApplyConditionalDirectives(lines);

                // Build a dictionary of translation items
                Dictionary<string/*MsgInfo.UniqueID*/, MsgInfo> keyValues = BuildMsgInfoDictionary(lines);

                int expandedReferenceCount;
                unexpandableReferenceCount = ExpandMsgstrs(keyValues, out expandedReferenceCount);

                // Write out the file with any adjusted msgstr entries...

                // build a second dictionary of the msgstrs we are changing, indexed by the linenumber
                Dictionary<int/*msgstr_lineNumber*/, MsgInfo> alteredMsgstrLines = new Dictionary<int, MsgInfo>();
                foreach (MsgInfo msgInfo in keyValues.Values) {
                    if (msgInfo.Msgstr_ContainsChanges) alteredMsgstrLines.Add(msgInfo.Msgstr_Info.LineNumber, msgInfo);
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
                            if (msgInfo.Msgstr_ContainsChanges) replaceLineWithAdjustedMsgstr = true;
                        }

                        // Only change the lines in the file that we need to, so all whitespace and weird user 
                        // formatting etc will be preserved.
                        if (replaceLineWithAdjustedMsgstr) {
                            outputWriter.WriteLine("msgstr \"" + msgInfo.Msgstr_Value + "\"");
                            linesToSkip = msgInfo.Msgstr_LineCount;

                        } else {
                            outputWriter.WriteLine(lineInfo.Line);
                        }
                    }
                }
                outputWriter.Close();

                DisplayInfo("Done - Expanded " + expandedReferenceCount + " references");
                if (unexpandableReferenceCount > 0) {
                    DisplayInfo("       Failed to expand " + unexpandableReferenceCount + " references");
                }

            } catch (LineException ex) {

                ErrorEncountered(ex.LineInfo, ex.Message);
                _errorLevel = (int)ErrorLevel.FatalError_Internal;

            } catch (Exception ex) {

                ErrorEncountered("Unexpected internal error - " + ex);
                _errorLevel = (int)ErrorLevel.FatalError_Internal;
            }

            // If the preprocessing was successful, then return the number of references
            // we found which could not be expanded, as a negative number (to prevent 
            // confusion with the error codes)
            if (_errorLevel == 0) _errorLevel = -unexpandableReferenceCount;

            return _errorLevel;
        }

        /// <summary>
        /// Returns a TextReader for the file pointed to by the $include line.
        /// Remember to Dispose() of it.
        /// May return null. 
        /// </summary>
        /// <param name="inputDirectory">The directory that contained the source file that was supplied to popp, or null (e.g. if stdin was the source)</param>
        /// <param name="currentIncludeDirectory">null unless 'line' is from an $included file, in which case it 
        /// should contain the directory containing the $included file so we can add that to our search path</param>
        /// <param name="includedFileNameAndPath">set to null unless the function returns a TextReader, in 
        /// which case it's </param>
        TextReader IncludedTextReader(LineInfo line, string inputDirectory, string currentIncludeDirectory, out string includedFileNameAndPath)
        {
            TextReader result = null;
            FileStream sourceStream = null;

            includedFileNameAndPath = null;

            try {
                string pathAndFileName = null;
                string fileName = ExtractString(line);

                // Add inputDirectory and "no directory" to the front of the list of include-directories to search
                List<string> directoryList = new List<string>();
                directoryList.Add(inputDirectory);
                if (currentIncludeDirectory != null) directoryList.Add(currentIncludeDirectory);
                directoryList.Add("");
                directoryList.AddRange(_options.IncludeDirectories);

                foreach (string directory in directoryList) {

                    string tempPath = Path.Combine(directory, fileName);
                    if (File.Exists(tempPath)) {
                        pathAndFileName = tempPath;
                        break;
                    }
                }

                if (pathAndFileName == null) {

                    throw new LineException(
                        line,
                        "$included source-file not found" +
                        (String.IsNullOrEmpty(fileName) ? "" : ": " + fileName)
                    );

                } else {

                    sourceStream = new FileStream(pathAndFileName, FileMode.Open, FileAccess.Read);
                    result = new StreamReader(sourceStream);
                    includedFileNameAndPath = Path.GetFullPath(pathAndFileName);
                }

            } catch (LineException) {
                throw;

            } catch (Exception ex) {

                if (sourceStream != null) sourceStream.Close();
                throw new LineException(line, "Unexpected error while expanding $include: " + ex);
            }

            return result;
        }


        LineType DetermineLineType(string line, int lineNumber, int includeFileID)
        {
            LineType result;

            if (String.IsNullOrEmpty(line.Trim())) {
                result = LineType.Whitespace;

            } else if (line[0] == '#') {
                result = LineType.Comment;

                if (line.Length > 11 && line[2] == '$' && (line[1] == ' ' || line[1] == '.')) {
                    // this could be an $include statement, as we allow includes of the alternate forms:
                    // # $include
                    // #.$include
                    if (line.Substring(3, 8).ToLower() == "include ") result = LineType.IncludeStatement;
                }

            } else if (line[0] == '"') {
                result = LineType.StrContinuation;

            } else if (line.StartsWith("msgstr", true, CultureInfo.InvariantCulture)) {
                result = LineType.Msgstr;

            } else if (line.StartsWith("msgid", true, CultureInfo.InvariantCulture)) {
                result = LineType.Msgid;

            } else if (line.StartsWith("msgctxt", true, CultureInfo.InvariantCulture)) {
                result = LineType.Msgctxt;

            } else if (line.StartsWith("$include ", true, CultureInfo.InvariantCulture)) {
                result = LineType.IncludeStatement;

            } else {
                // Can't parse it, say that it's a comment so that it will be preserved
                result = LineType.Comment;
                ErrorEncountered(lineNumber, includeFileID, "Could not determine the line type");
            }
            return result;
        }

        IEnumerable<LineInfo> BuildListOfLines(TextReader inputReader, string inputDirectory, bool expandIncludes)
        {
            List<int> includeFileIDStack = new List<int>();

            return BuildListOfLines(inputReader, inputDirectory, expandIncludes, includeFileIDStack);
        }

        /// <param name="expandIncludes">if true the referenced file will be inserted, if false, the include will be removed</param>
        /// <param name="depth">how many includes deep are we</param>
        /// <param name="includeFileStack">tracks the include files we are inside of, to prevent loops, and to know how deep we are</param>
        /// <param name="inputDirectory">The directory that contained the source file that was supplied to popp, or null (e.g. if stdin was the source)</param>
        IEnumerable<LineInfo> BuildListOfLines(TextReader inputReader, string inputDirectory, bool expandIncludes, List<int> includeFileIDStack)
        {
            int line_num = 0;
            int includeFileID = -1;
            string includeDirectory = null;
                                   
            if (includeFileIDStack.Count > 0) {
                // We're inside an include file, find our current directory
                includeFileID = includeFileIDStack.First();
                includeDirectory = Path.GetDirectoryName(_includeFileNames[includeFileID]);
            }

            string line;
            List<LineInfo> result = new List<LineInfo>();
            while ((line = inputReader.ReadLine()) != null) {

                line_num++;
                LineType lineType = DetermineLineType(line, line_num, includeFileID);
                LineInfo lineInfo = new LineInfo(lineType, line_num, includeFileID, line);

                if (lineType != LineType.IncludeStatement) {

                    result.Add(lineInfo);

                } else {
                    // this line is an $include statement
                    if (expandIncludes && includeFileIDStack.Count < cMaxIncludeDepth) {
                        // Expand the included file

                        string includeStatement_File;
                        TextReader includeStatement_FileReader = IncludedTextReader(lineInfo, inputDirectory, includeDirectory, out includeStatement_File);
                        
                        if (includeStatement_FileReader != null) using(includeStatement_FileReader) {

                            int includeStatement_FileID = _includeFileNames.IndexOfFileInList(includeStatement_File);
                            
                            if (includeFileIDStack.Contains(includeStatement_FileID)) {
                                throw new LineException(lineInfo, "Recursive $include statement found. Aborting.");
                            }

                            if (includeStatement_FileID < 0) {
                                // The $include statement is pointing to a new include file, add it to the global list of _includeFileNames
                                _includeFileNames.Add(includeStatement_File);
                                includeStatement_FileID = _includeFileNames.Count - 1;
                            }

                            // push includeStatement_FileID onto includeFileIDStack
                            includeFileIDStack.Insert(0, includeStatement_FileID); 
                            try {
                                IEnumerable<LineInfo> includedLines = BuildListOfLines(includeStatement_FileReader, inputDirectory, true, includeFileIDStack);
                                result.AddRange(includedLines);
                            } finally {
                                // pop includeStatement_FileID off includeFileIDStack
                                includeFileIDStack.RemoveAt(0);
                            }
                        }
                    }
                }
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
        /// Parse the PO formatted file into a dictionary of entries.
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
                            newEntry.Msgid_Value = ExtractString(line);

                        } else if (line.Type == LineType.Msgctxt) {
                            // msgctxt is optional, but if present it appears before the msgid
                            state = LineInfoState.AddingMsgctxt;
                            newEntry.Msgctxt_Value = ExtractString(line); 

                        } else if (line.Type == LineType.Msgstr || line.Type == LineType.StrContinuation) {
                            // An entry can't start with a msgstr or string-continuation
                            ErrorEncountered(line, "Unexpected string or msgstr");
                        }
                        break;

                    case LineInfoState.AddingMsgctxt:                        

                        if (line.Type == LineType.StrContinuation) {
                            newEntry.Msgctxt_Value += ExtractString(line);

                        } else if (line.Type == LineType.Msgid) {
                            state = LineInfoState.AddingMsgid;
                            newEntry.Msgid_Value = ExtractString(line);
                        
                        } else {
                            // msgctxt is optional, but if present it appears before the msgid
                            ErrorEncountered(line, "msgid not found after msgctxt");
                        }
                        break;

                    case LineInfoState.AddingMsgid:

                        if (line.Type == LineType.StrContinuation) {
                            newEntry.Msgid_Value += ExtractString(line);

                        } else if (line.Type == LineType.Msgstr) {
                            state = LineInfoState.AddingMsgstr;
                            newEntry.Msgstr_Value = ExtractString(line);
                            newEntry.Msgstr_Info = line;
                        
                        } else if (line.Type == LineType.Msgid) {
                            // We can't have two msgids in a row!
                            if (line.Line.StartsWith("msgid_plural", true, CultureInfo.InvariantCulture)) {
                                ErrorEncountered(line, "Multiple msgids encountered, PO plural-forms are not currently supported :(");
                            } else {
                                ErrorEncountered(line, "Unexpected msgid");
                            }
                        }
                        break;

                    case LineInfoState.AddingMsgstr:

                        if (line.Type == LineType.StrContinuation) {
                            newEntry.Msgstr_Value += ExtractString(line);
                            newEntry.Msgstr_LineCount++;

                        } else if (line.Type == LineType.Whitespace) {
                            // We've found the end of the entry
                            state = LineInfoState.FinishedEntry;
                            if (newEntry.IsValid()) {
                                string uniqueID = newEntry.UniqueID(_options.CaseSensitiveIDs);
                                if (result.ContainsKey(uniqueID)) {
                                    // Duplicate msgids encountered - Abort if the msgstr contains a reference, as duplicate msgids are now supported yet and will result in the reference not expanding
                                    TestDuplicateEntry(newEntry);
                                } else {
                                    result.Add(newEntry.UniqueID(_options.CaseSensitiveIDs), newEntry);
                                }
                            } else {
                                ErrorEncountered(line, "[End found of] invalid entry");
                            }
                            newEntry = new MsgInfo();

                        } else if (line.Type == LineType.Msgstr) {
                            ErrorEncountered(line, "Multiple msgstrs encountered, PO plural-forms are not currently supported :( - skipping line ");

                        } else {
                            ErrorEncountered(line, "Unexpected line encountered at end of entry \"" + newEntry.Msgid_Value + "\" (was expecting whitespace)");
                        }
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Duplicate msgids encountered - Abort if the msgstr contains a reference, as
        /// duplicate msgids are now supported yet and will result in the msgstr not being expanded.
        /// </summary>
        void TestDuplicateEntry(MsgInfo duplicateEntry) {

            if (GetFirstReference(duplicateEntry.Msgstr_Value, 0) != null) {
                throw new LineException(duplicateEntry.Msgstr_Info, "Duplicate msgid \"" + duplicateEntry.Msgid_Value + "\" encountered - Aborting because this is not supported yet and the msgstr contains a reference which popp will fail to expand.");
            } else {
                ErrorEncountered(duplicateEntry.Msgstr_Info, "Duplicate msgid \"" + duplicateEntry.Msgid_Value + "\" encountered (non-fatal).");
            }
        }


        /// <summary>
        /// Extracts the string inside the left-most " and the right-most "
        /// i.e. extracts the string specified in the line, but does not unencode any escaped characters
        /// </summary>
        string ExtractString(LineInfo lineInfo)
        {
            string result = String.Empty;

            // find the characters between the left-most " and the right-most "
            int quotePosLeft = lineInfo.Line.IndexOf('"');
            int quotePosRight = lineInfo.Line.LastIndexOf('"');
            
            if (quotePosLeft < 0 || quotePosRight <= quotePosLeft) {
                // A note about the wording of this error message: It can occur when quotes are missing
                // from a .PO entry such as a msgid or msgstr, but it can also happen if quotes are missing
                // from an $include statement intended for popp.
                ErrorEncountered(lineInfo, "Missing quotemarks - very bad - line will be missing from output");

            } else {
                result = lineInfo.Line.Substring(quotePosLeft + 1, quotePosRight - quotePosLeft - 1);
            }

            return result;
        }

        /// <summary>
        /// Expands references in all the MsgInfo.msgstrs in the msgInfoDictionary
        /// </summary>
        /// <param name="totalExpansions">Gives the total successful expansions performed</param>
        /// <returns>Returns how many references were recognised, but left unexpanded</returns>
        int ExpandMsgstrs(IDictionary<string/*MsgInfo.UniqueID*/, MsgInfo>  msgInfoDictionary, out int totalExpansions)
        {
            // mark all MsgInfos that don't need to be expanded
            foreach (MsgInfo msgInfo in msgInfoDictionary.Values) {
                msgInfo.Msgstr_ContainsUnexpandedReferences = ContainsReference(msgInfo.Msgstr_Value, msgInfoDictionary);
            }

            int successfulExpansionsTotal = 0;
            int successfulExpansions = 0;
            do {
                successfulExpansionsTotal += successfulExpansions;
                successfulExpansions = 0;                

                // Expand any references to Msgstrs that don't themselves still need expanding
                foreach (MsgInfo msgInfo in msgInfoDictionary.Values) {
                    if (msgInfo.Msgstr_ContainsUnexpandedReferences) {

                        ReferenceInfo reference;
                        int nextRefSearchPos = 0;
                        while ((reference = GetFirstReference(msgInfo.Msgstr_Value, nextRefSearchPos)) != null) {

                            nextRefSearchPos = reference.StartIndex + reference.Length;

                            MsgInfo referredMsgInfo;
                            if (msgInfoDictionary.TryGetValue(reference.UniqueID(_options.CaseSensitiveIDs), out referredMsgInfo)) {
                                // We have a msgstr with a msgid that matches the reference
                                if (referredMsgInfo.Msgstr_ContainsUnexpandedReferences) {
                                    // Not going to expand it unless/until the referred msgstr has been expanded. This prevents
                                    // indirect self-referential loops etc from being expanded
                                } else {
                                    // Replace the reference with the referred msgstr
                                    msgInfo.Msgstr_Value =
                                        msgInfo.Msgstr_Value.Substring(0, reference.StartIndex) +
                                        referredMsgInfo.Msgstr_Value +
                                        msgInfo.Msgstr_Value.Substring(reference.StartIndex + reference.Length, msgInfo.Msgstr_Value.Length - (reference.StartIndex + reference.Length));

                                    // If that was the last reference in the msgstr then we must un-mark it as containing 
                                    // unexpanded references.
                                    msgInfo.Msgstr_ContainsUnexpandedReferences = ContainsReference(msgInfo.Msgstr_Value, msgInfoDictionary);

                                    msgInfo.Msgstr_ContainsChanges = true;
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
        /// Returns true if the line contains a reference AND the reference exists in the msgInfoDictionary
        /// </summary>
        bool ContainsReference(string line, IDictionary<string/*MsgInfo.UniqueID*/, MsgInfo> msgInfoDictionary) 
        {
            bool result = false;

            ReferenceInfo reference;
            int nextRefSearchPos = 0;
            while ((reference = GetFirstReference(line, nextRefSearchPos)) != null) {

                nextRefSearchPos = reference.StartIndex + reference.Length;

                if (reference != null && msgInfoDictionary.ContainsKey(reference.UniqueID(_options.CaseSensitiveIDs))) {
                    result = true;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the number of references contained in msgInfoList, regardless of whether or not
        /// they refer to msgids which exist.
        /// </summary>
        int CountUnexpandedReferences(IEnumerable<MsgInfo> msgInfoList)
        {
            int result = 0;

            foreach (MsgInfo msgInfo in msgInfoList) {

                ReferenceInfo reference;
                int nextRefSearchPos = 0;
                while ((reference = GetFirstReference(msgInfo.Msgstr_Value, nextRefSearchPos)) != null) {
                    result++;
                    nextRefSearchPos = reference.StartIndex + reference.Length;

                    // Use DisplayInfo to avoid setting the errorLevel, since the negative unexpandedReferenceCount
                    // will be assigned to the errorLevel if there are no other errors.
                    DisplayInfo("Warning on line " + LineNumberToString(msgInfo.Msgstr_Info) + ": Could not resolve reference \"" + reference.Msgid + "\"");
                }
            }
            return result;
        }


        /// <summary>
        /// Returns information about the first reference found, starting from the 0-based
        /// startIndex position in the line. Null is returned if the line has no reference.
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


        void ErrorEncountered(int lineNumber, int includeFileID, string message)
        {
            ErrorEncountered(
                new LineInfo(LineType.Whitespace, lineNumber, includeFileID, null), // construct a dummy LineInfo to save having to duplicate the ErrorEncountered code
                message
            );
        }


        /// <summary>
        /// Sends an error message to the console, if errors are not suppressed
        /// and sets the return value to indicate a non-fatal error.
        /// </summary>
        /// <param name="lineNumber">0 if line number is not known, otherwise provide the line of the source file the error was encountered at</param>
        /// <seealso cref="DisplayInfo"/>
        void ErrorEncountered(LineInfo lineInfo, string message)
        {
            if (!_options.Quiet) {
                if (lineInfo == null) {   
                    // todo: write this to stderr   
                    Console.WriteLine("Error: " + message);
                } else {
                    Console.WriteLine("Error on " + LineNumberToString(lineInfo) + ": " + message);
                }
            }
            if (_errorLevel == 0) _errorLevel = (int)ErrorLevel.NonFatalError;
        }


        /// <summary>
        /// Returns either 'line x' or 'line x of "file.po"' depending on whether the 
        /// line came from the source file or an included file.
        /// </summary>
        /// <param name="lineInfo"></param>
        /// <returns></returns>
        string LineNumberToString(LineInfo lineInfo) {

            if (lineInfo.IncludeFileID >= 0) {

                return String.Format(
                        "line {0} of \"{1}\"",
                        lineInfo.LineNumber,
                        Path.GetFileName(_includeFileNames[lineInfo.IncludeFileID])
                );

            } else {
                return "line " + lineInfo.LineNumber;
            }
        }
        
        
        /// <summary>
        /// Use this form of ErrorEncountered only if the line number is not known.
        /// </summary>
        /// <seealso cref="DisplayInfo"/>
        void ErrorEncountered(string message)
        {
            ErrorEncountered(null, message);
        }

        /// <summary>
        /// Sends an info message to the console, if info is not suppressed
        /// </summary>
        void DisplayInfo(string message)
        {
            if (!_options.Quiet) Console.WriteLine(message);
        }


        public Preprocessor(Options options)
        {
            _options = options;
        }
    }
}
