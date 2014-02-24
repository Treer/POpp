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

    public class Program
    {
        internal const string cProgramVersion = "v0.1";
        internal const string cProgramNameShort = "popp";
        internal const string cProgramNameFull = "PO preprocessor";

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

                    case "--silent":
                    case "-silent":
                    case "/silent":
                    case "-s":
                    case "/s":
                        options.Silent = true;
                        break;

                    case "--count":
                    case "-count":
                    case "/count":
                    case "-c":
                    case "/c":
                        options.CountReferences = true;
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
                            resInf.OutputFileName = Path.GetFullPath(args[i]);
                        }
                        else
                        {
                            resInf.InputFileName = Path.GetFullPath(args[i]);
                            resInf.OutputFileName = Path.ChangeExtension(resInf.InputFileName,
                                "po");
                        }
                        inputFiles.Add(resInf);
                        break;
                }
            }

            if (inputFiles.Count == 0)
            {
                ShowUsage();
                return (int)ErrorLevel.FatalError_InvalidArgs;
            }

            foreach (TaskInfo res in inputFiles)
            {
                int ret = PreprocessFile(res.InputFileName, res.OutputFileName, options);
                if (ret != 0) return ret;
            }
            return 0;        
        }


        static int PreprocessFile(string sname, string dname, Options options)
        {
            FileStream source = null;
            FileStream dest = null;

            try {
                source = new FileStream(sname, FileMode.Open, FileAccess.Read);
                if (!options.CountReferences) {
                    dest = new FileStream(dname, FileMode.Create, FileAccess.Write);
                } else {
                    // When counting references in the input file we don't need an
                    // output file - the count result is returned as the errorLevel.
                }
                
            } catch (Exception ex) {

                Console.WriteLine("Error: {0}", ex.Message);
                if (source != null) source.Close();
                if (dest != null)   dest.Close();

                return (int)ErrorLevel.FatalError_InvalidArgs;
            }

            Preprocessor pp = new Preprocessor(options);
            if (options.CountReferences) {
                return pp.CountReferences(source);
            } else {
                return pp.Process(source, dest);
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

-s, --silent
    Suppresses console error messages and info messages.

-c, --count    
    Returns the number of references contained in the source file, regardless
    of whether the references are valid and can be expanded. No output file 
    is written.
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
