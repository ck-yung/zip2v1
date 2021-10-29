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
            return $"{compressed,8} ";
        }

        public static string CrcText(long crc)
        {
            return crc.ToString("X08")+ " ";
        }

        public static string CrcTotalText()
        { // ...... 123456789
            return "         ";
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
            Console.WriteLine("TODO: list.Parse()");
            foreach (var arg in args)
            {
                Console.WriteLine($"\t{arg}");
            }
            return true;
        }

        public override int SayHelp()
        {
            Console.WriteLine("TODO: list.SayHelp()");
            return 0;
        }
    }
}
