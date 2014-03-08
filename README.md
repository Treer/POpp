# popp - a PO file preprocessor 

[popp website](http://treer.github.io/POpp/)

For Gnu .po language translation files. See the [GNU gettext documentation](https://www.gnu.org/software/gettext/manual/html_node/index.html) for information about PO files.

The preprocessor allows your translated text to reference other items of 
translated text, in a way that should work within any .PO editor. The use 
of $include statements is also supported, and conditional directives 
might be added in future.

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


#### Currently:
  * **The Windows executable can be [downloaded here](https://mega.co.nz/#!7NVEHCZQ!zL9zvNUzWA-Hl5yyHA5jLY-PstFutCUpRNjujEWAO5M)** (v0.2.0)
   - md5: 51f4071976faf82f7cbcd8cf9325fe02 *popp.exe
  * It does *not* support conditional directives.
  * It does *not* support references to or from plural forms, though the rest of the file can be processed.
  * I'm still in the process of trying popp in a real project environment, so for now assume it's only been tested with the automatic test cases, and take care.

#### Licence:
MIT X11
  
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
however lines begining with "msgstr[_n_]" cannot contain references,
and plural forms cannot be referenced with the brace notation.

#### Include directives can have the following notation:                                                                                                                                                         
                                                                                                                                                                                                            
    $include "fileName.po"   
    	
    # $include "fileName.po"                                                                                                                                                                                
    #.$include "fileName.po"                                                                                                                                                                                
                                                                                                                                                                                                            
The .po hash-comment notations can be used if the file must be editable or                                                                                                                                  
parsable by other .po tools before being processed by popp.


Output files are written in UTF-8. If your source languages use unicode
characters that your shell can't display, then avoid using pipes and stick with 
specifying an input file and an output file. (Or proceed very carefully)


#### Options:

 * -nl, --nLF
  - Use LF for newlines.

 * -nc, --nCRLF
  - Use CRLF for newlines.

 * -ns, --nSource
  - _[Default]_ Determines LF or CRLF for newlines by what the source file
    uses.

 * -s, --sensitive, --casesensitive
  - The msgids inside curly-brace-references are matched case-insensitively 
    by default, the --sensitive option expands case-sensitive matches only.
  
 * -c, --count    
  - Returns the number of references contained in the source file, regardless
    of whether the references are valid and can be expanded. No output file 
    is written. Can be used as a second pass to confirm all references have
    been expanded (none were misspelled etc), but
	
    WARNING: Plural forms are not supported and references contained in 
    plural-form msgstrs are not counted.
                                                                                                                                                                                                            
-i [path], --includeDirectory [path]                                                                                                                                                                        
    Adds a directory to the end of the search path used to locate files                                                                                                                                     
    specified by $include directives.
	
 * -q, --quiet
  - Suppresses console error messages and info messages

  
#### Returns:
 * 0 - Success
 * 1 - fatal error - invalid arguments or file permissions.
 * 2 - fatal error - non-specific.
 * 3 - one or more warnings or non-fatal errors occurred.
 * less than 0 - Success, but not all references could be expanded, the
                  negative return value indicates how may references were
                  found that could not be expanded.
