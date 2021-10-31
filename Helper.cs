using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace zip2
{
    internal static class Helper
    {
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
            , ImmutableDictionary<string, string> optionShortcuts
            , ImmutableDictionary<string, string> withValueOptions
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
                else if (optionShortcuts.ContainsKey(current))
                {
                    yield return optionShortcuts[current];
                }
                else if (withValueOptions.ContainsKey(current))
                {
                    if (!enumThe.MoveNext())
                    {
                        throw new ArgumentException(
                            $"Missing value to '{current}','{withValueOptions[current]}'");
                    }
                    var valueThe = enumThe.Current;
                    yield return $"{withValueOptions[current]}{valueThe}";
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
