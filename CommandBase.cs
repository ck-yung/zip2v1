using System.Collections.Immutable;

namespace zip2
{
    abstract class CommandBase
    {
        public string zipFilename
        { get; private set; } = string.Empty;

        abstract public bool Parse(IEnumerable<string> args);
        abstract public int Invoke();
        abstract public int SayHelp();

        public (bool,IEnumerable<string>)
        FindSingleFilename(IEnumerable<string> args)
        {
            const string zipFilenamePrefix = "--file=";

            (string[] found, var others) = args
            .SubractStartsWith(zipFilenamePrefix);

            if (found.Length != 1)
            {
                return (false, args);
            }

            var otherArgs = others.ToArray();
            if (otherArgs.Contains("-?") ||
                otherArgs.Contains("--help"))
            {
                return (false, args);
            }

            zipFilename = found[0]
            .Substring(zipFilenamePrefix.Length);

            // Expand tilde ~ to %USERPROFILE%
            if (!File.Exists(zipFilename) &&
                zipFilename.Length > 2 &&
                zipFilename[0] == '~' &&
                zipFilename[1] == Path.DirectorySeparatorChar)
            {
                // windows os - user envir - USERPROFILE
                var homeDir = Environment
                    .GetEnvironmentVariable("USERPROFILE");
                if (homeDir != null && Directory.Exists(homeDir))
                {
                    zipFilename = Path.Combine(homeDir, zipFilename[2..]);
                }
            }

            return (true, others);
        }

        protected bool ExpandZipFilename()
        {
            if (File.Exists(zipFilename)) return true;
            var dirThe = Path.GetDirectoryName(zipFilename);
            if (!Directory.Exists(dirThe)) return false;
            var wildName = Path.GetFileName(zipFilename);
            var filenames = Directory.GetFiles(dirThe, wildName);
            if (filenames.Length == 1)
            {
                zipFilename = filenames[0];
                return true;
            }
            return false;
        }

        static public readonly string QuietText = "--quiet";
        static public readonly string QuietShortcut = "-q";
        static public Action<string> ItemPrint
        { get; private set; } = (it) => Console.Write(it);
        static public Action<string> ItemPrintLine
        { get; private set; } = (it) => Console.WriteLine(it);
        static public readonly ParameterSwitch Quiet =
            new ParameterSwitch("quiet", whenSwitch: () =>
            {
                ItemPrint = (_) => { };
                ItemPrintLine = (_) => { };
            });

        static public readonly ParameterOption<string> Password
            = new ParameterOptionSetter<string>("password",
                help: "PASSWORD, or console input if -",
                defaultValue: string.Empty,
                parse: (val, obj) =>
                {
                    if (string.IsNullOrEmpty(val))
                    {
                        return false;
                    }

                    if (val == "-")
                    {
                        obj.SetValue(Helper
                            .ReadConsolePassword(
                            "password"));
                    }
                    else
                    {
                        obj.SetValue(val);
                    }
                    return true;
                });

        static public readonly ParameterOption<string> PasswordFrom
            = new ParameterOptionSetter<string>("password-from",
                help: "FILENAME",
                defaultValue: string.Empty,
                parse: (val, obj) =>
                {
                    obj.SetValue(val);
                    return true;
                });

        static public readonly string TotalOffText = "--total-off";
        static public Action<string> TotalPrintLine
        { get; private set; } = (it) => Console.WriteLine(it);
        static public readonly ParameterSwitch TotalOff =
            new ParameterSwitch("total-off", whenSwitch: () =>
            {
                TotalPrintLine = (_) => { };
            });

        static public readonly string FilesFromText = "files-from";
        static public readonly string FilesFromPrefix = $"--{FilesFromText}=";
        static public readonly ParameterOptionString FilesFrom =
            new ParameterOptionString(
                FilesFromText, "FILENAME_OF_FILE_LIST  (stdin if -)",
                defaultValue: string.Empty);

