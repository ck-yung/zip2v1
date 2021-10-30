using System.Collections.Immutable;

namespace zip2
{
    public class Program
    {
        static int ShowVersion()
        {
            Console.WriteLine("TODO: Show version");
            return 0;
        }

        static public int Main(string[] argsSecondPhase)
        {
            try
            {
                return RunMain(argsSecondPhase);
            }
            catch (ArgumentException aex)
            {
                Console.WriteLine(aex.Message);
                return -1;
            }
            catch (Exception ee)
            {
                Console.WriteLine();
                var chkEnvr = Environment.GetEnvironmentVariable(
                    "zip2") ?? string.Empty;
                if (chkEnvr.Contains(":debug:"))
                {
                    Console.WriteLine(ee);
                    return 0;
                }
                Console.WriteLine(ee.Message);
                return -1;
            }
        }

        readonly static string ListCommandText = "--list";
        readonly static string CreateCommandText = "--create";
        readonly static string RestoreCommandText = "--restore";

        readonly static ImmutableDictionary<string,string>
        CommandShortcuts =
        new Dictionary<string, string>() {
            ["-l"] = ListCommandText,
            ["-c"] = CreateCommandText,
            ["-r"] = RestoreCommandText,
        }.ToImmutableDictionary();

        readonly static ImmutableDictionary<string, string>
        OptionShortcuts =
        new Dictionary<string, string>()
        {
            ["-q"] = "--quiet",
        }.ToImmutableDictionary();

        static int SayCommanHelp()
        {
            Console.WriteLine("Syntax: zip2 -v");
            Console.WriteLine("Syntax: zip2 --version");
            Console.WriteLine();
            Console.WriteLine("On-line help:");
            foreach  (var shortName in CommandShortcuts.Keys)
            {
                Console.Write("Syntax: zip2");
                Console.WriteLine(
                    $" {CommandShortcuts[shortName]} --help");
            }
            Console.WriteLine();
            Console.WriteLine("Shortcut command:");
            foreach  (var shortName in CommandShortcuts.Keys)
            {
                Console.WriteLine(
                    $"Syntax: zip2 {shortName}f ZIPFILENAME [OPT ..]");
            }
            return 9;
        }

        static int RunMain(string[] mainArgs)
        {
            var (showVersion, _) = Helper.SubractAny(
                mainArgs, new string[] { "-v","--version"});
            if (showVersion.Any())
            {
                return ShowVersion();
            }

            var argsFirstPhase = Helper.ExpandToCommand(
                mainArgs, CommandShortcuts,
                OptionShortcuts,
                new Dictionary<string, string>(){
                    ["-f"] = "--file=",
                    ["-T"] = "--files-from=",
                }.ToImmutableDictionary()
            );

            var commandMap = new Dictionary<
            string,Func<CommandBase>>(){
                [ListCommandText] = () => new list.Command(),
                [CreateCommandText] = () => new create.Command(),
                [RestoreCommandText] = () => new restore.Command(),
            };

            var (commands, argsSecondPhase) = Helper.SubractAny(
                argsFirstPhase, commandMap.Keys.ToArray());

            if (commands.Length!=1)
            {
                return SayCommanHelp();
            }

            var commandText = commands[0];

            var cmdThe = commandMap[commandText]()!;

            var (isOk, argsThirdPhase) = cmdThe.FindSingleFilename(
                argsSecondPhase);
            var (anyHelp, args) = Helper.SubractAny(argsThirdPhase,
                new string[] { "-?", "-h", "--help" });
            if ((!isOk) || anyHelp.Any())
            {
                return cmdThe.SayHelp();
            }

            if (cmdThe.Parse(args))
            {
                return cmdThe.Invoke();
            }

            return 99;
        }
    }
}
