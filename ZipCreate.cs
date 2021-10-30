namespace zip2.create
{
    internal class Command : CommandBase
    {
        public override int Invoke()
        {
            Console.WriteLine($"[dbg] file='{zipFilename}'");
            Console.WriteLine($"[dbg] quiet:'{(Quiet ? "Yes" : "No")}'");
            Console.WriteLine("TODO: create.Invoke()");
            return 0;
        }

        public override bool Parse(IEnumerable<string> args)
        {
            var (optUnknown, filenameToBeBackup) = opts.ParseFrom(args);

            if (optUnknown.Length > 0)
            {
                throw new InvalidValueException(
                    optUnknown[0], nameof(list));
            }

            // [TODO] check 'FilesFrom' also
            if (filenameToBeBackup.Length < 1)
            {
                throw new ArgumentException("No file to be backup.");
            }
            Console.WriteLine($"[dbg] files-from '{FilesFrom}'");
            foreach (var arg in filenameToBeBackup)
            {
                Console.WriteLine($"[dbg] backup '{arg}'");
            }

            return true;
        }

        public override int SayHelp()
        {
            return base.SayHelp(nameof(create), opts
                , "NEWZIPFILE");
        }

        static IParser[] opts =
        {
            Quiet,
            FilesFrom,
        };
    }
}