        public int SayHelp( string name
            , IParser[] parsers
            , ImmutableDictionary<string, string>? optionShortCuts = null
            , ImmutableDictionary<string, string[]>? switchShortCuts = null
            , Action? optionalAction = null
            , string zipFileHint = "ZIPFILE"
            )
        {
            Console.Write(
                $"Syntax: zip2 --{name} --file={zipFileHint}");
            Console.WriteLine(" [OPT ..] [FILE ..]");
            Console.Write(
                $"Syntax: zip2 -{name[0]}f {zipFileHint}");
            Console.WriteLine(" [OPT ..] [FILE ..]");

            Console.WriteLine("OPT:");
            foreach (var opt in parsers)
            {
                var prefix = $"--{opt.Name()}=";
                if (opt is ISwitch || opt is ParameterSwitch)
                {
                    prefix = $"--{opt.Name()} ";
                }
                Console.Write($"  {prefix,19}");
                if (!string.IsNullOrEmpty(opt.OnlineHelp()))
                {
                    Console.Write(opt.OnlineHelp());
                }
                Console.WriteLine();
            }

            optionalAction?.Invoke();

            Action printShortcutTitle = () =>
            {
                Console.WriteLine("Shortcut:");
            };

            if (optionShortCuts?.Any() ?? false)
            {
                printShortcutTitle();
                printShortcutTitle = () => { };
                foreach (var key in optionShortCuts.Keys
                    .OrderBy((it) => it))
                {
                    Console.WriteLine($"{key,19} ->  {optionShortCuts[key]}");
                }
            }

            if (switchShortCuts?.Any() ?? false)
            {
                printShortcutTitle();
                foreach (var key in switchShortCuts.Keys
                    .OrderBy((it) => it))
                {
                    Console.Write($"{key,19} ->");
                    Console.WriteLine($"  {string.Join("  ", switchShortCuts[key])}");
                }
            }
            Console.Write(@"Please check ""https://github.com/ck-yung/zip2/blob/master/faq/readme.MD""");
            return 0;
        }
    }

    public abstract class ParameterOption<R>: IParser
    {
        protected R _value;
        readonly protected string OptionName;
        readonly protected string Help;
        readonly protected bool _requiredSingleValue;

        protected ParameterOption(string option, string help,
            R defaultValue, bool requiredSingleValue)
        {
            OptionName = option;
            Help = help;
            _value = defaultValue;
            _requiredSingleValue = requiredSingleValue;
        }
        public string Name()
        {
            return OptionName;
        }

        public string OnlineHelp()
        {
            return Help;
        }

        static public implicit operator R(ParameterOption<R> arg)
        {
            return arg._value;
        }

        virtual public IEnumerable<string> ToValues(string arg)
        {
            yield return arg.Substring(OptionName.Length + 3);
        }
        abstract public bool Parse(string value);

        virtual public bool IsPrefix(string arg)
        {
            return arg.StartsWith($"--{OptionName}=");
        }

        public bool RequireSingleValue()
        {
            return _requiredSingleValue;
        }

        public virtual bool ParseMany(IEnumerable<string> values)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class ParameterFunction<T, R> :
        ParameterOption<Func<T,R>>
    {
        public ParameterFunction(string option, string help,
            Func<T,R> defaultValue) :
            base(option, help, defaultValue,
                requiredSingleValue:true)
        { }

        public R Invoke(T arg)
        {
            return ((Func<T, R>)_value)(arg);
        }

        protected Func<IEnumerable<string>, bool> _parseMany
            = IParser.FakeParseMany;

        public override bool ParseMany(IEnumerable<string> values)
        {
            return _parseMany(values);
        }

        public ParameterFunction(string option, string help,
            Func<T, R> defaultValue,
            Func<IEnumerable<string>, bool> parseMany) :
            base(option, help, defaultValue,
                requiredSingleValue: false)
        {
            _parseMany = parseMany;
        }
    }

