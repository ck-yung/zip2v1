using ICSharpCode.SharpZipLib.Zip;
using System.Collections.Immutable;

namespace zip2.restore
{
    internal class Command : zip2.list.Command
    {
        public override int Invoke()
        {
            if (!File.Exists(zipFilename))
            {
                WriteConsole($"Zip file '{zipFilename}' is NOT found!");
                WriteConsole(Environment.NewLine);
                return 1;
            }

            switch (NameFilter == Helper.StringFilterAlwaysTrue,
                string.IsNullOrEmpty(FilesFrom))
            {
                case (false, false):
                    WriteConsole("Cannot handle files from --files-from");
                    WriteConsole($"={FilesFrom} and command-line arg FILE.");
                    WriteConsole(Environment.NewLine);
                    return 1;

                case (true, false):
                    if (FilesFrom == "-")
                    {
                        if (!Console.IsInputRedirected)
                        {
                            Console.WriteLine("Only support redir input.");
                            return 1;
                        }

                        NameFilter = ToNameAnyMatchFilter(
                            Helper.ReadConsoleAllLines()
                            .Select((it) => it.Trim())
                            .Where((it) => it.Length > 0)
                            .Select((it) => Helper.ToStandardDirSep(it))
                            .Distinct());
                    }
                    else
                    {
                        NameFilter = ToNameAnyMatchFilter(
                            File.ReadAllLines(FilesFrom)
                            .Select((it) => it.Trim())
                            .Where((it) => it.Length > 0)
                            .Distinct());
                    }
                    break;
                default:
                    break;
            }

            var toEnvirDirSep = Helper.GetEnvirDirSepFunc();

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

                    if (SkipOldPath)
                    {
                        ToOutputFilename = (it) =>
                        toEnvirDirSep(Path.Join(OutputDir,
                            Path.GetFileName(it)));
                    }
                    else
                    {
                        ToOutputFilename = (it) =>
                        toEnvirDirSep(Path.Join(OutputDir, it));
                    }
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

                    if (SkipOldPath)
                    {
                        ToOutputFilename = (it) =>
                        toEnvirDirSep(Path.Join(NewOutputDir,
                            Path.GetFileName(it)));
                    }
                    else
                    {
                        ToOutputFilename = (it) =>
                        toEnvirDirSep(Path.Join(NewOutputDir, it));
                    }
                    break;
                default:
                    break;
            }

            var zFile = new ZipFile(File.OpenRead(zipFilename));
            var countRestore = zFile.GetZipEntries()
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
                            rtn = true;
                        }
                        ForceRename(tmpFilename,it.TargetFilename);
                    }
                    catch (Exception ee)
                    {
                        WriteConsole(ee.Message);
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

        void ForceRename( string oldFileame, string targetFilename)
        {
            int cnt = 0;
            var theFilename = targetFilename;
            var dirFileName = Path.GetFileNameWithoutExtension(theFilename);
            var extThe = Path.GetExtension(theFilename);
            while (File.Exists(theFilename))
            {
                cnt += 1;
                theFilename = $"{dirFileName} ({cnt}){extThe}";
            }
            (new FileInfo(oldFileame)).MoveTo(targetFilename);
        }

        public override int SayHelp()
        {
            base.SayHelp(nameof(restore), opts);

            Console.Write($"  {ExclFilePrefix,19}");
            Console.WriteLine("FILENAME[,FILEWILD ..]");
            Console.Write($"  {ExclDirPrefix,19}");
            Console.WriteLine("DIRNAME[,DORWILD] ..]");

            bool ifShortCut = false;
            if (SwitchShortCuts.Any())
            {
                ifShortCut = true;
                Console.WriteLine("Shortcut:");
                foreach (var opt in SwitchShortCuts)
                {
                    Console.Write($"{opt.Key,19} ->");
                    Console.WriteLine($"  {string.Join("  ", opt.Value)}");
                }
            }
            if (OptionShortCuts.Any())
            {
                if (!ifShortCut) Console.WriteLine("Shortcut:");
                foreach (var opt in OptionShortCuts)
                {
                    Console.WriteLine($"{opt.Key,19} ->  {opt.Value}");
                }
            }

            return 0;
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

        Func<string, string> ToOutputFilename =
            (it) => it;

        static ParameterSwitch SkipOldPath =
            new ParameterSwitch("no-dir");

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
        };
    }
}
