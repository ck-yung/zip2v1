using ICSharpCode.SharpZipLib.Zip;

namespace zip2.list
{
    internal class Command : CommandBase
    {
        string ToRatioText(long originalSize, long compressedSize)
        {
            if (originalSize < 1) return " 0 ";
            if (compressedSize >= originalSize) return "-0 ";
            compressedSize = originalSize - compressedSize;
            compressedSize *= 100;
            compressedSize /= originalSize;
            if (compressedSize < 1L) return " 0 ";
            if (compressedSize > 98L) return "99 ";
            return string.Format("{0,2} ", compressedSize);
        }

        public override int Invoke()
        {
            int cntFound = 0;
            long totalSize = 0L;
            long totalCompressedSize = 0L;
            // ..................... 12345678-
            string CrcInTotalLine = "         ";
            bool anyCrypted = false;
            DateTime MostEarlyDate = DateTime.MaxValue;
            DateTime MostLastDate = DateTime.MinValue;
            using var fs = File.OpenRead(zipFilename);
            var zipThe = new ZipFile(fs);
            foreach (ZipEntry itm in zipThe)
            {
                Console.Write(ToRatioText(itm.Size, itm.CompressedSize));
                Console.Write(string.Format("{0,12} ", itm.Size));
                Console.Write(string.Format("{0,12} ", itm.CompressedSize));
                Console.Write(itm.Crc.ToString("X08")+ " ");
                Console.Write(itm.DateTime.ToString("yyyy-MM-dd HH:mm "));
                Console.Write(itm.IsCrypted ? "*" : " ");
                Console.WriteLine(itm.Name);
                cntFound += 1;
                totalSize += itm.Size;
                totalCompressedSize = +itm.CompressedSize;
                if (MostEarlyDate > itm.DateTime) MostEarlyDate = itm.DateTime;
                if (MostLastDate < itm.DateTime) MostLastDate = itm.DateTime;
                if (itm.IsCrypted) anyCrypted = true;
            }
            Console.Write(ToRatioText(totalSize, totalCompressedSize));
            Console.Write(string.Format("{0,12} ", totalSize));
            Console.Write(string.Format("{0,12} ", totalCompressedSize));
            Console.Write(CrcInTotalLine);
            Console.Write(MostEarlyDate.ToString("yyyy-MM-dd HH:mm "));
            Console.Write("- ");
            Console.Write(MostLastDate.ToString("yyyy-MM-dd HH:mm "));
            Console.Write(anyCrypted ? "*" : " ");
            Console.Write($"(#file={cntFound}) ");
            Console.WriteLine(Path.GetFileName(zipFilename));
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
