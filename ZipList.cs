using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace zip2.list
{
    public static class Feature
    {
        internal static string ToConsoleText(this ZipEntry arg)
        {
            StringBuilder tmp = new(RatioText(
                arg.Size,arg.CompressedSize));
            tmp.Append(SizeText(arg.Size));
            tmp.Append(CompressedText(arg.CompressedSize));
            tmp.Append(CrcText(arg.Crc));
            tmp.Append(DateText(arg.DateTime));
            tmp.Append(CryptedMask(arg.IsCrypted));
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

        public static string SizeText(long size)
        {
            return $"{size,8} ";
        }

        public static string CompressedText(long compressed)
        {
            return list.Command.Opt.Show
                .CompressedText($"{compressed,8} ");
        }

        public static string CrcText(long crc)
        {
            return list.Command.Opt.Show.Crc(crc);
        }

        public static string CrcTotalText()
        {
            return list.Command.Opt.Show.CrcTotal();
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
            Console.WriteLine(sum.ToString());
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
                                $"'{value}' is unknown to '--{obj.Name}='");
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
        static IParser[] opts = new IParser[] {
            Opt.Show,
            };
    }
}
