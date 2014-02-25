# popp - a PO file preprocessor 
For Gnu .po language translation files. See the [GNU gettext documentation](https://www.gnu.org/software/gettext/manual/html_node/index.html) for information about PO files.


This preprocessor allows your translated text to contain references
to other items of translated text, in a way that should work within any .PO editor. 

For example, you can have .PO source files like this:

    msgid "ProductName short"
    msgstr "POpp"
    
    msgid "Welcome text"
    msgstr "Congratulations on your download of {id:productname short}"
    
which can be converted automatically to this:

    msgid "ProductName short"
    msgstr "POpp"
    
    msgid "Welcome text"
    msgstr "Congratulations on your download of POpp"



In future, popp may also support $include and conditional directives such as $define, $if, $else etc.

#### Currently:
  * **The Windows executable can be [downloaded here](https://mega.co.nz/#!zYkiSDIA!zzQkqeOChgqUiUYsXKQDNaW1X0ZMdw2suyYrrbtUFt4)** (v0.12)
   - md5: f60c75ad0efeb28194430cd62ab0a9ce *popp.exe
   - sha256: 6ad63858827c9bd92282136fad26f831c74489f25dd23aa1801bb88bf9c0a134 *popp.exe
  * It does *not* support conditional directives.
  * It does *not* support references to or from plural forms, though the rest of the file will be processed.
  * I haven't tried this in a real project environment yet, it's only been tested with the automatic test cases - take care.

#### Language:
  * C#, compiles with Mono and Visual Studio, but the UnitTests project is a Visual Studio one (VS Express 2013).
  * popp requires v3.5 of the .Net framework.
  * UnitTests require v4.0 of the .Net framework.

## Documentation:

Expands .po msgstrs which reference other msgstr values via a curly brace
notation, for example `{id:ProductName_short}` will be expanded to the msgstr
which has the msgid of `ProductName_short`.

#### Usage:                                                                                                                                                                         
    popp [options] source.popp [dest.po]

#### Brace notation:

 * `{id:msgid}` or `{id:msgid-msgctxt}`
 * references can be escaped with a backslash, e.g. `\{id:msgid}` is ignored.	
	
**WARNING**: Plural forms are not supported, the file can still be processed,
however lines begining with "msgstr[_n_]" will not have their content expanded,
and plural forms cannot be referenced with the brace notation.

Output files are written in UTF-8. If your source languages use unicode
characters that your shell can't display, then avoid using pipes and stick with 
specifying an input file and an output file. (Or proceed very carefully)


#### Options:

 * -nl, --nLF
  - Use LF for newlines

 * -nc, --nCRLF
  - Use CRLF for newlines

 * -ns, --nSource
  - _[Default]_ Determines LF or CRLF for newlines by what the source file
    uses

 * -q, --quiet
  - Suppresses console error messages and info messages

 * -s, --sensitive, --casesensitive
  - The msgids inside curly-brace-references are matched case-insensitively 
    by default, the --sensitive option expands case-sensitive matches only.
  
 * -c, --count    
  - Returns the number of references contained in the source file, regardless
    of whether the references are valid and can be expanded. No output file 
    is written. Can be used to check for misspelled msgids, but
    WARNING: Plural forms are not supported and references contained in 
    plural-form msgstrs are not counted.
	
 * -dSym
  - _[Not implemented]_ Defines a symbol for evaluation of conditional
    expressions such as $if and $elseif

#### Returns:
 * 0 - Success
 * 1 - fatal error - invalid arguments or file permissions.
 * 2 - fatal error - non-specific.
 * 3 - one or more warnings or non-fatal errors occurred.
 * less than 0 - Success, but not all references could be expanded, the
                  negative return value indicates how may references were
                  found that could not be expanded.
