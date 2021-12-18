using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Immutable;

namespace zip2.restore
{
    internal class Command : zip2.list.Command
    {
        public override int Invoke()
        {
            switch (string.IsNullOrEmpty(Password),
                string.IsNullOrEmpty(PasswordFrom),
                string.IsNullOrEmpty(PasswordFromRaw))
            {
                case (true, false, true):
                    using (var inpFs = File.OpenText(PasswordFrom))
                    {
                        var textThe = inpFs.ReadToEnd().Trim();
                        if (string.IsNullOrEmpty(textThe))
                        {
                            TotalPrintLine($"File '{PasswordFrom}' contains blank content!");
                            return 1;
                        }
                        ((IParser)Password).Parse(textThe);
                    }
                    break;
                case (true, true, false):
                    using (var inpFs = File.OpenRead(PasswordFromRaw))
                    {
                        var readSize = new FileInfo(PasswordFromRaw).Length;
                        if (1 > readSize)
                        {
                            TotalPrintLine($"File '{PasswordFromRaw}' is empty!");
                            return 1;
                        }
                        var buf = new byte[readSize];
                        inpFs.Read(buf);
                        var textThe = System.Text.Encoding.UTF8.GetString(buf);
                        ((IParser)Password).Parse(textThe);
                    }
                    break;
                case (false, false, _):
                case (false, _, false):
                case (_, false, false):
                    TotalPrintLine(
                        " Only one of '--password', '--password-from' and '--password-from-raw' can be assigned.");
                    return 1;
                default:
                    break;
            }

            string? basicOutputDir = null;

            switch (string.IsNullOrEmpty(OutputDir),
                string.IsNullOrEmpty(NewOutputDir))
            {
                case (false, false):
                    Console.WriteLine("Cannot both assign 'output-dir' and 'new-dir'");
                    return 1;

                case (false, true):
                    if (!Directory.Exists(OutputDir))
                    {
                        Console.WriteLine(
                            $"Output dir '{OutputDir}' is NOT found!");
                        return 1;
                    }
                    basicOutputDir = OutputDir;
                    break;

                case (true, false):
                    basicOutputDir = NewOutputDir;
                    if (basicOutputDir == "-")
                    {
                        basicOutputDir = Path.GetFileNameWithoutExtension( zipFilename);
                    }
                    if (Directory.Exists(basicOutputDir))
                    {
                        Console.WriteLine(
                            $"New output dir '{basicOutputDir}' is FOUND!");
                        return 1;
                    }
                    Directory.CreateDirectory(basicOutputDir);
                    ItemPrintLine($"New output dir {basicOutputDir} is created");
                    break;

                default:
                    break;
            }

            var toEnvirDirSep = Helper.GetEnvirDirSepFunc();
            switch (string.IsNullOrEmpty(basicOutputDir),
                (bool)SkipOldPath)
            {
                case (true, true):
                    ToOutputFilename = (it) => Path.GetFileName(it);
                    break;
                case (true, false):
                    ToOutputFilename = (it) => toEnvirDirSep(it);
                    break;
                case (false, true):
                    ToOutputFilename = (it) => toEnvirDirSep(
                        Path.Join(basicOutputDir,
                        Path.GetFileName(it)));
                    break;
                default:
                    ToOutputFilename = (it) => toEnvirDirSep(
                        Path.Join(basicOutputDir,it));
                    break;
            }

            var dirMovePriorTo = Path.Join(basicOutputDir,
                "zip2prior "
                + DateTime.Now.ToString("s").Replace(":","-")
                + "." + DateTime.Now.ToString("fff"));
            var zFile = new ZipFile(File.OpenRead(zipFilename));
            if (!string.IsNullOrEmpty(Password))
            {
                zFile.Password = Password;
            }
            var tmpExtThe = "." + Guid.NewGuid().ToString("N");
            var countRestore = MyGetZipEntires(zFile)
                .Where((it) => NameFilter.Invoke(it.Name))
                .Where((it) => !ExclNameFilter.Invoke(it.Name))
                .Where((it) => !ExclDirFilter.Invoke(it.Name))
                .Where((it) => !it.IsDirectory)
                .Select((it) => new
                {
                    Entry = it,
                    TargetFilename = ToOutputFilename(it.Name)
                })
                .Select((it) =>
                {
                    bool rtn = false;
                    ItemPrint(it.Entry.Name);
                    string tmpFilename = "?";
                    try
                    {
                        tmpFilename = it.TargetFilename + tmpExtThe;
                        string? dirThe = Path.GetDirectoryName(tmpFilename);
                        if (!string.IsNullOrEmpty(dirThe) && !Directory.Exists(dirThe))
                        {
                            Directory.CreateDirectory(dirThe);
                        }

                        using (var streamThe = new FileStream(tmpFilename, FileMode.Create))
                        using (var inpStream = zFile.GetInputStream(it.Entry))
                        {
                            inpStream.CopyTo(streamThe, 32 * 1024);
                        }

                        ForceRename(
                            oldFilename:tmpFilename,
                            moveOldToDir:dirMovePriorTo,
                            targetFilename:it.TargetFilename,
                            originalTimestamp:it.Entry.DateTime);
                        tmpFilename = "?";
                        rtn = true;
                    }
                    catch (ZipException zipEe)
                    {
                        ItemErrorPrintFilename(it.Entry.Name);
                        ItemErrorPrintMessage($" {zipEe.Message}");
                        if (File.Exists(tmpFilename))
                            File.Delete(tmpFilename);
                    }
                    catch (Exception ee)
                    {
                        ItemErrorPrintFilename(it.Entry.Name);
                        ItemPrint(" ");
                        var checkDebug = Environment
                        .GetEnvironmentVariable("zip2");
                        if (checkDebug?.Contains(":debug:")??false)
                        {
                            ItemErrorPrintMessage($" {ee.ToString()}");
                        }
                        else
                        {
                            ItemErrorPrintMessage($" {ee.Message}");
                        }
                        if (File.Exists(tmpFilename))
                            File.Delete(tmpFilename);
                    }
                    ItemPrint(Environment.NewLine);
                    return rtn;
                })
                .Where((it) => it)
                .Count();

            switch (countMovePrior)
            {
                case 1:
                    TotalPrintLine(
                        $" One existing file is moved to {dirMovePriorTo}");
                    break;
                case > 1:
                    TotalPrintLine(
                        $" {countMovePrior} existing files are moved to {dirMovePriorTo}");
                    break;
                default:
                    break;
            }

            switch (countRestore)
            {
                case 0:
                    TotalPrintLine(" No file is restored.");
                    break;
                case 1:
                    TotalPrintLine(" One file is restored.");
                    break;
                default:
                    TotalPrintLine($" {countRestore} files are restored.");
                    break;
            }

            return 0;
        }

