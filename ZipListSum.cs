using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace zip2
{
    internal class ZipEntrySum
    {
        public bool AnyCrypted { get; private set; } = false;
        protected readonly HashSet<string> CrcList = new();
        public string CrcText()
        {
            var crcGrand = string.Join("|",
                CrcList.OrderBy((it) => it).ToArray());
            if (string.IsNullOrEmpty(crcGrand))
            { // ...... 12345678
                return "00000000";
            }

            var crc32 = new ICSharpCode.SharpZipLib.Checksum.Crc32();
            crc32.Reset();
            crc32.Update(Encoding.ASCII.GetBytes(crcGrand));
            return crc32.Value.ToString("X08");
        }
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

        static Action<HashSet<string>, long> AddCrc
        { get; set; } = (_, _) => { };

        public static void EnableAddCrc()
        {
            AddCrc = (setThe, crcThe) =>
            {
                var crcThis = crcThe.ToString("X08");
                setThe.Add(crcThis);
            };
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
            AddCrc(CrcList, arg.Crc);
            return this;
        }

        public ZipEntrySum AddWith(ZipEntrySum arg)
        {
            Count += arg.Count;
            Size += arg.Size;
            CompressedSize += arg.CompressedSize;
            if (DateTime > arg.DateTime)
                DateTime = arg.DateTime;
            if (DateTimeLast < arg.DateTime)
                DateTimeLast = arg.DateTime;
            if (arg.AnyCrypted) AnyCrypted = true;
            foreach (var crc in arg.CrcList)
                CrcList.Add(crc);
            return this;
        }
    }

    static internal class SomeExtensions
    {
        static public IEnumerable<ZipEntry>
        GetZipEntries( this ZipFile arg)
        {
            foreach (ZipEntry item in arg)
                yield return item;
        }

        static public IEnumerable<ZipEntry> Invoke(
            this IEnumerable<ZipEntry> seq,
            Func<IEnumerable<ZipEntry>,IEnumerable<ZipEntry>> func)
        {
            return func(seq);
        }

        static public IEnumerable<ZipEntrySum> Invoke(
            this IEnumerable<ZipEntrySum> seq,
            Func<IEnumerable<ZipEntrySum>,IEnumerable<ZipEntrySum>> func)
        {
            return func(seq);
        }
    }
}
