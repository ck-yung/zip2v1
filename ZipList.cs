using System.Collections.Immutable;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace zip2.list
{
    internal static class Feature
    {
        internal static string ToConsoleText(this ZipEntry arg)
        {
            StringBuilder tmp = new(Opt.Hide.RatioText(
                Feature.RatioText(arg.Size,arg.CompressedSize)));
            tmp.Append(Opt.Hide.SizeText(
                Command.SizeFormat.Invoke(arg.Size)));
            tmp.Append(Opt.Show.CompressedText(
                Command.SizeFormat.Invoke(arg.CompressedSize)));
            tmp.Append(Opt.Show.Crc(arg.Crc));
            tmp.Append(Opt.Hide.DateText(
                Command.DateFormat.Invoke(arg.DateTime)));
            tmp.Append(Opt.Hide.CryptedMarkText(arg.IsCrypted));
            tmp.Append(arg.Name);
            tmp.Append(Environment.NewLine);
            return Opt.Total.ItemText(tmp.ToString());
        }

        static internal string ToConsoleText(this ZipEntrySum arg)
        {
            StringBuilder tmp = new(Opt.Hide.RatioText(
                Feature.RatioText(arg.Size, arg.CompressedSize)));

            tmp.Append(Opt.Hide.SizeText(
                Command.SizeFormat.Invoke(arg.Size)));

            tmp.Append(Opt.Show.CompressedText(
                Command.SizeFormat.Invoke(arg.CompressedSize)));

            tmp.Append(Opt.Show.CrcTotal());

            var dateText = Opt.Hide.DateText(
                Command.DateFormat.Invoke(arg.DateTime));
            if (!string.IsNullOrEmpty(dateText))
            {
                tmp.Append(dateText);
                tmp.Append("- ");
                tmp.Append(Opt.Hide.DateText(
                    Command.DateFormat.Invoke(arg.DateTimeLast)));
            }

            tmp.Append(Opt.Hide.CountText(
                Command.CountFormat.Invoke(arg.Count)));
            tmp.Append(Opt.Hide.CryptedMarkText(arg.AnyCrypted));
            tmp.Append(arg.Name);
            tmp.Append(Environment.NewLine);
            return Opt.Total.GrandText(tmp.ToString());
        }

        internal static string RatioText(
            long original, long compressed)
        {
            if (original < compressed) return Command.NegativeZeroText;
            if (original < 1) return " 0 ";
            compressed = original - compressed;
            compressed *= 100;
            compressed /= original;
            if (compressed < 1L) return " 0 ";
            if (compressed > 98L) return "99 ";
            return $"{compressed,2} ";
        }
    }

    internal class Command : CommandBase
    {
        static readonly public string ExclFilePrefix = "--excl-file=";
        static readonly public string ExclDirPrefix = "--excl-dir=";

        static ImmutableDictionary<string, string> OptionShortCuts =
            new Dictionary<string, string>
            {
                ["-o"] = "--sort=",
                ["-T"] = FilesFromPrefix,
                ["-x"] = ExclFilePrefix,
                ["-X"] = ExclDirPrefix,
            }.ToImmutableDictionary<string,string>();

        static ImmutableDictionary<string, string[]> SwitchShortCuts =
            new Dictionary<string, string[]>
            {
                ["-b"] = new string[]
                {
                    "--hide=ratio,size,date,crypted,count",
                    "--total=off"
                },
                ["-t"] = new string[] { "--total=only" },
            }.ToImmutableDictionary<string, string[]>();

        static protected Func<ZipFile, IEnumerable<ZipEntry>> MyGetZipEntires
        { get; set; } = (zs) => zs.GetZipEntries();

        public override int Invoke()
        {
            var sum = SumUp.Invoke(zipFilename);
            Console.Write(sum.ToConsoleText());
            return 0;
        }

        static protected Func<string, bool> NameFilter { get; set; }
            = Helper.StringFilterAlwaysTrue;

        protected Func<string,bool> ToNameAnyMatchFilter(
            IEnumerable<string> args)
        {
            var argsList = args.ToList();
            if (!argsList.Any())
            {
                return Helper.StringFilterAlwaysTrue;
            }

            var regexs = argsList
                .Select((it) => it.ToDosRegex())
                .ToList();

            args = args
                .Select((it) => it.Replace("\\", "/"))
                .ToArray();

            Func<string, bool> filterToFullPath =
                (arg) => args.Any((it)
                => it.Equals(arg,StringComparison.InvariantCultureIgnoreCase));

            Func<string, bool> filterToFilename =
                (arg) =>
                {
                    var filename = Path.GetFileName(arg);
                    return regexs.Any((it)
                        => it.Match(filename).Success);
                };

            return (arg)
                => filterToFullPath(arg)
                || filterToFilename(arg);
        }

        static protected Func<string, bool> ExclNameFilter { get; set;}
            = Helper.StringFilterAlwaysFalse;

        static protected Func<string, bool> ExclDirFilter { get; set;}
            = Helper.StringFilterAlwaysFalse;

        protected Func<string,bool> ToDirPartAnyMatch(string[] args)
        {
            var regexs = args
                .Select((it) => it.ToDosRegex())
                .ToArray();

            Func<string, bool> filterToDirParts =
                (arg) =>
                {
                    var dirParts = Path.GetDirectoryName(
                        arg)?.Split('/','\\')
                        ?? Array.Empty<string>();
                    return regexs.Any((it)
                    => dirParts.Any((part)
                    => it.Match(part).Success));
                };

            return (arg) => filterToDirParts(arg);
        }

        static IEnumerable<string> ReadConsoleLines()
        {
            var reader = new StreamReader(
                Console.OpenStandardInput());
            while (true)
            {
                var inpLine = reader.ReadLine();
                if (inpLine == null) break;
                yield return inpLine;
            }
        }

        static IEnumerable<string> ReadFileLines(string filename)
        {
            using var reader = File.OpenText(filename);
            while (true)
            {
                var inpLine = reader.ReadLine();
                if (inpLine == null) break;
                yield return inpLine;
            }
        }

        public virtual bool ParseFilesForm()
        {
            if (string.IsNullOrEmpty(FilesFrom)) return true;

            if (NameFilter != Helper.StringFilterAlwaysTrue)
            {
                Console.Write($"'{FilesFromPrefix}' and command-line");
                Console.WriteLine(" FILE cannot both be assigned.");
                return false;
            }

            if (ExclNameFilter != Helper.StringFilterAlwaysFalse)
            {
                Console.Write($"'{FilesFromPrefix}' and command-line");
                Console.WriteLine($" {ExclFilePrefix} cannot both be assigned.");
                return false;
            }

            if (ExclDirFilter != Helper.StringFilterAlwaysFalse)
            {
                Console.Write($"'{FilesFromPrefix}' and command-line");
                Console.WriteLine($" {ExclDirPrefix} cannot both be assigned.");
                return false;
            }

            if (FilesFrom == "-")
            {
                MyGetZipEntires = (zs) =>
                {
                    return ReadConsoleLines()
                    .Select((it) =>
                    zs.FindEntry(it, ignoreCase: true))
                    .Where((it) => it >= 0)
                    .Select((it) => zs[it]);
                };
                return true;
            }

            MyGetZipEntires = (zs) =>
            {
                return ReadFileLines(FilesFrom)
                .Select((it) =>
                zs.FindEntry(it, ignoreCase: true))
                .Where((it) => it >= 0)
                .Select((it) => zs[it]);
            };

            return true;
        }

        public override bool Parse(IEnumerable<string> args)
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
                .Where((it) => it.Length>0)
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
                .Where((it) => it.Length>0)
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

        public override int SayHelp()
        {
            return SayHelp(nameof(list), opts,
                OptionShortCuts, SwitchShortCuts,
                optionalAction:() =>
                {
                    Console.Write($"  {ExclFilePrefix,19}");
                    Console.WriteLine("FILENAME[,FILEWILD ..]");
                    Console.Write($"  {ExclDirPrefix,19}");
                    Console.WriteLine("DIRNAME[,DORWILD] ..]");
                });
        }

        static internal ParameterFunction<long, string> SizeFormat =
            new ParameterFunctionSetter<long, string>(
            option: "size-format", "normal|comma",
            defaultValue: (size) =>
            {
                char[] units = new char[] { ' ', 'k', 'm' };
                foreach (var unit in units)
                {
                    if (size < 10000L)
                    {
                        return string.Format("{0,4}{1} ", size, unit);
                    }
                    size /= 1024L;
                }
                return string.Format("{0,4}g ", size);
            },
            parse: (val, obj) =>
            {
                switch (val)
                {
                    case "comma":
                        return obj.SetValue((size) =>
                        string.Format("{0,18:#,#} ", size));
                    case "normal":
                        return obj.SetValue((arg) =>
                        string.Format("{0,9} ", arg));
                    default:
                        return false;
                }
            });

        static internal ParameterSwitch CountComma =
            new ParameterSwitch("count-comma", whenSwitch: () =>
             {
                 ((IParser)(CountFormat!)).Parse("5");
             });

        static internal ParameterFunction<int, string> CountFormat =
            new ParameterFunctionSetter<int, string>(
            option: "count-width", "NUMBER",
            defaultValue: (count) => $"{count,4} ",
            parse: (val, obj) =>
            {
                if (int.TryParse(val, out int widthThe))
                {
                    var fmt = $"{{0,{widthThe}}} ";
                    if (CountComma)
                    {
                        fmt = $"{{0,{widthThe}:N0}} ";
                    }
                    obj.SetValue((it) => String.Format(fmt,it));
                    return true;
                }
                return false;
            });

        static internal ParameterFunction<DateTime, string> DateFormat =
            new ParameterFunctionSetter<DateTime, string>(
            option: "date-format", "long|short",
            defaultValue: (date) => date.ToString("yy-MM-dd HH:mm "),
            parse: (val, obj) =>
            {
                switch (val)
                {
                    case "long":
                        return obj.SetValue((date) =>
                        date.ToString("yyyy-MM-dd HH:mm:ss "));
                    case "short":
                        return obj.SetValue((date) =>
                        date.ToString("yy-MM-dd "));
                    default:
                        return false;
                }
            });

        static private ZipEntrySum sumDefaultInvoke(string filename)
        {
            return MyGetZipEntires(new ZipFile(
                File.OpenRead(filename)))
            .Where((it) => NameFilter.Invoke(it.Name))
            .Where((it) => ! ExclNameFilter.Invoke(it.Name))
            .Where((it) => ! ExclDirFilter.Invoke(it.Name))
            .Invoke((seq) => Sort.Invoke(seq))
            .Invoke((seq) => ReverseEntry(seq))
            .Select((itm) =>
            {
                Console.Write(itm.ToConsoleText());
                return itm;
            })
            .Aggregate( new ZipEntrySum( Path.GetFileName(
                filename)),
            (acc,itm) => acc.AddWith(itm));
        }

        static private ParameterFunction<string,ZipEntrySum> SumUp =
            new ParameterFunctionSetter<string,ZipEntrySum>(
                option:"sum", help:"ext|dir",
                defaultValue: sumDefaultInvoke,
                parse: (val,opt) =>
                {
                    switch (val)
                    {
                        case "ext":
                            Opt.Show.Crc = (_) => string.Empty;
                            Opt.Show.CrcTotal = () => string.Empty;
                            return opt.SetValue((filename) =>
                            MyGetZipEntires(new ZipFile(
                                File.OpenRead(filename)))
                            .Where((it) => NameFilter.Invoke(it.Name))
                            .Where((it) => ! ExclNameFilter.Invoke(it.Name))
                            .Where((it) => ! ExclDirFilter.Invoke(it.Name))
                            .GroupBy((item) => Path.GetExtension(item.Name))
                            .Select((grp) => grp.Aggregate(
                                new ZipEntrySum(
                                    string.IsNullOrEmpty(grp.Key)
                                    ? "*NoExt*" : grp.Key),
                                    (ZipEntrySum acc, ZipEntry itm) =>
                                    acc.AddWith(itm)))
                            .Invoke((seq) => SortSum!.Invoke(seq))
                            .Invoke((seq) => Reverse!.Invoke(seq))
                            .Select((grp) =>
                            {
                                Console.Write(
                                    Opt.Total.ItemText(grp.ToConsoleText()));
                                return grp;
                            })
                            .Aggregate(new ZipEntrySum(
                                Path.GetFileName(filename)),
                                (acc, itm) => acc.AddWith(itm)));
                        case "dir":
                            Opt.Show.Crc = (_) => string.Empty;
                            Opt.Show.CrcTotal = () => string.Empty;
                            return opt.SetValue((filename) =>
                            MyGetZipEntires(new ZipFile(
                                File.OpenRead(filename)))
                            .Where((it) => NameFilter.Invoke(it.Name))
                            .Where((it) => ! ExclNameFilter.Invoke(it.Name))
                            .Where((it) => ! ExclDirFilter.Invoke(it.Name))
                            .GroupBy((item) => item.Name.GetRootDirectory())
                            .Select((grp) => grp.Aggregate(
                                new ZipEntrySum(
                                    string.IsNullOrEmpty(grp.Key)
                                    ? "*NoExt*" : grp.Key),
                                    (ZipEntrySum acc, ZipEntry itm) =>
                                    acc.AddWith(itm)))
                            .Invoke((seq) => SortSum!.Invoke(seq))
                            .Invoke((seq) => Reverse!.Invoke(seq))
                            .Select((grp) =>
                            {
                                Console.Write(
                                    Opt.Total.ItemText(grp.ToConsoleText()));
                                return grp;
                            })
                            .Aggregate(new ZipEntrySum(
                                Path.GetFileName(filename)),
                                (acc, itm) => acc.AddWith(itm)));
                        default:
                            return false;
                    }
                });

        static private
            Func<IEnumerable<ZipEntrySum>,IEnumerable<ZipEntrySum>>
            SortSum = Seq<ZipEntrySum>.NoChange;

        static private ParameterFunction<
        IEnumerable<ZipEntry>,IEnumerable<ZipEntry>>
        Sort = new ParameterFunctionSetter<
        IEnumerable<ZipEntry>,IEnumerable<ZipEntry>>( option:"sort",
            help:"path|name|ext|size|date|last|count|ratio|name,size|ext,size"
            +"|name,date|ext,date",
            defaultValue: Seq<ZipEntry>.NoChange,
                parse: (val,opt) =>
                {
                    switch (val)
                    {
                        case "path":
                            opt.SetValue(
                                (seq) => seq.OrderBy((it) => it.Name));
                            SortSum =
                                (seq) => seq.OrderBy((it) => it.Name);
                            return true;
                        case "name":
                            opt.SetValue(
                                (seq) => seq
                                .OrderBy((it) => Path.GetFileName(it.Name))
                                .ThenBy((it) => it.Name));
                            SortSum =
                                (seq) => seq.OrderBy((it) => it.Name);
                            return true;
                        case "ext":
                            opt.SetValue(
                                (seq) => seq
                                .OrderBy((it) => Path.GetExtension(it.Name))
                                .ThenBy((it) => it.Name));
                            return true;
                        case "name,size":
                            opt.SetValue(
                                (seq) => seq
                                .OrderBy((it) => Path.GetFileName(it.Name))
                                .ThenBy((it) => it.Size));
                            SortSum =
                                (seq) => seq.OrderBy((it) => it.Name);
                            return true;
                        case "ext,size":
                            opt.SetValue(
                                (seq) => seq
                                .OrderBy((it) => Path.GetExtension(it.Name))
                                .ThenBy((it) => it.Size));
                            return true;
                        case "name,date":
                            opt.SetValue(
                                (seq) => seq
                                .OrderBy((it) => Path.GetFileName(it.Name))
                                .ThenBy((it) => it.DateTime));
                            SortSum =
                                (seq) => seq.OrderBy((it) => it.Name);
                            return true;
                        case "ext,date":
                            opt.SetValue(
                                (seq) => seq
                                .OrderBy((it) => Path.GetExtension(it.Name))
                                .ThenBy((it) => it.DateTime));
                            return true;
                        case "size":
                            opt.SetValue(
                                (seq) => seq.OrderBy((it) => it.Size));
                            SortSum =
                                (seq) => seq.OrderBy((it) => it.Size);
                            return true;
                        case "date":
                            opt.SetValue(
                                (seq) => seq.OrderBy((it) => it.DateTime));
                            SortSum =
                                (seq) => seq.OrderBy((it) => it.DateTime);
                            return true;
                        case "last":
                            SortSum =
                                (seq) => seq.OrderBy((it) => it.DateTimeLast);
                            return true;
                        case "count":
                            SortSum =
                                (seq) => seq.OrderBy((it) => it.Count);
                            return true;
                        case "ratio":
                            opt.SetValue(
                                (seq) => seq.OrderBy((it) =>
                                Feature.RatioText(it.Size,it.CompressedSize)));
                            SortSum =
                                (seq) => seq.OrderBy((it) =>
                                Feature.RatioText(it.Size,it.CompressedSize));
                            return true;
                        default:
                            return false;
                    }
                });

        static Func<IEnumerable<ZipEntry>,IEnumerable<ZipEntry>>
            ReverseEntry = Seq<ZipEntry>.NoChange;
        static readonly ParameterFunctionSwitch<
            IEnumerable<ZipEntrySum>, IEnumerable<ZipEntrySum>> Reverse =
            new ParameterFunctionSwitch<
                IEnumerable<ZipEntrySum>, IEnumerable<ZipEntrySum>>(
            "reverse", help:string.Empty,
            defaultValue: Seq<ZipEntrySum>.NoChange,
            altValue: (seq) => seq.Reverse(),
            whenSwitch: () =>
            {
                ReverseEntry = (seq) => seq.Reverse();
            });

        static public string NegativeZeroText { get; private set;} = " 0 ";

        static readonly ParameterSwitch NegativeZero =
        new ParameterSwitch("negative-zero",
        help: "show ratio '-0' if \"original<compress\"",
        whenSwitch: () =>{ NegativeZeroText = "-0 ";} );

        static private IParser[] opts = new IParser[] {
            Opt.Show,
            Opt.Hide,
            Opt.Total,
            (IParser) SizeFormat,
            (IParser) DateFormat,
            (IParser) CountComma,
            (IParser) CountFormat,
            (IParser) NegativeZero,
            (IParser) Sort,
            (IParser) SumUp,
            (IParser) Reverse,
            (IParser) FilesFrom,
            };
    }

    sealed class Opt: ParameterOptionSetter<int>
    {
        static public readonly Opt Show = new Opt();
        public Func<long, string> Crc { get; set; }
        = (_) => string.Empty;
        public Func<string> CrcTotal { get; set; }
        = () => string.Empty;
        public Func<string,string> CompressedText { get; private set;}
        = (_) => string.Empty;

        private Opt() : base("show", "crc,compress",
            defaultValue: 0,
            parseMany: (values) => {
                foreach (var arg in values)
                {
                    switch (arg)
                    {
                        case "crc":
                            Show.Crc = (arg) => arg.ToString("X08") + " ";
                            // ................... 123456789
                            Show.CrcTotal = () => "         ";
                            break;
                        case "compress":
                            Show.CompressedText = (arg) => arg;
                            break;
                        default:
                            return false;
                    }
                };
                return true;
            })
        {
        }

        override public IEnumerable<string> ToValues(string arg)
        {
            foreach (var token in arg.Substring(Name().Length + 3).Split(','))
            {
                yield return token;
            }
        }

        internal class HideClass: ParameterOptionSetter<int>
        {
            public Func<string,string> RatioText { get; private set;}
            = (it) => it;
            public Func<string,string> SizeText { get; private set;}
            = (it) => it;
            public Func<string,string> DateText { get; private set;}
            = (it) => it;
            public Func<bool,string> CryptedMarkText { get; private set;}
            = (it) => it ? "*" : " ";
            public Func<string,string> CountText { get; private set;}
            = (it) => it;

            internal HideClass() : base("hide",
            "ratio,size,date,crypted,count",
            defaultValue: 0,
            parseMany: (values) =>
            {
                foreach (var arg in values)
                {
                    switch (arg)
                    {
                        case "ratio":
                            Hide.RatioText = (_) => string.Empty;
                            break;
                        case "size":
                            Hide.SizeText = (_) => string.Empty;
                            break;
                        case "date":
                            Hide.DateText = (_) => string.Empty;
                            break;
                        case "crypted":
                            Hide.CryptedMarkText = (_) => string.Empty;
                            break;
                        case "count":
                            Hide.CountText = (_) => string.Empty;
                            break;
                        default:
                            return false;
                    }
                };
                return true;
            })
            {
            }

            override public IEnumerable<string> ToValues(string arg)
            {
                foreach (var token in arg[
                    (Name().Length + 3)..].Split(','))
                {
                    yield return token;
                }
            }
        }

        static internal HideClass Hide = new();

        internal class TotalClass: ParameterOptionSetter<bool>
        {
            public Func<string,string> ItemText { get; private set;}
            = (it) => it;
            public Func<string,string> GrandText { get; private set;}
            = (it) => it;

            internal TotalClass() : base("total", "only|off",
            defaultValue: false,
            parse: (value, obj) => {
                switch (value)
                {
                    case "only":
                        Total.ItemText = (_) => string.Empty;
                        Total.GrandText = (it) => it;
                        return true;
                    case "off":
                        Total.ItemText = (it) => it;
                        Total.GrandText = (_) => string.Empty;
                        return true;
                    default:
                        return false;
                }
            })
            {
            }

            override public IEnumerable<string> ToValues(string arg)
            {
                foreach (var token in arg.Substring(Name().Length + 3).Split(','))
                {
                    yield return token;
                }
            }
        }

        static internal TotalClass Total = new();
    }
}
