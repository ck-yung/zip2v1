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
    }
}
