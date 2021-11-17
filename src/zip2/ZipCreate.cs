using System.Collections.Immutable;
using ICSharpCode.SharpZipLib.Zip;

namespace zip2.create
{
    internal class Command : CommandBase
    {
        public override int Invoke()
        {
            switch (string.IsNullOrEmpty(EncryptPassword),
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
                        ((IParser)EncryptPassword).Parse(textThe);
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
                        ((IParser)EncryptPassword).Parse(textThe);
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

            switch (FilenamesToBeBackup.Count(),
                string.IsNullOrEmpty(FilesFrom))
            {
                case (0, true):
                    TotalPrintLine("No file to be backup.");
                    return 1;
                case ( > 0, false):
                    Console.Write("Cannot handle files from --files-from");
                    Console.WriteLine($"={FilesFrom} and command-line arg FILE.");
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
                        TotalPrintLine(
                            $"No file is found in (files-from) '{FilesFrom}'");
                        return 1;
                    }
                    break;
                default:
                    break;
            }

            if (File.Exists(zipFilename))
            {
                Console.WriteLine($"Output zip file '{zipFilename}' is found!");
                return 1;
            }

            int countAdd = 0;
            var zDirThe = Path.GetDirectoryName(zipFilename);
            var zFilename = Path.GetFileName(zipFilename);
            var zShadowOutputFilename = zFilename + "."
                + Path.GetRandomFileName() + ".tmp";
            var zShadowOutputPathName = (string.IsNullOrEmpty(zDirThe))
                ? zShadowOutputFilename
                : Path.Join(zDirThe, zShadowOutputFilename);
            using (var realOutputFile = File.Create(zipFilename))
            {
                realOutputFile.Write(new byte[] { 19, 97, 7, 1});
                using (ZipOutputStream zs = new ZipOutputStream(
                    File.Create(zShadowOutputPathName)))
                {
                    zs.UseZip64 = UseZip64.Dynamic;
                    zs.SetLevel(CompressLevel);

                    if (!string.IsNullOrEmpty(EncryptPassword))
                    {
                        zs.Password = EncryptPassword;
                    }

                    foreach (var filename in FilenamesToBeBackup)
                    {
                        ItemPrint(filename);
                        countAdd += (AddToZip(filename, zs)) ? 1 : 0;
                        ItemPrint(Environment.NewLine);
                    }

                    zs.Finish();
                    zs.Close();
                }

                var zShadowOutputFileSize =
                    File.Exists(zShadowOutputPathName)
                    ? new FileInfo(zShadowOutputPathName).Length
                    : -1;

                switch (countAdd, zShadowOutputFileSize)
                {
                    case (0,_):
                        TotalPrintLine(" No file is stored.");
                        if (zShadowOutputFileSize >= 0)
                        {
                            File.Delete(zShadowOutputPathName);
                        }
                        break;
                    case (1, >= 100):
                        TotalPrintLine(" One file is stored.");
                        break;
                    case (_, < 100):
                        TotalPrintLine(" Unknown error");
                        if (zShadowOutputFileSize >= 0)
                        {
                            File.Delete(zShadowOutputPathName);
                        }
                        break;
                    default:
                        TotalPrintLine($" {countAdd} files are stored.");
                        break;
                }
            }

            var temp2Filename = Path.GetRandomFileName() + ".tmp";
            var temp2Pathname = (string.IsNullOrEmpty(zDirThe))
                ? temp2Filename : Path.Join(zDirThe, temp2Filename);

            switch (File.Exists(zipFilename),
                File.Exists(zShadowOutputPathName))
            {
                case (false, true):
                    try
                    {
                        new FileInfo(zShadowOutputPathName)
                            .MoveTo(zipFilename);
                    }
                    catch (Exception ee)
                    {
                        Console.Error.WriteLine($"{ee.Message}");
                        Console.Error.WriteLine(
                            $"Failed to rename to '{zipFilename}'");
                    }
                    break;

                case (true, true):
                    try
                    {
                        new FileInfo(zipFilename)
                            .MoveTo(temp2Pathname);
                        new FileInfo(zShadowOutputPathName)
                            .MoveTo(zipFilename);
                    }
                    catch (Exception ee)
                    {
                        Console.Error.WriteLine($"{ee.Message}");
                        Console.Error.WriteLine(
                            $" Failed while rename file '{zipFilename}'");
                    }
                    finally
                    {
                        if (File.Exists(temp2Pathname))
                        {
                            File.Delete(temp2Pathname);
                        }
                    }
                    break;

                case (true, false):
                    if (File.Exists(zipFilename))
                    {
                        File.Delete(zipFilename);
                    }
                    break;

                default:
                    Console.Error.WriteLine(
                        " Failed by some unknown error!");
                    break;
            }

            return 0;
        }

