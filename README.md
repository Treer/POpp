# popp - a PO file preprocessor 
For Gnu .po language translation files. See the [GNU gettext documentation](https://www.gnu.org/software/gettext/manual/html_node/index.htm) for information about PO files:


This preprocessor allows msgstrs to contain expandable references
to other msgstrs. In future it may support conditional directives
such as $define, $if, and $elseif etc.

#### Currently:
  * It does **not** support conditional directives.
  * It does **not** support references in or to plural forms.

#### Language:
  * Written in C#, currently not tested against Mono.
  * popp was written with v3.5 of the .Net framework
  * UnitTests were written with v4.5 of the .Net framework.

## Documentation:

Expands .po msgstrs which reference other msgstr values via a curly brace
notation, for example `{id:ProductName_short}` will be expanded to the msgstr
which has the msgid of `ProductName_short`.

#### Brace notation:

 * `{id:msgid}` or `{id:msgid-msgctxt}`
 * references can be escaped with a backslash, e.g. `\{id:msgid}` is ignored.	
	
**WARNING**: Plural forms are not supported, the file can still be processed,
however lines begining with "msgstr[_n_]" will not have their content expanded,
and plural forms cannot be referenced with the brace notation.

Output files are written in UTF-8


#### Options:

 * -nLF
  - Use LF for newlines

 * -nCRLF
  - Use CRLF for newlines

 * -nSource
  - _[Default]_ Determines LF or CRLF for newlines by what the source file
    uses

 * -silent
  - Suppresses console error messages and info messages

 * -Dsym
  - _[Not implemented]_ Defines a symbol for evaluation of conditional
    expressions such as $IF and $ELSEIF


#### Returns:
 * 0 - Success
 * 1 - fatal error - invalid arguments or file permissions.
 * 2 - fatal error - non-specific.
 * 3 - one or more warnings or non-fatal errors occurred.
 * less than 0 - Success, but not all references could be expanded, the
                  negative return value indicates how may references were
                  found that could not be expanded.
