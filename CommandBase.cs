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

            if (found.Length == 1)
            {
                zipFilename = found[0]
                .Substring(zipFilenamePrefix.Length);
                return (true, others);
            }
            return (false, args); ;
        }

        static public ParameterSwitch Quiet = new ParameterSwitch("quiet");

        protected int SayHelp
            ( string name, IParser[] opts
            , string fileHint = "FILE"
            )
        {
            Console.WriteLine(
                $"Syntax: zip2 {name} --file=ZIPFILE [OPT ..] [{fileHint} ..]");
            Console.WriteLine("OPT:");
            foreach (var opt in opts)
            {
                var prefix = $"--{opt.Name()}";
                Console.Write($"  {prefix,19}");
                if (!string.IsNullOrEmpty(opt.OnlineHelp()))
                {
                    Console.Write($"={opt.OnlineHelp()}");
                }
                Console.WriteLine();
            }
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
            R defaultValue, bool requiredSingleValue = true)
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
    }

    public abstract class ParameterFunction<T, R> :
        ParameterOption<Func<T,R>>
    {
        public ParameterFunction(string option, string help,
            Func<T,R> defaultValue,
            bool requiredSingleValue = true) :
            base(option,help,defaultValue, requiredSingleValue)
        { }
        public R Invoke(T arg)
        {
            return ((Func<T, R>)_value)(arg);
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
            Func<string, ParameterOptionSetter<R>, bool> parse,
            bool requiredSingleValue = false) :
            base (option, help, defaultValue, requiredSingleValue)
        {
            _parse = parse;
        }
        override public bool Parse(string arg)
        {
            return _parse(arg,this);
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
            Func<string, ParameterFunctionSetter<T, R>, bool> parse,
            bool requiredSingleValue = true)
            : base(option,help,defaultValue, requiredSingleValue)
        {
            _parse = parse;
        }
    }

    public class ParameterSwitch: ParameterOption<bool>
    {
        public ParameterSwitch(string option)
            : base(option,string.Empty,false,
            requiredSingleValue:false)
        {
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
            return true;
        }
    }
}