        bool AddToZip(string filename, ZipOutputStream zs)
        {
            try
            {
                if (!File.Exists(filename))
                {
                    ItemPrint(" not found");
                    return false;
                }

                var sizeThe = (new FileInfo(filename)).Length;
                var entry = new ZipEntry(Helper.ToStandardDirSep(filename))
                {
                    DateTime = File.GetLastWriteTime(filename),
                    Size = sizeThe,
                };

                long writtenSize = 0L;
                zs.PutNextEntry(entry, isTranscational:true);
                byte[] buffer = new byte[32 * 1024];
                using (FileStream fs = File.OpenRead(filename))
                {
                    try
                    {
                        for (int readSize = fs.Read(buffer, 0, buffer.Length);
                            readSize > 0 && sizeThe > writtenSize;
                            readSize = fs.Read(buffer, 0, buffer.Length))
                        {
                            zs.Write(buffer, 0, readSize);
                            writtenSize += readSize;
                        }
                    }
                    catch (ZipException zipEe)
                    {
                        ItemErrorPrintFilename(filename);
                        ItemErrorPrintMessage($" {zipEe.Message}");
                    }
                    catch (Exception ee)
                    {
                        ItemErrorPrintFilename(filename);
                        var checkDebug = Environment
                        .GetEnvironmentVariable("zip2");
                        if (checkDebug?.Contains(":debug:") ?? false)
                        {
                            ItemErrorPrintMessage($" {ee.ToString()}");
                        }
                        else
                        {
                            ItemErrorPrintMessage($" {ee.Message}");
                        }
                    }
                }

                if (sizeThe==writtenSize)
                {
                    zs.CloseEntry();
                }
                else
                {
                    ItemErrorPrintFilename(filename);
                    ItemErrorPrintMessage(" is abortd");
                    ItemPrint(
                        $" because want {sizeThe}b but find {writtenSize}b !");
                    zs.Rollback();
                    return false;
                }

                return true;
            }
            catch (Exception ee)
            {
                ItemErrorPrintFilename(filename);
                ItemErrorPrintMessage($" {ee.Message}");
                return false;
            }
        }

        static ImmutableDictionary<string, string[]> SwitchShortCuts =
            new Dictionary<string, string[]>
            {
                [QuietShortcut] = new string[] { QuietText },
                ["-0"] = new string[] { "--compress-level=store" },
                ["-1"] = new string[] { "--compress-level=fast" },
                ["-2"] = new string[] { "--compress-level=good" },
                ["-3"] = new string[] { "--compress-level=better" },
            }.ToImmutableDictionary<string, string[]>();

        static ImmutableDictionary<string, string> OptionShortCuts =
            new Dictionary<string, string>
            {
                ["-T"] = FilesFromPrefix,
            }.ToImmutableDictionary<string, string>();

        List<string> FilenamesToBeBackup = new List<string>();

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
                "store|fast|good|better  (default good)", 5,
                parse: (val,obj) =>
                {
                    switch (val)
                    {
                        case "store":
                            obj.SetValue(0);
                            return true;
                        case "fast":
                            obj.SetValue(2);
                            return true;
                        case "good":
                            obj.SetValue(5);
                            return true;
                        case "better":
                            obj.SetValue(9);
                            return true;
                        default:
                            return false;
                    }
                });

        static public readonly ParameterOption<string> EncryptPassword
            = new ParameterOptionSetter<string>("password",
                help: "PASSWORD, or console input if -",
                defaultValue: string.Empty,
                parse: (val, obj) =>
                {
                    if (string.IsNullOrEmpty(val))
                    {
                        return false;
                    }

                    if (val == "-")
                    {
                        obj.SetValue(Helper
                            .ReadConsolePassword(
                            "password",
                            requireInputCount: 2));
                    }
                    else
                    {
                        obj.SetValue(val);
                    }
                    return true;
                });

        static IParser[] opts =
        {
            Quiet,
            TotalOff,
            FilesFrom,
            CompressLevel,
            EncryptPassword,
            PasswordFrom,
            PasswordFromRaw,
        };
    }
}
