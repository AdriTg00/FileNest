using System.Text;
using FileNest.Core;

namespace FileNest.Cli;

internal static class Program
{
    private sealed class Arguments
    {
        public Options Options { get; } = new();
        public bool Apply { get; set; }
        public bool Quiet { get; set; }
        public bool Undo { get; set; }
        public bool Help { get; set; }
        public bool Menu { get; set; }
        public bool LangOnly { get; set; }
        public int ColorMode { get; set; }          // 0 = auto, 1 = forzar, -1 = desactivar
        public string UndoLog { get; set; } = "";
    }

    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        ConsoleUi.Ansi = ConsoleUi.EnableAnsi();

        var parsed = new Arguments();
        if (!ParseArguments(args, parsed)) return 2;

        bool noColorEnv = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));
        ConsoleUi.Color = parsed.ColorMode switch
        {
            1 => true,
            -1 => false,
            _ => ConsoleUi.Ansi && !noColorEnv,
        };

        if (parsed.LangOnly)          // uso interno de Organizador.bat
        {
            Console.Out.WriteLine(Lang.Code);
            return 0;
        }

        if (parsed.Help)
        {
            ConsoleUi.Help();
            return 0;
        }

        if (parsed.Menu)
        {
            ConsoleUi.Menu();
            return 0;
        }

        return parsed.Undo
            ? ConsoleUi.Undo(parsed.UndoLog)
            : ConsoleUi.Organize(parsed.Options, parsed.Apply, parsed.Quiet);
    }

    private static bool ParseArguments(string[] args, Arguments parsed)
    {
        for (int i = 0; i < args.Length; ++i)
        {
            string arg = args[i];

            switch (arg)
            {
                case "--help":
                case "-h":
                case "/?":
                    parsed.Help = true;
                    break;

                case "--menu":
                    parsed.Menu = true;
                    break;

                case "--lang":
                    parsed.LangOnly = true;
                    break;

                case "--apply":
                    parsed.Apply = true;
                    break;

                case "--by-date":
                    parsed.Options.ByDate = true;
                    break;

                case "--recursive":
                case "-r":
                    parsed.Options.Recursive = true;
                    break;

                case "--quiet":
                case "-q":
                    parsed.Quiet = true;
                    break;

                case "--color":
                    parsed.ColorMode = 1;
                    break;

                case "--no-color":
                    parsed.ColorMode = -1;
                    break;

                case "--dir":
                case "-d":
                {
                    string? value = NextValue(args, ref i, "--dir");
                    if (value == null) return false;
                    parsed.Options.Directory = value;
                    break;
                }

                case "--undo":
                {
                    string? value = NextValue(args, ref i, "--undo");
                    if (value == null) return false;
                    parsed.Undo = true;
                    parsed.UndoLog = value;
                    break;
                }

                case "--rule":
                {
                    string? value = NextValue(args, ref i, "--rule");
                    if (value == null) return false;

                    int eq = value.IndexOf('=');
                    if (eq <= 0 || eq + 1 >= value.Length)
                    {
                        ConsoleUi.Line("  " + ConsoleUi.C("\u001b[91m", Lang.T(
                            "ERROR: --rule debe tener la forma .ext=Carpeta (ej: --rule .psd=Diseno)",
                            "ERROR: --rule must look like .ext=Folder (e.g. --rule .psd=Design)")));
                        return false;
                    }

                    parsed.Options.CustomRules.Add(new Rule(
                        RuleEngine.NormalizePattern(value[..eq]),
                        value[(eq + 1)..].Trim()));
                    break;
                }

                default:
                    ConsoleUi.Line("  " + ConsoleUi.C("\u001b[91m",
                        Lang.T("ERROR: opcion desconocida: ", "ERROR: unknown option: ") + arg));
                    ConsoleUi.Line(Lang.T("  Usa --help para ver las opciones disponibles.",
                                          "  Use --help to see the available options."));
                    return false;
            }
        }

        return true;
    }

    private static string? NextValue(string[] args, ref int index, string name)
    {
        if (index + 1 >= args.Length)
        {
            ConsoleUi.Line("  " + ConsoleUi.C("\u001b[91m",
                Lang.T("ERROR: falta el valor de ", "ERROR: missing value for ") + name));
            return null;
        }

        return args[++index];
    }
}
