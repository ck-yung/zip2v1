# Howo to create a new zip file?

## Create a new zip file for some given filenames

    zip2 --create --file=/backup3.dir/backup4new.zip readme.MD dir2cs/readme.MD

### Shortcut command:

    zip2 readme.MD dir2cs/readme.MD -cf /backup3.dir/backup4new.zip

## Create a new zip file for a directory excluing files *.dll and directories bin, obj

    dir2 ef6samples --dir=sub --excl-file=*.dll --excl-dir=bin --excl-dir=obj | zip2 -cf /backup3.dir/new5.zip --files-from=-

### Shortcut command:

    dir2 ef6samples -sx *.dll -X bin,obj | zip2 -cf /backup3.dir/new5.zip -T -

## Inquiry on-line help

    zip2 --create

    zip2 -c

* Remark:

dir2 is a dotnet tool and is able to be installed by command

    dotnet install -g dir2
