namespace zip2
{
    interface IParser
    {
        bool Parse(string value);
        string Name();
        string OnlineHelp();
        bool IsPrefix(string arg);
        IEnumerable<string> ToValues(string arg);
        bool RequireSingleValue();
    }
 
    interface IInvoke<T,R>: IParser
    {
        R Invoke(T arg);
    }

    interface IInvokeSetter<T,R>: IInvoke<T,R>
    {
        public void SetInvoke(Func<T, R> invokeNew);
    }

    class CommandLineOption<T,R> : IInvoke<T,R>
    {
        readonly public string Option;
        readonly public string Help;
        protected Func<T, R> _invoke;
        protected Func<string, bool> _parse;
        protected readonly bool _requiredSingleValue;

        public CommandLineOption( string option, string help,
        Func<T,R> invokeDefault, bool requiredSingleValue = true)
        {
            Option = option;
            Help = help;
            _invoke = invokeDefault;
            _parse = (_) => false;
            _requiredSingleValue = requiredSingleValue;
        }

        virtual public IEnumerable<string> ToValues(string arg)
        {
            yield return arg.Substring(Option.Length + 3);
        }
        public string Name()
        {
            return Option;
        }

        public string OnlineHelp()
        {
            return Help;
        }

        public R Invoke(T arg)
        {
            return _invoke(arg);
        }

        public bool Parse(string value)
        {
            return _parse(value);
        }

        public override string ToString()
        {
            return $"--{Option}=";
        }

        public bool IsPrefix(string arg)
        {
            return arg.StartsWith("--" + Option + "=");
        }

        public bool RequireSingleValue()
        {
            return _requiredSingleValue;
        }
    }

    class CommandLineOptionSetter<T,R>
        : CommandLineOption<T,R>, IInvokeSetter<T,R>
    {
        public CommandLineOptionSetter(
            string option, string help, Func<T, R> invokeDefault,
            Func<string, CommandLineOptionSetter<T, R>, bool> parse,
            bool requiredSingleValue = true)
            : base(option, help, invokeDefault, requiredSingleValue)
        {
            _parse = (value) => parse(value,this);
        }

        public void SetInvoke(Func<T, R> invokeNew)
        {
            _invoke = invokeNew;
        }
    }
}