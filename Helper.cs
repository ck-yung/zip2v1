using System.Collections.Immutable;

namespace zip2
{
    internal static class Helper
    {
        static public IEnumerable<string>
        ExpandToCommand
        ( string[] args
        , ImmutableDictionary<string,string> fromShortcuts
        , ImmutableDictionary<string,string> withValueOptions
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
                if (fromShortcuts.ContainsKey(current))
                {
                    yield return fromShortcuts[current];
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
}
