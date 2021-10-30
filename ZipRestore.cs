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
            string newDir = NewDir;
            Console.WriteLine($"[dbg] newDir='{newDir}'");

            var a2 = "Good";
            var a3 = NameFilter(a2);
            Console.WriteLine(
                $"[todo] {a2} {(a3 ? "matches" : "does not match")}");
            Console.WriteLine("[todo] Restore.Invoke()");
            return 0;
        }

        public override int SayHelp()
        {
            Console.Write("Syntax: zip2");
            Console.WriteLine($" --{nameof(restore)} --file=ZIPFILE [OPT ..]");
            Console.WriteLine("OPT:");
            foreach (var opt in opts)
            {
                var prefix = $"--{opt.Name()}=";
                if (opt is ISwitch || opt is ParameterSwitch)
                {
                    prefix = $"--{opt.Name()} ";
                }
                Console.WriteLine($"  {prefix,20}{opt.OnlineHelp()}");
            }
            return 0;
        }

        public override bool Parse(
            IEnumerable<string> args)
        {
            foreach (var arg in args)
            {
                Console.WriteLine($"\t'{arg}'");
            }
            var qry9 = opts
                .Aggregate(args, (seq, opt) => opt.Parse(seq))
                .GroupBy((it) => it.StartsWith('-'))
                .ToDictionary((grp) => grp.Key, (grp) => grp.ToArray());

            if (qry9.ContainsKey(true))
            {
                throw new InvalidValueException(
                    qry9[true][0], nameof(list));
            }

            string[] otherArgs = qry9.ContainsKey(false)
                ? qry9[false] : Array.Empty<string>();
            if (otherArgs.Length > 0)
            {
                var regexs = otherArgs
                    .Select((it) => it.ToDosRegex())
                    .ToArray();

                otherArgs = otherArgs
                    .Select((it) => it.Replace("\\", "/"))
                    .ToArray();

                Func<string, bool> filterToFullPath =
                    (arg) => otherArgs.Any((it)
                    => it.Equals(arg));

                Func<string, bool> filterToFilename =
                    (arg) =>
                    {
                        var filename = Path.GetFileName(arg);
                        return regexs.Any((it)
                            => it.Match(filename).Success);
                    };

                NameFilter = (arg)
                    => filterToFullPath(arg)
                    || filterToFilename(arg);
            }
            Console.WriteLine("[todo] Restore.Parse()");
            return true;
        }

        static public ParameterOption<string> NewDir =
            new ParameterOptionSetter<string>(
                "new-dir",
                help: string.Empty,
                defaultValue: string.Empty,
                parse: (val, opt) =>
                {
                    opt.SetValue(val);
                    return true;
                },
                requiredSingleValue: true);

        static IParser[] opts =
        {
            Quiet,
            FilesFrom,
            NewDir,
        };
    }
}
