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
            var zDirThe = Path.GetDirectoryName(zipFilename);
            var zFilename = Path.GetFileName(zipFilename);
            var tempFilename = zFilename + "."
                + Path.GetRandomFileName() + ".tmp";
            var tempPathname = (string.IsNullOrEmpty(zDirThe))
                ? tempFilename : Path.Join(zDirThe, tempFilename);
            using (var realOutputFile = File.Create(zipFilename))
            {
                realOutputFile.Write(new byte[] { 0, 0});
                using (ZipOutputStream zs = new ZipOutputStream(
                    File.Create(tempPathname)))
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
                        if (File.Exists(tempPathname))
                        {
                            File.Delete(tempPathname);
                        }
                        break;
                    case 1:
                        WriteConsole("One file is stored.");
                        break;
                    default:
                        WriteConsole($"{countAdd} files are stored.");
                        break;
                }
            }
            var temp2Filename = Path.GetRandomFileName() + ".tmp";
            var temp2Pathname = (string.IsNullOrEmpty(zDirThe))
                ? temp2Filename : Path.Join(zDirThe, temp2Filename);
            switch (File.Exists(zipFilename), File.Exists(tempPathname))
            {
                case (true, true):
                    try
                    {
                        var infoZip9 = new FileInfo(zipFilename);
                        infoZip9.MoveTo(temp2Pathname);
                    }
                    catch (Exception ee)
                    {
                        Console.Error.WriteLine($"{ee.Message}");
                        Console.Error.WriteLine(
                            $" Failed to rename from empty '{zipFilename}'");
                    }

                    try
                    {
                        var infoZip8 = new FileInfo(tempPathname);
                        infoZip8.MoveTo(zipFilename);
                    }
                    catch (Exception ee)
                    {
                        Console.Error.WriteLine($"{ee.Message}");
                        Console.Error.WriteLine(
                            $"Failed to rename to real '{zipFilename}'");
                    }

                    try
                    {
                        if (File.Exists(temp2Pathname))
                        {
                            File.Delete(temp2Pathname);
                        }
                    }
                    catch
                    {
                    }

                    break;

                case (false, true):
                    try
                    {
                        var infoZip7 = new FileInfo(tempPathname);
                        infoZip7.MoveTo(zipFilename);
                    }
                    catch (Exception ee)
                    {
                        Console.Error.WriteLine($"{ee.Message}");
                        Console.Error.WriteLine(
                            $"Failed to rename to '{zipFilename}'");
                    }
                    break;
                default:
                    Console.Error.WriteLine("Failed by some unknown error!");
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

                var sizeThe = (new FileInfo(filename)).Length;
                var entry = new ZipEntry(Helper.ToStandardDirSep(filename))
                {
                    DateTime = File.GetLastWriteTime(filename),
                    Size = sizeThe,
                };

                long writtenSize = 0L;
                zs.PutNextEntry(entry);
                byte[] buffer = new byte[32 * 1024];
                using (FileStream fs = File.OpenRead(filename))
                {
                    try
                    {
                        for (int readSize = fs.Read(buffer, 0, buffer.Length);
                            readSize > 0;
                            readSize = fs.Read(buffer, 0, buffer.Length))
                        {
                            zs.Write(buffer, 0, readSize);
                            writtenSize += readSize;
                        }
                    }
                    catch (Exception ee)
                    {
                        WriteConsole(ee.Message);
                        WriteConsole($" wantSize:{sizeThe} but realSize:{writtenSize}");
                    }
                }
                zs.CloseEntry();
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
            return SayHelp(nameof(create), opts
                , OptionShortCuts
                , SwitchShortCuts
                , zipFileHint:"NEWZIPFILE"
                );
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
