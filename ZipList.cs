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

            tmp.Append(Opt.Hide.CountText(arg.Count));
            tmp.Append(Opt.Hide.CryptedMarkText(arg.AnyCrypted));
            tmp.Append(arg.Name);
            tmp.Append(Environment.NewLine);
            return Opt.Total.GrandText(tmp.ToString());
        }

        internal static string RatioText(
            long original, long compressed)
        {
            if (original < 1) return " 0 ";
            if (compressed >= original) return "-0 ";
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
        public override int Invoke()
        {
            var sum = SumUp.Invoke(zipFilename);
            Console.Write(sum.ToConsoleText());
            return 0;
        }

        public override bool Parse(IEnumerable<string> args)
        {
            IEnumerable<string> argsThe = args;
            foreach (var opt in opts)
            {
                (string[] founds, argsThe) = argsThe
                .SubractStartsWith(opt.IsPrefix,
                toValues: (seq) => seq.Select(
                    (it) => opt.ToValues(it))
                    .SelectMany((seq2) => seq2));

                if (opt.RequireSingleValue()
                && founds.Length>1)
                {
                    Console.WriteLine(
                        $"Too many value to --{opt.Name()}");
                    return false;
                }

                if (!founds.All((it) => opt.Parse(it)))
                {
                    return false;
                }
            }
            return true;
        }

        public override int SayHelp()
        {
            Console.WriteLine("Syntax: zip2 --list --file=ZIPFILE [OPT ..]");
            Console.WriteLine("OPT:");
            foreach (var opt in opts)
            {
                var prefix = $"--{opt.Name()}=";
                Console.WriteLine($"  {prefix,20}{opt.OnlineHelp()}");
            }

            return 0;
        }

        static public ParameterFunction<long, string> SizeFormat =
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
                        Console.WriteLine($"'{val}' is unknown to '--{obj.Name()}'");
                        return false;
                }
            });

        static public ParameterFunction<DateTime, string> DateFormat =
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
                        Console.WriteLine($"'{val}' is unknown to '--{obj.Name()}'");
                        return false;
                }
            });

        static ZipEntrySum sumDefaultInvoke(string filename)
        {
            return (new ZipFile(File.OpenRead(filename)))
            .GetZipEntries()
            .Invoke((seq) => Sort.Invoke(seq))
            .Select((itm) =>
            {
                Console.Write(itm.ToConsoleText());
                return itm;
            })
            .Aggregate( new ZipEntrySum( Path.GetFileName(
                filename)),
            (acc,itm) => acc.AddWith(itm));
        }

        static ParameterFunction<string,ZipEntrySum> SumUp =
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
                            (new ZipFile( File.OpenRead(filename)))
                            .GetZipEntries()
                            .GroupBy((item) => Path.GetExtension(item.Name))
                            .Select((grp) => grp.Aggregate(
                                new ZipEntrySum(
                                    string.IsNullOrEmpty(grp.Key)
                                    ? "*NoExt*" : grp.Key),
                                    (ZipEntrySum acc, ZipEntry itm) =>
                                    acc.AddWith(itm)))
                            .Invoke((seq) => SortSum!.Invoke(seq))
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
                            (new ZipFile( File.OpenRead(filename)))
                            .GetZipEntries()
                                .GroupBy((item) => item.Name.GetRootDirectory())
                                .Select((grp) => grp.Aggregate(
                                    new ZipEntrySum(
                                        string.IsNullOrEmpty(grp.Key)
                                        ? "*NoExt*" : grp.Key),
                                        (ZipEntrySum acc, ZipEntry itm) =>
                                        acc.AddWith(itm)))
                                .Invoke((seq) => SortSum!.Invoke(seq))
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

        static Func<IEnumerable<ZipEntrySum>,IEnumerable<ZipEntrySum>>
            SortSum = Seq<ZipEntrySum>.NoChange;

        static ParameterFunction<
        IEnumerable<ZipEntry>,IEnumerable<ZipEntry>>
        Sort = new ParameterFunctionSetter<
        IEnumerable<ZipEntry>,IEnumerable<ZipEntry>>(
            option:"sort", help:"name|ext|size|date|last|count|ratio",
            defaultValue: Seq<ZipEntry>.NoChange,
                parse: (val,opt) =>
                {
                    switch (val)
                    {
                        case "name":
                            opt.SetValue(
                                (seq) => seq.OrderBy((it) => it.Name));
                            SortSum =
                                (seq) => seq.OrderBy((it) => it.Name);
                            return true;
                        case "ext":
                            opt.SetValue(
                                (seq) => seq
                                .OrderBy((it) => Path.GetExtension(it.Name))
                                .ThenBy((it) => it.Name));
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

        static IParser[] opts = new IParser[] {
            Opt.Show,
            Opt.Hide,
            Opt.Total,
            (IParser) SizeFormat,
            (IParser) DateFormat,
            (IParser) Sort,
            (IParser) SumUp,
            };
    }

    sealed internal class Opt: ParameterOptionSetter<bool>
    {
        static public readonly Opt Show = new Opt();
        public Func<long, string> Crc { get; set; }
        = (_) => string.Empty;
        public Func<string> CrcTotal { get; set; }
        = () => string.Empty;
        public Func<string,string> CompressedText { get; private set;}
        = (_) => string.Empty;

        private Opt() : base("show", "crc,compress", defaultValue: false,
        parse: (value, obj) => {
            switch (value)
            {
                case "crc":
                    Show.Crc = (arg) => arg.ToString("X08") + " ";
                    // ................... 123456789
                    Show.CrcTotal = () => "         ";
                    return true;
                case "compress":
                    Show.CompressedText = (arg) => arg;
                    return true;
                default:
                    Console.WriteLine(
                        $"'{value}' is unknown to '--{obj.Name()}'");
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

        internal class HideClass: ParameterOptionSetter<bool>
        {
            public Func<string,string> RatioText { get; private set;}
            = (it) => it;
            public Func<string,string> SizeText { get; private set;}
            = (it) => it;
            public Func<string,string> DateText { get; private set;}
            = (it) => it;
            public Func<bool,string> CryptedMarkText { get; private set;}
            = (it) => it ? "*" : " ";
            public Func<int,string> CountText { get; private set;}
            = (it) => $"{it,5} ";

            internal HideClass() : base("hide",
            "ratio,size,date,crypted,count",
            defaultValue: false,
            parse: (value, obj) => {
                switch (value)
                {
                    case "ratio":
                        Hide.RatioText = (_) => string.Empty;
                        return true;
                    case "size":
                        Hide.SizeText = (_) => string.Empty;
                        return true;
                    case "date":
                        Hide.DateText = (_) => string.Empty;
                        return true;
                    case "crypted":
                        Hide.CryptedMarkText = (_) => string.Empty;
                        return true;
                    case "count":
                        Hide.CountText = (_) => string.Empty;
                        return true;
                    default:
                        Console.WriteLine(
                            $"'{value}' is unknown to '--{obj.Name()}'");
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
                        Console.WriteLine(
                            $"'{value}' is unknown to '--{obj.Name()}'");
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
