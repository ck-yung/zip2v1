using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace zip2
{
    internal static class Helper
    {
        static public readonly Func<string,bool>
        StringFilterAlwaysTrue = (_) => true;

        static public readonly Func<string,bool>
        StringFilterAlwaysFalse = (_) => false;

        static public string ToStandardDirSep(string arg)
        {
            return arg.Replace('\\','/');
        }

        static public Func<string,string> GetEnvirDirSepFunc()
        {
            if (Path.DirectorySeparatorChar!='/')
                return (it) => it.Replace('/',Path.DirectorySeparatorChar);
            return (it) => it;
        }

        static public (string[] otherOptions, string[] otherArgs)
            ParseFrom(
            this IParser[] opts, IEnumerable<string> args)
        {
            var qry9 = opts
                .Aggregate(args, (seq, opt) => opt.Parse(seq))
                .GroupBy((it) => it.StartsWith('-'))
                .ToDictionary((grp) => grp.Key, (grp) => grp.ToArray());
            return (qry9.ContainsKey(true) ? qry9[true]
                : Array.Empty<string>(),
                qry9.ContainsKey(false) ? qry9[false]
                : Array.Empty<string>());
        }

        static public IEnumerable<string> Parse(
            this IParser opt, IEnumerable<string> args)
        {
            (string[] founds, IEnumerable<string> rtn) =
                args
                .SubractStartsWith(opt.IsPrefix,
                toValues: (seq) => seq.Select(
                    (it) => opt.ToValues(it))
                    .SelectMany((seq2) => seq2));

            if (founds.Length == 0) return rtn;

            if (opt.RequireSingleValue())
            {
                if (founds.Length > 1)
                {
                    throw new TooManyValuesException(
                        $"Too many value to --{opt.Name()}");
                }
                if (!opt.Parse(founds[0]))
                {
                    throw new InvalidValueException(
                        founds[0], opt.Name());
                }
            }
            else
            {
                if (!opt.ParseMany(founds
                    .Select((it) => it.Split(','))
                    .SelectMany((it) => it)
                    .Distinct()))
                {
                    throw new InvalidValueException(
                        String.Join(';',founds), opt.Name());
                }
            }

            return rtn;
        }

        static public Regex ToDosRegex(this string arg)
        {
            var regText = new System.Text.StringBuilder("^");
            regText.Append(arg
                .Replace(@"\", @"\\")
                .Replace("^", @"\^")
                .Replace("$", @"\$")
                .Replace(".", @"\.")
                .Replace("?", ".")
                .Replace("*", ".*")
                .Replace("(", @"\(")
                .Replace(")", @"\)")
                .Replace("[", @"\[")
                .Replace("]", @"\]")
                .Replace("{", @"\{")
                .Replace("}", @"\}")
                ).Append('$');
            return new Regex( regText.ToString(),
                RegexOptions.IgnoreCase);
        }

        static public IEnumerable<string>
        ExpandToCommand( string[] args
            , ImmutableDictionary<string, string> commandShortcuts
            , ImmutableDictionary<string, string> switchShortcuts
            , ImmutableDictionary<string, string> optionShortcuts
            )
        {
            IEnumerable<string> ExpandCombiningShortcut()
            {
                var enum2 = args.AsEnumerable().GetEnumerator();
                while (enum2.MoveNext())
                {
                    var curr2 = enum2.Current;
                    if (curr2.Length < 3) yield return curr2;
                    else if (curr2.StartsWith("--")) yield return curr2;
                    else if (curr2[0] != '-') yield return curr2;
                    else foreach (var chOpt in curr2[1..])
                            yield return $"-{chOpt}";
                }
            }

            var enumThe = ExpandCombiningShortcut().GetEnumerator();
            while (enumThe.MoveNext())
            {
                var current = enumThe.Current;
                if (commandShortcuts.ContainsKey(current))
                {
                    yield return commandShortcuts[current];
                }
                else if (switchShortcuts.ContainsKey(current))
                {
                    yield return switchShortcuts[current];
                }
                else if (optionShortcuts.ContainsKey(current))
                {
                    if (!enumThe.MoveNext())
                    {
                        throw new ArgumentException(
                            $"Missing value to '{current}','{optionShortcuts[current]}'");
                    }
                    var valueThe = enumThe.Current;
                    yield return $"{optionShortcuts[current]}{valueThe}";
                }
                else
                {
                    yield return current;
                }
            }
        }

        static public IEnumerable<string>
        ExpandToOptions(IEnumerable<string> args
            , ImmutableDictionary<string, string[]> switchShortcuts
            , ImmutableDictionary<string, string> optionShortcuts
            )
        {
            IEnumerable<string> ExpandCombiningShortcut()
            {
                var enum2 = args.AsEnumerable().GetEnumerator();
                while (enum2.MoveNext())
                {
                    var curr2 = enum2.Current;
                    if (curr2.Length < 3) yield return curr2;
                    else if (curr2.StartsWith("--")) yield return curr2;
                    else if (curr2[0] != '-') yield return curr2;
                    else foreach (var chOpt in curr2[1..])
                            yield return $"-{chOpt}";
                }
            }

            var enumThe = ExpandCombiningShortcut().GetEnumerator();
            while (enumThe.MoveNext())
            {
                var current = enumThe.Current;
                if (switchShortcuts.ContainsKey(current))
                {
                    foreach (var switchThe in switchShortcuts[current])
                    {
                        yield return switchThe;
                    }
                }
                else if (optionShortcuts.ContainsKey(current))
                {
                    if (!enumThe.MoveNext())
                    {
                        throw new ArgumentException(
                            $"Missing value to '{current}','{optionShortcuts[current]}'");
                    }
                    var valueThe = enumThe.Current;
                    yield return $"{optionShortcuts[current]}{valueThe}";
                }
                else
                {
                    yield return current;
                }
            }
        }

        static public (string[], IEnumerable<string>)
        SubractAny (this IEnumerable<string> args,
        string[] toFind)
        {
            Func<string,bool> finding =
            (arg) => toFind.Any((it) => it==arg);

            var qryThis = args
            .GroupBy((it) => finding(it))
            .ToImmutableDictionary(
                (grp) => grp.Key, (grp) => grp.AsEnumerable());

            string[] startingWith = (qryThis.ContainsKey(true))
                ? qryThis[true].Distinct().ToArray()
                : Array.Empty<string>();

            var others = (qryThis.ContainsKey(false))
                ? qryThis[false] : Array.Empty<string>();

            return (startingWith,others);
        }

        static public (string[], IEnumerable<string>)
        SubractStartsWith(this IEnumerable<string> args, string starting)
        {
            var qryThis = args
            .GroupBy((it) => it.StartsWith(starting))
            .ToImmutableDictionary(
                (grp) => grp.Key, (grp) => grp.AsEnumerable());

            string[] startingWith = (qryThis.ContainsKey(true))
                ? qryThis[true].Distinct().ToArray()
                : Array.Empty<string>();

            var others = (qryThis.ContainsKey(false))
                ? qryThis[false] : Array.Empty<string>();

            return (startingWith,others);
        }

        static public (string[], IEnumerable<string>)
        SubractStartsWith(this IEnumerable<string> args,
        Func<string,bool> filter,
        Func<IEnumerable<string>,IEnumerable<string>> toValues)
        {
            var qryThis = args
            .GroupBy((it) => filter(it))
            .ToImmutableDictionary(
                (grp) => grp.Key, (grp) => grp.AsEnumerable());

            string[] startingWith = (qryThis.ContainsKey(true))
                ? toValues(qryThis[true]).Distinct().ToArray()
                : Array.Empty<string>();

            var others = (qryThis.ContainsKey(false))
                ? qryThis[false] : Array.Empty<string>();

            return (startingWith,others);
        }

        static public string GetRootDirectory(this string arg)
        {
            if (string.IsNullOrEmpty(arg)) return "?";
            var parts = arg.Split('/', '\\');
            if (parts.Length == 1) return ".";
            return parts[0];
        }

        static public IEnumerable<string> ReadConsoleAllLines()
        {
            string? inputLine = null;
            while (true)
            {
                inputLine = Console.ReadLine();
                if (inputLine == null) break;
                yield return inputLine;
            }
        }

        static public string ReadConsolePassword(
            int requireInputCount = 1)
        {
            string NoEchoInput(string inputPrompt)
            {
                Console.Error.Write(inputPrompt);
                var buf = new Stack<char>();
                ConsoleKeyInfo cki;
                Console.TreatControlCAsInput = true;
                do
                {
                    cki = Console.ReadKey(true);
                    if (cki.Key == ConsoleKey.Enter) break;
                    int inp = (int)cki.KeyChar;

                    if (((ConsoleModifiers.Shift & cki.Modifiers) != 0)
                        && (inp >= 'a') && (inp <= 'z'))
                    {
                        inp += 'A' - 'a';
                    }

                    if ((inp >= ' ') && (inp < 127))
                    {
                        buf.Push((char)inp);
                        Console.Error.Write('*');
                    }
                    else if (cki.Key == ConsoleKey.Backspace)
                    {
                        if (buf.Count > 0)
                        {
                            buf.Pop();
                            Console.Error.Write('<');
                        }
                    }
                } while (true);
                Console.Error.WriteLine();
                var tmp2 = buf.ToArray();
                Array.Reverse(tmp2);
                return new string(tmp2);
            }

            var get1 = NoEchoInput("Input password: ");

            if (string.IsNullOrEmpty(get1))
            {
                throw new ArgumentException("No password is input!");
            }

            if (requireInputCount == 1)
            {
                return get1;
            }

            if (get1 != NoEchoInput("Input again: "))
            {
                throw new ArgumentException(
                    "Different passwords are input!");
            }

            return get1;
        }
    }

    internal class Seq<T>
    {
        static readonly public Func<IEnumerable<T>,IEnumerable<T>>
        NoChange = (seq) => seq;
    }

    class TooManyValuesException : ArgumentException
    {
        public TooManyValuesException(string message)
            : base(message) { }
    }

    class InvalidValueException : ArgumentException
    {
        public InvalidValueException(string value, string optName)
            : base($"'{value}' is unknown to --{optName}") { }
    }
}