        static Action<string, DateTime> SetTimestamp =
            (filename, timestamp) => File.SetLastWriteTime(filename, timestamp);

        int countMovePrior = 0;
        void ForceRename( string oldFilename,
            string moveOldToDir,
            string targetFilename,
            DateTime originalTimestamp)
        {
            var theFilename = targetFilename;

            if (File.Exists(theFilename))
            {
                var moveOldToPathname = Path.Join(moveOldToDir,
                    targetFilename);
                var dir2 = Path.GetDirectoryName(moveOldToPathname);
                if (!string.IsNullOrEmpty(dir2) && !Directory.Exists(dir2))
                {
                    Directory.CreateDirectory(dir2);
                }
                var oldFilename2 = Path.GetFileNameWithoutExtension(moveOldToPathname);
                int oldCount2 = 0;
                var oldExtension2 = Path.GetExtension(moveOldToPathname);
                while (File.Exists(moveOldToPathname))
                {
                    oldCount2 += 1;
                    moveOldToPathname = Path.Join(moveOldToDir,
                    $"{oldFilename2}({oldCount2}){oldExtension2}");
                }
                (new FileInfo(theFilename)).MoveTo(moveOldToPathname);
                countMovePrior += 1;
            }
            (new FileInfo(oldFilename)).MoveTo(theFilename);
            SetTimestamp(theFilename, originalTimestamp);
        }

