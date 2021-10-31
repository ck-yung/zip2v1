using System.Collections.Immutable;
using ICSharpCode.SharpZipLib.Zip;

namespace zip2.create
{
    internal class Command : CommandBase
    {
        public override int Invoke()
        {
            switch (FilenamesToBeBackup.Count(),
                string.IsNullOrEmpty(FilesFrom))
            {
                case (0, true):
                    WriteConsole("No file to be backup.");
                    WriteConsole(Environment.NewLine);
                    return 1;
                case ( > 0, false):
                    WriteConsole("Cannot handle files from --files-from");
                    WriteConsole($"={FilesFrom} and command-line arg FILE.");
                    WriteConsole(Environment.NewLine);
                    return 1;
                case (0, false):
                    if (FilesFrom == "-")
                    {
                        if (!Console.IsInputRedirected)
                        {
                            Console.WriteLine("Only support redir input.");
                            return 1;
                        }
                        FilenamesToBeBackup.AddRange(
                            Helper.ReadConsoleAllLines()
                            .Select((it) => it.Trim())
                            .Where((it) => it.Length > 0)
                            .Select((it) => Helper.ToStandardDirSep(it))
                            .Distinct());
                    }
                    else
                    {
                        FilenamesToBeBackup.AddRange(File
                            .ReadAllLines(FilesFrom)
                            .Select((it) => it.Trim())
                            .Where((it) => it.Length > 0)
                            .Distinct());
                    }

                    if (FilenamesToBeBackup.Count()==0)
                    {
                        WriteConsole("No file is found in '--files-from'");
                        WriteConsole($" '{FilesFrom}'");
                        WriteConsole(Environment.NewLine);
                        return 1;
                    }
                    break;
                default:
                    break;
            }

            if (File.Exists(zipFilename))
            {
                WriteConsole($"Output zip file '{zipFilename}' is found!");
                WriteConsole(Environment.NewLine);
                return 1;
            }

            int countAdd = 0;
            using (ZipOutputStream zs = new ZipOutputStream(
                File.Create(zipFilename)))
            {
                zs.UseZip64 = UseZip64.Dynamic;
                zs.SetLevel(CompressLevel);

                foreach (var filename in FilenamesToBeBackup)
                {
                    WriteConsole(filename);
                    WriteConsole(" ");
                    countAdd += (AddToZip(filename, zs)) ? 1 : 0;
                    WriteConsole(Environment.NewLine);
                }

                zs.Finish();
                zs.Close();
            }

            switch (countAdd)
            {
                case 0:
                    WriteConsole("No file is stored.");
                    if (File.Exists(zipFilename))
                    {
                        File.Delete(zipFilename);
                    }
                    break;
                case 1:
                    WriteConsole("One file is stored.");
                    break;
                default:
                    WriteConsole($"{countAdd} files are stored.");
                    break;
            }
            WriteConsole(Environment.NewLine);

            return 0;
        }

        bool AddToZip(string filename, ZipOutputStream zs)
        {
            try
            {
                if (!File.Exists(filename))
                {
                    WriteConsole("not found");
                    return false;
                }

                var entry = new ZipEntry(Helper.ToStandardDirSep(filename))
                {
                    DateTime = File.GetLastWriteTime(filename),
                    Size = (new FileInfo(filename)).Length,
                };

                zs.PutNextEntry(entry);
                byte[] buffer = new byte[32 * 1024];
                using (FileStream fs = File.OpenRead(filename))
                {
                    for (int readSize = fs.Read(buffer, 0, buffer.Length);
                        readSize > 0;
                        readSize = fs.Read(buffer, 0, buffer.Length))
                    {
                        zs.Write(buffer, 0, readSize);
                    }
                }

                return true;
            }
            catch (Exception ee)
            {
                WriteConsole(ee.Message);
                return false;
            }
        }

        static ImmutableDictionary<string, string[]> SwitchShortCuts =
            new Dictionary<string, string[]>
            {
                ["-q"] = new string[] { "--quiet"},
                ["-0"] = new string[] { "--compress-level=0" },
                ["-1"] = new string[] { "--compress-level=1" },
                ["-2"] = new string[] { "--compress-level=2" },
            }.ToImmutableDictionary<string, string[]>();

        static ImmutableDictionary<string, string> OptionShortCuts =
            new Dictionary<string, string>
            {
                ["-T"] = FilesFromPrefix,
            }.ToImmutableDictionary<string, string>();

        List<string> FilenamesToBeBackup = new List<string>();
        Action<string> WriteConsole = (msg) => Console.Write(msg);

        public override bool Parse(IEnumerable<string> args)
        {
            (string[] optUnknown, string[] filenamesToBeBackup) =
                opts.ParseFrom(
                Helper.ExpandToOptions(args,
                switchShortcuts: SwitchShortCuts,
                optionShortcuts: OptionShortCuts));
            FilenamesToBeBackup.AddRange(
                filenamesToBeBackup
                .Select((it) => it.Trim())
                .Where((it) => it.Length > 0)
                .Distinct());
            if (Quiet)
            {
                WriteConsole = (_) => { };
            }
            return true;
        }

        public override int SayHelp()
        {
            base.SayHelp(nameof(create), opts
                , "NEWZIPFILE");

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

        static readonly ParameterOption<int> CompressLevel
            = new ParameterOptionSetter<int>("compress-level",
                "0|1|2  (default 1)", 5,
                parse: (val,obj) =>
                {
                    switch (val)
                    {
                        case "0":
                            obj.SetValue(0);
                            return true;
                        case "1":
                            obj.SetValue(5);
                            return true;
                        case "2":
                            obj.SetValue(9);
                            return true;
                        default:
                            return false;
                    }
                });

        static IParser[] opts =
        {
            Quiet,
            FilesFrom,
            (IParser) CompressLevel,
        };
    }
}
