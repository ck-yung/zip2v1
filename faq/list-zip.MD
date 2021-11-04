# Howo to list backuping files in a zip file?

## List all backup files in a zip file

    zip2 --list --file=/backup3.dir/backup5.zip

### Shortcut command:

    zip2 -lf /backup3.dir/backup5.zip

## List some files in a zip file

    zip2 *.cs --excl-file=test*.* --excl-dir=temp* --file=/backup3.dir/backup5.zip --list

### Shortcut command:

    dir2 *.cs -x test*.* -X temp* -f /backup3.dir/backup5.zip -l

## List the grouping by file extensions in a zip file

    zip2 --sum=ext --list --file=backup5.zip

### Shortcut command:

    dir2 --sum=ext -lf backup5.zip

## List filename only in a zip file

    zip2 --hide=ratio,size,date,crypted --total=off --file=backup5.zip

### Shortcut command:

    dir2 -blf backup5.zip

## List some file info in a zip file 

    zip2 --hide=ratiodate,crypted --show=compress --file=backup5.zip

## Inquiry on-line help

    zip2 --list

    zip2 -l
