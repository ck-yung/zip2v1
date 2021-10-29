using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace zip2.list
{
    public static class Feature
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
                Feature.DateText(arg.DateTime)));
            tmp.Append(Opt.Hide.CryptedMarkText(
                CryptedMask(arg.IsCrypted)));
            tmp.Append(arg.Name);
            return tmp.ToString();
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

            var dateText = Opt.Hide.DateText(Feature.DateText(arg.DateTime));
            if (!string.IsNullOrEmpty(dateText))
            {
                dateText = $"{dateText}- {Opt.Hide.DateText(Feature.DateText(arg.DateTimeLast))}";
                tmp.Append(dateText);
            }

            tmp.Append(Opt.Hide.CountText(Feature.CountText(arg.Count)));
            tmp.Append(Opt.Hide.CryptedMarkText(Feature.CryptedMask(arg.AnyCrypted)));
            tmp.Append(arg.Name);
            return tmp.ToString();
        }

        public static string RatioText(
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

        public static string CryptedMask(bool crypted)
        {
            return crypted ? "*" : " ";
        }

        public static string DateText(DateTime datetime)
        {
            return datetime.ToString("yyyy-MM-dd HH:mm:ss ");
        }

        public static string CountText(int count)
        {
            return $"{count,4} ";
        }
    }

    internal class Command : CommandBase
    {
        public override int Invoke()
        {
            ZipEntrySum sum = new(Path.GetFileName(zipFilename));
            using var fs = File.OpenRead(zipFilename);
            var zipThe = new ZipFile(fs);
            foreach (ZipEntry itm in zipThe)
            {
                Console.WriteLine(itm.ToConsoleText());
                sum.AddWith(itm);
            }
            Console.WriteLine(sum.ToConsoleText());
            return 0;
        }

        public override bool Parse(IEnumerable<string> args)
        {
            (string[] founds, var others) = args
            .SubractStartsWith(Opt.Show.IsPrefix,
            toValues: (seq) => seq.Select(
                (it) => Opt.Show.ToValues(it))
                .SelectMany((seq2) => seq2));

            if (!founds.All((it) => Opt.Show.Parse(it)))
            {
                return false;
            }

            (string[] founds2, var others2) = others
            .SubractStartsWith(Opt.Hide.IsPrefix,
            toValues: (seq) => seq.Select(
                (it) => Opt.Hide.ToValues(it))
                .SelectMany((seq2) => seq2));

            if (!founds2.All((it) => Opt.Hide.Parse(it)))
            {
                return false;
            }

            (string[] founds3, var others3) = others
            .SubractStartsWith(SizeFormat.IsPrefix,
            toValues: (seq) => seq
            .Select((it) => SizeFormat.ToValues(it))
            .SelectMany((it) => it));

            if (!founds3.All((it) => SizeFormat.Parse(it)))
            {
                return false;
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
                        Console.WriteLine($"'{val}' is unknown to '{obj.Name()}'");
                        return false;
                }
            });

        static IParser[] opts = new IParser[] {
            Opt.Show,
            Opt.Hide,
            (IParser) SizeFormat,
            };
    }

    sealed internal class Opt: ParameterOptionSetter<bool>
    {
        static public readonly Opt Show = new Opt();
        public Func<long, string> Crc { get; private set; }
        = (_) => string.Empty;
        public Func<string> CrcTotal { get; internal set; }
        = () => string.Empty;
        public Func<string,string> CompressedText { get; private set;}
        = (_) => string.Empty;

        private Opt() : base("show", "crc,compress", false,
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
                            $"'{value}' is unknown to '--{obj.Name()}='");
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
            public Func<string,string> CryptedMarkText { get; private set;}
            = (it) => it;
            public Func<string,string> CountText { get; private set;}
            = (it) => it;

            internal HideClass() : base("hide",
            "ratio,size,date,crypted,count", false,
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
                                $"'{value}' is unknown to '--{obj.Name()}='");
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
    }

}
