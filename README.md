# popp - a PO file preprocessor 
For Gnu .po language translation files. See the [GNU gettext documentation](https://www.gnu.org/software/gettext/manual/html_node/index.html) for information about PO files.


This preprocessor allows msgstrs to contain expandable references
to other msgstrs, and should work well with .PO file editors. In 
future it may support conditional directives such as $define, $if, 
and $include etc.

#### Currently:
  * It does *not* support conditional directives.
  * It does *not* support references to or from plural forms, though the rest of the file will be processed.
  * An executable for Windows can be [downloaded here](https://mega.co.nz/#!jN02nTya!-u0OEfuKOuq-dZ79kFH8oIPfWMM7U4M4h74s5JR5rGQ).
   - md5: ce21d05523c066f9380392c9ee3ceb57 *popp.exe
   - sha256: 6d425a25092fe3fe5eda88f072c162bd6796d6d48c1a52cb28a3f0d89179351f *popp.exe
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

Output files are written in UTF-8


#### Options:

 * -nl, --nLF
  - Use LF for newlines

 * -nc, --nCRLF
  - Use CRLF for newlines

 * -ns, --nSource
  - _[Default]_ Determines LF or CRLF for newlines by what the source file
    uses

 * -s, --silent
  - Suppresses console error messages and info messages

 * -c, --count    
  - Returns the number of references contained in the source file, regardless
    of whether the references are valid and can be expanded. No output file 
    is written.
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
