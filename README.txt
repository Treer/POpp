popp - a PO file preprocessor (for Gnu .po language translation files)

See the GNU gettext documentation for information about PO files:
https://www.gnu.org/software/gettext/manual/html_node/index.htm

This preprocessor allows msgstrs to contain expandable references
to other msgstrs. In future it may support conditional directives
such as #define, $if, and $elseif etc. but

Currently:
  It does NOT support conditional directives.
  It does NOT support plural forms.

Licence:
  This project is GPL, it contains a small amount of code obtained
  from the mono project under GPL licence. If I remove that code
  then it can be released under other licences, however given that
  PO files are a Gnu initiative, I don't yet see any advantage in
  it being released under other licences.

Language:
  Written in C#, currently not tested against Mono.

------------------------------------------------------------------------------
Documentation:
------------------------------------------------------------------------------

Expands .po msgstrs which reference other msgstr values via a curly brace
notation, for example {id:ProductName_short} will be expanded to the msgstr
which has the msgid of "ProductName_short".

Brace notation:

    {id:msgid}
    or
    {id:msgid-msgctxt}

    references can be escaped with a backslash, e.g. \{id:msgid} is ignored.	
	
WARNING: Plural forms are not supported, the file can still be processed,
however lines begining with "msgstr[n]" will not have their content expanded,
and plural forms cannot be referenced with the brace notation.

Output files are written in UTF-8


Options:

-nLF
    Use LF for newlines

-nCRLF
    Use CRLF for newlines

-nSource
    [Default] Determines LF or CRLF for newlines by what the source file
    uses.

-silent
    Suppresses console error messages and info messages.

-D<sym>
    [Not implemented] Defines a symbol for evaluation of conditional
    expressions such as $IF and $ELSEIF


Returns:
    0 - success
    1 - fatal error - invalid arguments or file permissions.
    2 - fatal error - non-specific.
    3 - one or more warnings or non-fatal errors occurred.
    less than 0 - success, but not all references could be expanded, the
                  negative return value indicates how may references were
                  found that could not be expanded.