    internal class ParameterOptionSetter<R> :
        ParameterOption<R>
    {
        public bool SetValue(R newValue)
        {
            _value = newValue;
            return true;
        }

        readonly Func<string, ParameterOptionSetter<R>, bool> _parse;

        public ParameterOptionSetter(string option, string help,
            R defaultValue,
            Func<string, ParameterOptionSetter<R>, bool> parse) :
            base (option, help, defaultValue,
                requiredSingleValue:true)
        {
            _parse = parse;
        }

        override public bool Parse(string arg)
        {
            return _parse(arg,this);
        }

        readonly Func<IEnumerable<string>, bool> _parseMany
            = IParser.FakeParseMany;

        public ParameterOptionSetter(string option, string help,
            R defaultValue,
            Func<IEnumerable<string>, bool> parseMany) :
            base(option, help, defaultValue,
                requiredSingleValue: false)
        {
            _parse = (_, _) => false;
            _parseMany = parseMany;
        }

        override public bool ParseMany(IEnumerable<string> values)
        {
            return _parseMany(values);
        }
    }

    internal class ParameterFunctionSetter<T, R> :
        ParameterFunction<T, R>
    {
        public bool SetValue(Func<T, R> newValue)
        {
            _value = newValue;
            return true;
        }

        public override bool Parse(string value)
        {
            return _parse(value, this);
        }

        readonly Func<string, ParameterFunctionSetter<T, R>, bool> _parse;

        public ParameterFunctionSetter(string option, string help,
            Func<T, R> defaultValue,
            Func<string, ParameterFunctionSetter<T, R>, bool> parse)
            : base(option, help, defaultValue)
        {
            _parse = parse;
        }
    }

    public class ParameterSwitch: ParameterOption<bool>
    {
        Action _whenSwitch = () => {};
        public ParameterSwitch(string option, string help = "")
            : base(option, help, defaultValue: false,
            requiredSingleValue: true)
        {
        }

        public ParameterSwitch(string option, Action whenSwitch,
        string help = "")
            : base(option, help, defaultValue: false,
            requiredSingleValue:true)
        {
            _whenSwitch = whenSwitch;
        }

        public override bool IsPrefix(string arg)
        {
            return arg.Equals($"--{OptionName}");
        }
        public override IEnumerable<string> ToValues(string arg)
        {
            yield return arg;
        }
        public override bool Parse(string value)
        {
            _value = true;
            _whenSwitch();
            return true;
        }
    }
    public interface ISwitch
    {
        void WhenSwitch();
    }

    public class ParameterFunctionSwitch<T, R> :
        ParameterFunction<T, R>, ISwitch
    {
        Func<T, R> altValue;
        Action _whenSwitch = () => { };
        public ParameterFunctionSwitch(string option, string help,
            Func<T, R> defaultValue, Func<T, R> altValue)
            : base(option, help, defaultValue)
        {
            this.altValue = altValue;
        }

        public ParameterFunctionSwitch(string option, string help,
            Func<T, R> defaultValue, Func<T, R> altValue,
            Action whenSwitch)
            : base(option, help, defaultValue)
        {
            this.altValue = altValue;
            _whenSwitch = whenSwitch;
        }

        public override bool IsPrefix(string arg)
        {
            return arg.Equals($"--{OptionName}");
        }
        public override IEnumerable<string> ToValues(string arg)
        {
            yield return arg;
        }
        public override bool Parse(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            _value = altValue;
            WhenSwitch();
            return true;
        }

        public void WhenSwitch()
        {
            _whenSwitch();
        }
    }

    public class ParameterOptionString : IParser
    {
        protected string _value;
        readonly protected string OptionName;
        readonly protected string Help;

        public ParameterOptionString(string option,
            string help, string defaultValue)
        {
            OptionName = option;
            Help = help;
            _value = defaultValue;
        }

        public string Name()
        {
            return OptionName;
        }

        public string OnlineHelp()
        {
            return Help;
        }

        static public implicit operator string(
            ParameterOptionString arg)
        {
            return arg._value;
        }

        public override string ToString()
        {
            return _value;
        }

        virtual public IEnumerable<string> ToValues(string arg)
        {
            yield return arg.Substring(OptionName.Length + 3);
        }

        public bool Parse(string value)
        {
            _value = value;
            return true;
        }

        virtual public bool IsPrefix(string arg)
        {
            return arg.StartsWith($"--{OptionName}=");
        }

        public bool RequireSingleValue()
        {
            return true;
        }

        public bool ParseMany(IEnumerable<string> values)
        {
            throw new NotImplementedException();
        }
    }
}