        public override int SayHelp()
        {
            return SayHelp(nameof(restore), opts,
                OptionShortCuts, SwitchShortCuts,
                optionalAction:()=>
                {
                    Console.Write($" {ExclFilePrefix,23}");
                    Console.WriteLine("FILENAME[,FILEWILD ..]");
                    Console.Write($" {ExclDirPrefix,23}");
                    Console.WriteLine("DIRNAME[,DIRWILD] ..]");
                });
        }

        public override bool Parse(
            IEnumerable<string> args)
        {
            (string[] optOther, string[] otherArgs) = opts.ParseFrom(
                Helper.ExpandToOptions(args,
                switchShortcuts: SwitchShortCuts,
                optionShortcuts: OptionShortCuts));

            (string[] optExclNames, IEnumerable<string> otherArgs2)
            = Helper.SubractStartsWith(
                optOther.AsEnumerable(), ExclFilePrefix);
            optExclNames = optExclNames
                .Select((it) => it.Substring(ExclFilePrefix.Length))
                .Select((it) => it.Split(","))
                .SelectMany((it) => it)
                .Select((it) => it.Trim())
                .Where((it) => it.Length > 0)
                .Distinct()
                .ToArray();
            if (optExclNames.Length > 0)
            {
                ExclNameFilter = ToNameAnyMatchFilter(optExclNames);
            }

            (string[] optExclDir, IEnumerable<string> optUnknownSeq)
            = Helper.SubractStartsWith(
                otherArgs2, ExclDirPrefix);
            optExclDir = optExclDir
                .Select((it) => it.Substring(ExclDirPrefix.Length))
                .Select((it) => it.Split(","))
                .SelectMany((it) => it)
                .Select((it) => it.Trim())
                .Where((it) => it.Length > 0)
                .Distinct()
                .ToArray();

            if (optExclDir.Length > 0)
            {
                ExclDirFilter = ToDirPartAnyMatch(optExclDir);
            }

            var optUnknown = optUnknownSeq.ToArray();
            if (optUnknown.Length > 0)
            {
                throw new InvalidValueException(
                    optUnknown[0], nameof(list));
            }

            if (otherArgs.Length > 0)
            {
                NameFilter = ToNameAnyMatchFilter(otherArgs);
            }

            if (!ParseFilesForm())
            {
                return false;
            }

            if (!ExpandZipFilename())
            {
                Console.WriteLine($"'{zipFilename}' is NOT a zip file.");
                return false;
            }

            return true;
        }

        static ParameterOptionString OutputDir =
            new ParameterOptionString(
                "output-dir", help: "OUTPUT_DIR",
                defaultValue: string.Empty);

        static ParameterOptionString NewOutputDir =
            new ParameterOptionString(
                "new-dir", help: "NEW_OUTPUT_DIR ( - to use zipfilename)",
                defaultValue: string.Empty);

        Func<string, string> ToOutputFilename = (it) => it;

        static ParameterSwitch SkipOldPath =
            new ParameterSwitch("no-dir");

        static ParameterSwitch NotUpdateLastWriteTime =
            new ParameterSwitch("no-timestamp",
                whenSwitch: () =>
                {
                    SetTimestamp = (_, _) => { };
                });

        static ImmutableDictionary<string, string[]> SwitchShortCuts =
            new Dictionary<string, string[]>
            {
                ["-q"] = new string[] { "--quiet" },
            }.ToImmutableDictionary<string, string[]>();

        static ImmutableDictionary<string, string> OptionShortCuts =
            new Dictionary<string, string>
            {
                [QuietShortcut] = QuietText,
                ["-T"] = FilesFromPrefix,
                ["-o"] = "--output-dir=",
                ["-n"] = "--new-dir=",
                ["-x"] = ExclFilePrefix,
                ["-X"] = ExclDirPrefix,
            }.ToImmutableDictionary<string, string>();

        static IParser[] opts =
        {
            Quiet,
            TotalOff,
            FilesFrom,
            SkipOldPath,
            OutputDir,
            NewOutputDir,
            NotUpdateLastWriteTime,
            Password,
            PasswordFrom,
            PasswordFromRaw,
        };
    }
}
