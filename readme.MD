# zip2

~version 0.9.0~

#### Syntax Samples

##### Create a new zip file

- zip2 --create --file=/tmp/test2.zip readme.MD bin/Debug/dir2.dll
- zip2 -cf /tmp/test3.zip readme.MD bin/Debug/dir2.pdb
- dir2 src/dir2cs/ --relative -X bin,obj,.git -sb | zip2 -cf /tmp/dir2.zip -T -

##### View content of a zip file

- zip2 --list --file=/tmp/test2.zip --excl-file=dummy*.* --excl-dir=obj
- zip2 -lf /tmp/test2.zip -x dummy*.* -X obj
- zip2 -x dummy*.* -X obj -lf /tmp/test2.zip

##### Restore (extract) from a zip file

- zip2 --restore --file=/tmp/test2.zip --output=/tmp/exsiting-dir2 --excl-file=*.dll --excl-dir=tempDir,tmp
- zip2 -rf /tmp/test2.zip -o /tmp/exisiting-dir2 -x *.dll -X tempDir,tmp
- zip2 -x *.dll -X tempDir,tmp -f /tmp/test2.zip -n /tmp/newDir -r

#### Trademarks and Copyright

- SharpZipLib.NETStandard is maintained by ICSharpCode. https://github.com/icsharpcode

- PKZIP is a registered trademarks of PKWARE, Inc.

- AES Decryption Routines by Dr. Brian Gladman

Below are given the license and disclaimer for software written by Dr. Brian Gladman for implementing decryption for AES.

Copyright (c) 2002, Dr Brian Gladman <brg@gladman.me.uk>, Worcester, UK.  All rights reserved.



#### Third party library for zip2
- SharpZipLib.NETStandard (Version 1.0.7)

Yung, Chun Kau

<yung.chun.kau@gmail.com>

2021 Nov 01
