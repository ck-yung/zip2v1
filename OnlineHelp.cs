using System.Collections.Immutable;
using System.Reflection;

namespace zip2
{
    internal static class OnlineHelp
    {
        static public int ShowVersion()
        {
            var assemblyThe = Assembly.GetExecutingAssembly();

            string Get<T>(Func<T, string> select)
                where T : Attribute
            {
                if (Attribute.IsDefined(assemblyThe, typeof(T)))
                {
                    T attribute = (T)Attribute.GetCustomAttribute(
                        assemblyThe, typeof(T))!;
                    return select.Invoke(attribute);
                }
                else
                {
                    return string.Empty;
                }
            }

            var title = Get<AssemblyTitleAttribute>(
                it => it.Title);
            var version = Get<AssemblyFileVersionAttribute>(
                it => it.Version);
            Console.WriteLine($"{title} Version {version}");
            var copyright = Get<AssemblyCopyrightAttribute>(
                it => it.Copyright);
            Console.WriteLine(copyright);
            var company = Get<AssemblyCompanyAttribute>(
                it => it.Company);
            Console.WriteLine(company);
            var description = Get<AssemblyDescriptionAttribute>(
                it => it.Description);
            Console.WriteLine(description);
            return 1;
        }

        static public int IsShow(
            ImmutableDictionary<string, string>
            CommandShortcuts)
        {
            Console.WriteLine("Syntax: zip2 -v");
            Console.WriteLine("Syntax: zip2 --version");
            Console.WriteLine();
            Console.WriteLine("On-line help:");
            foreach (var shortName in CommandShortcuts.Keys)
            {
                Console.Write("Syntax: zip2");
                Console.WriteLine(
                    $" {CommandShortcuts[shortName]} --help");
            }
            Console.WriteLine();
            Console.WriteLine("Shortcut command:");
            foreach (var shortName in CommandShortcuts.Keys)
            {
                Console.WriteLine(
                    $"Syntax: zip2 {shortName}f ZIPFILENAME [OPT ..]");
            }
            return 1;
        }
    }
}
