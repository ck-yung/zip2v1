using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Immutable;

namespace zip2.restore
{
    internal class Command : zip2.list.Command
    {
        public override int Invoke()
        {
            string? basicOutputDir = null;

            switch (string.IsNullOrEmpty(OutputDir),
                string.IsNullOrEmpty(NewOutputDir))
            {
                case (false, false):
                    WriteConsole(
                        "Cannot both assign 'output-dir' and 'new-dir'");
                    WriteConsole(Environment.NewLine);
                    return 1;

                case (false, true):
                    if (!Directory.Exists(OutputDir))
                    {
                        Console.Error.WriteLine(
                            $"Output dir '{OutputDir}' is NOT found!");
                        return 1;
                    }
                    basicOutputDir = OutputDir;
                    break;

                case (true, false):
                    if (Directory.Exists(NewOutputDir))
                    {
                        Console.Error.WriteLine(
                            $"New output dir '{NewOutputDir}' is FOUND!");
                        return 1;
                    }
                    Directory.CreateDirectory(NewOutputDir);
                    WriteConsole($"New output dir {NewOutputDir} is created");
                    WriteConsole(Environment.NewLine);
                    basicOutputDir = NewOutputDir;
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

            var zFile = new ZipFile(File.OpenRead(zipFilename));
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
                    WriteConsole(it.Entry.Name);
                    try
                    {
                        string tmpFilename = it.TargetFilename + "." + Guid.NewGuid(
                            ).ToString("N");
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
                        ForceRename(tmpFilename,it.TargetFilename,it.Entry.DateTime);
                        rtn = true;
                    }
                    catch (Exception ee)
                    {
                        WriteConsole(" ");
                        WriteConsole(ee.ToString());
                    }
                    WriteConsole(Environment.NewLine);
                    return rtn;
                })
                .Where((it) => it)
                .Count();
            switch (countRestore)
            {
                case 0:
                    WriteConsole("No file is restored.");
                    break;
                case 1:
                    WriteConsole("One file is restored.");
                    break;
                default:
                    WriteConsole($"{countRestore} files are restored.");
                    break;
            }
            WriteConsole(Environment.NewLine);

            return 0;
        }

        static Action<string, DateTime> SetTimestamp =
            (filename, timestamp) => File.SetLastWriteTime(filename, timestamp);

        void ForceRename( string oldFileame, string targetFilename,
            DateTime originalTimestamp)
        {
            int cnt = 0;
            var theFilename = targetFilename;
            var dir2 = Path.GetDirectoryName(theFilename);
            var dirWithFileName = (string.IsNullOrEmpty(dir2))
                ? Path.GetFileNameWithoutExtension(theFilename)
                : Path.Join(dir2, Path.GetFileNameWithoutExtension(theFilename));
            var extThe = Path.GetExtension(theFilename);

            while (File.Exists(theFilename))
            {
                cnt += 1;
                theFilename = $"{dirWithFileName} ({cnt}){extThe}";
            }

            (new FileInfo(oldFileame)).MoveTo(theFilename);

            if (cnt==0)
            {
                if (File.Exists(targetFilename))
                {
                    SetTimestamp(targetFilename, originalTimestamp);
                }
            }
            else
            {
                WriteConsole($" -> {theFilename}");
            }
        }

        public override int SayHelp()
        {
            return SayHelp(nameof(restore), opts,
                OptionShortCuts, SwitchShortCuts,
                optionalAction:()=>
                {
                    Console.Write($"  {ExclFilePrefix,19}");
                    Console.WriteLine("FILENAME[,FILEWILD ..]");
                    Console.Write($"  {ExclDirPrefix,19}");
                    Console.WriteLine("DIRNAME[,DORWILD] ..]");
                });
        }

        Action<string> WriteConsole = (msg) => Console.Write(msg);

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

            if (Quiet)
            {
                WriteConsole = (_) => { };
            }

            if (!ParseFilesForm())
            {
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
                "new-dir", help: "NEW_OUTPUT_DIR",
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
                ["-T"] = FilesFromPrefix,
                ["-o"] = "--output-dir=",
                ["-n"] = "--new-dir=",
                ["-x"] = ExclFilePrefix,
                ["-X"] = ExclDirPrefix,
            }.ToImmutableDictionary<string, string>();

        static IParser[] opts =
        {
            Quiet,
            FilesFrom,
            SkipOldPath,
            OutputDir,
            NewOutputDir,
            NotUpdateLastWriteTime,
        };
    }
}
