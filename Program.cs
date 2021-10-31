using System.Collections.Immutable;

namespace zip2
{
    public class Program
    {
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
            ["-v"] = "--version",
            ["-h"] = "--help",
            ["-?"] = "--help",
        }.ToImmutableDictionary();

        static int RunMain(string[] mainArgs)
        {
            if (mainArgs.Contains("-v") ||
                mainArgs.Contains("--version"))
            {
                return OnlineHelp.ShowVersion();
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
            }.ToImmutableDictionary();

            var (commands, argsSecondPhase) = Helper.SubractAny(
                argsFirstPhase, commandMap.Keys.ToArray());

            if (commands.Length != 1)
            {
                return OnlineHelp.IsShow(
                    CommandShortcuts);
            }

            var commandText = commands[0];
            var cmdThe = commandMap[commandText]()!;

            var (isZipFilenameFound, args) =
                cmdThe.FindSingleFilename(
                argsSecondPhase);

            if (!isZipFilenameFound)
            {
                return cmdThe.SayHelp();
            }

            if (cmdThe.Parse(args))
            {
                return cmdThe.Invoke();
            }

            return 1;
        }
    }
}
