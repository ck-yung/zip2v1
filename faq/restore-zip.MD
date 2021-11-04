# Howo to extract a zip file?

## Restore all backup files in a zip file to a new directory

    zip2 --restore --file=/backup3.dir/backup5.zip --new-dir=/tmp/newDir

### Shortcut command:

    zip2 -rf /backup3.dir/backup5.zip -n /tmp/newDir

## Restore some files in a zip file to a existing directory

    zip2 *.cs --file=/backup3.dir/backup5.zip --restore --excl-file=test*.* --excl-dir=temp*

### Shortcut command:

    dir2 *.cs -f /backup3.dir/backup5.zip -r -x test*.* -X temp*

## Restore some files listing in a file "2019-06-12-list.txt"

    zip2 --files-from=2019-06-12-list.txt --file=backup5.zip --restore

### Shortcut command:

    zip2 -T 2019-06-12-list.txt -f backup5.zip -r

* Remark:

The file referred by option  "--files-from=" should be contained filenames only. Wild-card should not be contained.

## Inquiry on-line help

    zip2 --restore

    zip2 -r
