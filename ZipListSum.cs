using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace zip2
{
    internal class ZipEntrySum
    {
        public bool AnyCrypted { get; private set; } = false;

        public int Count { get; private set; } = 0;
        public long Size { get; private set; } = 0L;
        public long CompressedSize { get; private set; } = 0L;
        public DateTime DateTime { get; private set; }
        = DateTime.MaxValue;
        public DateTime DateTimeLast { get; private set; }
        = DateTime.MinValue;
        public readonly string Name;

        public ZipEntrySum(string name)
        {
            Name = name;
        }

        public ZipEntrySum AddWith(ZipEntry arg)
        {
            Count += 1;
            Size += arg.Size;
            CompressedSize += arg.CompressedSize;
            if (DateTime > arg.DateTime)
                DateTime = arg.DateTime;
            if (DateTimeLast < arg.DateTime)
                DateTimeLast = arg.DateTime;
            if (arg.IsCrypted) AnyCrypted = true;
            return this;
        }

        public override string ToString()
        {
            StringBuilder tmp = new(list.Feature.RatioText(
                Size,CompressedSize));

            tmp.Append(list.Feature.CompressedText(
                CompressedSize));

            tmp.Append(list.Feature.SizeText(Size));

            tmp.Append(list.Feature.CrcTotalText());

            var dateText = list.Feature.DateText(DateTime);
            if (!string.IsNullOrEmpty(dateText))
            {
                dateText = $"{dateText}- {list.Feature.DateText(DateTimeLast)}";
            }
            tmp.Append(dateText);

            tmp.Append(list.Feature.CountText(Count));
            tmp.Append(list.Feature.CryptedMask(AnyCrypted));
            tmp.Append(Name);
            return tmp.ToString();
        }
    }
}
