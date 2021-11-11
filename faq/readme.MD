# zip2 FAQ

## What is new feature of ver 0.9.3

- Add option '--password=' to "create" command and "restore" command. For example, password "1234" would be applied to a zip file if '--password=1234' is assigned.
- Ask user for console input password if '--password=-'
- Read password from a text file if '--password-from=FILE' is assigned. The input text will be trimmed space and CRLF.
- Read password from a file if '--password-from-raw=FILE' is aassinged.
- Fix bug to "restore" command if '--no-dir' and some filenames are duplicated.
- Nofity user if some existing files are moved to "zip2 prior _TIMESTAMP_" directory.

## Create a new zip file

[Go to create demo](https://github.com/ck-yung/zip2/blob/master/faq/create-zip.MD)

## Restore or extract from a new zip file

[Go to create demo](https://github.com/ck-yung/zip2/blob/master/faq/restore-zip.MD)

## List content of a new zip file

[Go to create demo](https://github.com/ck-yung/zip2/blob/master/faq/list-zip.MD)

## What is next?

While creating a new zip file, suppress duplicate file.

_Enjoy!_

Yung, Chun Kau

