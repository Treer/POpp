// .po preprocessor
//
// This file is MIT X11 license
//
// It contains code modified from the mono project under MIT X11 license.
namespace popp
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Resources;
    using System.Text;

    /// <summary>
    /// Handles the user interface. i.e. deals with the commandline args
    /// then invokes the Preprocessor
    /// </summary>
    public class Program
    {
        internal const string cProgramVersion = "v0.12";
        internal const string cProgramNameShort = "popp";
        internal const string cProgramNameFull = "PO preprocessor";

        internal const string cNewline_LF = "\n";
        internal const string cNewline_CRLF = "\r\n";
        internal const string cNewline_Default = cNewline_CRLF;

        public static int Main(string[] args)
        {
            List<TaskInfo> inputFiles = new List<TaskInfo>();
            Options options = new Options();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--help":
                    case "-h":
                    case "/h":
                    case "-?":
                    case "/?":
                        ShowUsage();
                        return (int)ErrorLevel.Success;

                    case "--version":
                        ShowVersion();
                        return (int)ErrorLevel.Success;

                    case "--nlf":
                    case "-nlf":
                    case "/nlf":
                    case "-nl":
                    case "/nl":
                        options.NewlinePreference = NewLineOptions.LF;
                        break;

                    case "--ncrlf":
                    case "-ncrlf":
                    case "/ncrlf":
                    case "-nc":
                    case "/nc":
                        options.NewlinePreference = NewLineOptions.CRLF;
                        break;

                    case "--nsource":
                    case "-nsource":
                    case "/nsource":
                    case "-ns":
                    case "/ns":
                        options.NewlinePreference = NewLineOptions.SameAsSource;
                        break;

                    case "--quiet":
                    case "-quiet":
                    case "/quiet":
                    case "-q":
                    case "/q":
                    case "--silent":
                    case "-silent":
                    case "/silent":
                        options.Quiet = true;
                        break;

                    case "--sensitive":
                    case "-sensitive":
                    case "/sensitive":
                    case "-s":
                    case "/s":
                    case "--casesensitive":
                    case "-casesensitive":
                    case "/casesensitive":
                        options.CaseSensitiveIDs = true;
                        break;


                    case "--count":
                    case "-count":
                    case "/count":
                    case "-c":
                    case "/c":
                        options.CountReferences = true;
                        break;

                    case "--includedirectory":
                    case "-includedirectory":
                    case "/includedirectory":
                    case "-i":
                    case "/i":
                        if ((i + 1) < args.Length) {
                            if (AddIncludeDirectory(args[i + 1], options)) {
                                i++;
                            } else {
                                return (int)ErrorLevel.FatalError_InvalidArgs;
                            }
                        } else {
                            ShowUsage();
                            return (int)ErrorLevel.FatalError_InvalidArgs;
                        }
                        break;


                    default:
                        if (!IsFileArgument(args[i]))
                        {
                            ShowUsage();
                            return (int)ErrorLevel.FatalError_InvalidArgs;
                        }

                        TaskInfo resInf = new TaskInfo();
                        if ((i + 1) < args.Length)
                        {
                            resInf.InputFileName = Path.GetFullPath(args[i]);
                            // move to next arg, since we assume that one holds
                            // the name of the output file
                            i++;

                            if (args[i] == "-") {
                                // specifying the last filename as '-' will override the default 
                                // behaviour of using a default named output file, will use stdout instead
                                resInf.OutputFileName = String.Empty;
                            } else {
                                resInf.OutputFileName = Path.GetFullPath(args[i]);
                            }
                        }
                        else
                        {
                            resInf.InputFileName = Path.GetFullPath(args[i]);
                            resInf.OutputFileName = Path.ChangeExtension(resInf.InputFileName, "po");

                            if (resInf.InputFileName == resInf.OutputFileName) {
                                // The input file already had a .po extension! Our output file will fail to open

                                if (options.CountReferences) {
                                    // When counting references in the input file we don't need an
                                    // output file - the count result is returned as the errorLevel.
                                } else {
                                    Console.WriteLine(
                                        "Error: When only a source file is provided, popp will assume a .po extension\r\n" +
                                        "       for the output file, but this input file already has a .po extension.\r\n\r\n" +
                                        "       If you want the output sent to stdout instead, then specify a hyphen (-)\r\n" +
                                        "       as the output file."
                                    );
                                    return (int)ErrorLevel.FatalError_InvalidArgs;
                                }
                            }

                        }
                        inputFiles.Add(resInf);
                        break;
                }
            }

            if (inputFiles.Count == 0)
            {
                // no files were specified, assume they want to use stdin/stdout

                bool stdInputAvailable;
                try {
                    stdInputAvailable = Console.KeyAvailable;
                } catch {
                    // Apparently Console.KeyAvailable can throw an exception if the stdin is coming from a file
                    // See https://stackoverflow.com/questions/3961542/checking-standard-input-in-c-sharp
                    stdInputAvailable = true;
                }

                if (stdInputAvailable) {

                    TaskInfo resInf = new TaskInfo();
                    resInf.InputFileName = String.Empty;
                    resInf.OutputFileName = String.Empty;
                    inputFiles.Add(resInf);

                } else {

                    ShowUsage();
                    return (int)ErrorLevel.FatalError_InvalidArgs;
                }
            }

            foreach (TaskInfo res in inputFiles)
            {
                int ret = PreprocessFile(res.InputFileName, res.OutputFileName, options);
                if (ret != 0) return ret;
            }
            return 0;        
        }


        /// <summary>
        /// Adds the directory to the IncludeDirectories list in options.
        /// Writes error to the console and returns false if the directory doesn't exist.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        static bool AddIncludeDirectory(string directory, Options options) {

            bool result = false;

            string includeDir = null;
            try {
                if (Directory.Exists(directory)) {
                    includeDir = Path.GetFullPath(directory);
                }
            } catch (Exception ex) {
                Console.WriteLine("Argument error, could not find directory \"" + directory + "\": " + ex);
            }

            if (String.IsNullOrEmpty(includeDir)) {
                Console.WriteLine("Argument error, could not find directory \"" + directory + "\"");
            } else {
                options.IncludeDirectories.Add(includeDir);
                result = true;
            }

            return result;
        }


        static int PreprocessFile(string sname, string dname, Options options)
        {
            TextReader inputReader;
            TextWriter outputWriter = null;

            FileStream sourceStream = null;
            FileStream destStream = null;
            string sourceDirectory = null;
            try {

                if (sname == String.Empty) {
                    inputReader = Console.In;
                } else {
                    sourceStream = new FileStream(sname, FileMode.Open, FileAccess.Read);
                    inputReader = new StreamReader(sourceStream);

                    try {
                        sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(sname));
                    } catch (Exception ex) {
                        // This shouldn't happen unless permissions are really screwy, or I've made a terrible mistake.
                        Console.WriteLine("Non-fatal exception attempting to read source path: " + ex);
                    }
                }

                if (options.CountReferences) {
                    // When counting references in the input file we don't need an
                    // output file - the count result is returned as the errorLevel.
                } else {
                    if (dname == String.Empty) {
                        outputWriter = Console.Out;
                        options.Quiet = true; // Don't output info messages in the middle of the file ;)
                    } else {

                        // Unicode BOM causes syntax errors in the gettext utils
                        Encoding utf8WithoutBom = new UTF8Encoding(false);

                        outputWriter = new StreamWriter(
                            new FileStream(dname, FileMode.Create, FileAccess.Write),
                            utf8WithoutBom
                        );
                    }

                    // determine which newline character to use.
                    string newline = cNewline_Default;
                    switch (options.NewlinePreference) {
                        case NewLineOptions.SameAsSource:
                            // lookahead in inputReader to see whether the line is broken with LF or CRLF
                            if (sourceStream != null && sourceStream.CanSeek) {
                                long startPosition = sourceStream.Position;
                                int peekedChar;
                                while ((peekedChar = sourceStream.ReadByte()) != -1) {
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
                                sourceStream.Seek(startPosition, SeekOrigin.Begin);
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
                }

            } catch (Exception ex) {

                Console.WriteLine("Error: {0}", ex.Message);
                if (sourceStream != null) sourceStream.Close();
                if (destStream   != null) destStream.Close();

                return (int)ErrorLevel.FatalError_InvalidArgs;
            }

            Preprocessor pp = new Preprocessor(options);
            if (options.CountReferences) {
                return pp.CountReferences(inputReader, sourceDirectory);
            } else {
                return pp.Process(inputReader, sourceDirectory, outputWriter);
            }
        }

        private static bool RunningOnUnix
        {
            get
            {
                // check for Unix platforms - see FAQ for more details
                // http://www.mono-project.com/FAQ:_Technical#How_to_detect_the_execution_platform_.3F
                int platform = (int)Environment.OSVersion.Platform;
                return ((platform == 4) || (platform == 128) || (platform == 6));
            }
        }

        private static bool IsFileArgument(string arg)
        {
            if ((arg[0] != '-') && (arg[0] != '/'))
                return true;

            // cope with absolute filenames for resx files on unix, as
            // they also match the option pattern
            //
            // `/home/test.resx' is considered as a resx file, however
            // '/test.resx' is considered as error
            return (RunningOnUnix && arg.Length > 2 && arg.IndexOf('/', 2) != -1);
        }

        static void ShowVersion()
        {
            Console.WriteLine(cProgramNameFull + " " + cProgramVersion);
        }

        static void ShowUsage()
        {

            string Usage = cProgramNameFull + " " + cProgramVersion +
                @"
 (Available from github.com/Treer/POpp)

Usage:
    popp [options] source.popp [dest.po]";
            Usage += @"

Expands .po msgstrs which reference other msgstr values via a curly brace
notation, for example {id:ProductName_short} will be expanded to the msgstr
which has the msgid of ""ProductName_short"".

Brace notation:

    {id:msgid} 
    or 
    {id:msgid-msgctxt}

    references can be escaped with a backslash, e.g. \{id:msgid} is ignored.

WARNING: Plural forms are not supported, the file can still be processed,
however lines begining with ""msgstr[n]"" will not have their content 
expanded, and plural forms cannot be referenced with the brace notation.

Output files are written in UTF-8

";

            Usage += @"

Options:

-nl, --nLF
    Use LF for newlines

-nc, --nCRLF
    Use CRLF for newlines 
    
-ns, --nSource
    [Default] Determines LF or CRLF for newlines by what the source file
    uses.

-q, --quiet
    Suppresses console error messages and info messages

-s, --sensitive, --casesensitive
    The msgids inside curly-brace-references are matched case-insensitively 
    by default, the --sensitive option will only expand case-sensitive 
    matches.

-c, --count    
    Returns the number of references contained in the source file, regardless
    of whether the references are valid and can be expanded. No output file 
    is written. Can be used as a second pass to confirm all references have
	been expanded (none were misspelled etc), but

    WARNING: Plural forms are not supported and references contained in 
    plural-form msgstrs are not counted.

-D<sym>
    [Not implemented] Defines a symbol for evaluation of conditional 
    expressions such as $IF and $ELSEIF
    ";
            Usage += @"

Returns:
    0 - success
    1 - fatal error - invalid arguments or file permissions.
    2 - fatal error - non-specific.
    3 - one or more warnings or non-fatal errors occurred.
    less than 0 - success, but not all references could be expanded, the
                  negative return value indicates how may references were 
                  found that could not be expanded.
";
            Console.WriteLine(Usage);
        }

    }
}
