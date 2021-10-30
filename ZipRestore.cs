namespace zip2.restore
{
    internal class Command : zip2.list.Command
    {
        public Command()
        {
            Console.WriteLine(
                "[dbg] Restore command object is created.");
        }

        public override int Invoke()
        {
            Console.WriteLine($"[dbg] file='{zipFilename}'");
            Console.WriteLine($"[dbg] quiet:'{(Quiet?"Yes":"No")}'");
            Console.WriteLine($"[dbg] newDir='{NewDir}'");

            var a2 = "Good";
            var a3 = NameFilter(a2);
            Console.WriteLine(
                $"[todo] {a2} {(a3 ? "matches" : "does not match")}");
            Console.WriteLine("[todo] Restore.Invoke()");
            return 0;
        }

        public override int SayHelp()
        {
            return base.SayHelp(nameof(restore), opts);
        }

        public override bool Parse(
            IEnumerable<string> args)
        {
            var (optUnknown, otherArgs) = opts.ParseFrom(args);

            if (optUnknown.Length > 0)
            {
                throw new InvalidValueException(
                    optUnknown[0], nameof(list));
            }

            if (otherArgs.Length > 0)
            {
                NameFilter = ToNameFilterFunc(otherArgs);
            }

            return true;
        }

        static public ParameterOptionString NewDir =
            new ParameterOptionString(
                "new-dir", help: "NEW_OUTPUT_DIR",
                defaultValue: string.Empty);

        static IParser[] opts =
        {
            Quiet,
            FilesFrom,
            NewDir,
        };
    }
}
