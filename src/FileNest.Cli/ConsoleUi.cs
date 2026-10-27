using System.Runtime.InteropServices;
using System.Text;
using FileNest.Core;

namespace FileNest.Cli;

/// <summary>Todo lo que se ve en la consola: colores, cajas, barras y menus.</summary>
internal static class ConsoleUi
{
    private const int BoxWidth = 62;
    private const string Reset = "\u001b[0m";
    private const string Bold = "\u001b[1m";
    private const string Grey = "\u001b[90m";

    public static bool Color { get; set; }
    public static bool Ansi { get; set; }

    // ---------------------------------------------------------------- salida --

    public static void Out(string text) => Console.Out.Write(text);

    public static void Line(string text = "") => Console.Out.WriteLine(text);

    public static string C(string code, string text) => Color ? code + text + Reset : text;

    /// <summary>Activa los colores ANSI en la consola (si es una consola de verdad).</summary>
    public static bool EnableAnsi()
    {
        IntPtr handle = GetStdHandle(StdOutputHandle);
        if (handle == IntPtr.Zero || handle == new IntPtr(-1)) return false;

        if (!GetConsoleMode(handle, out uint mode)) return false;
        mode |= EnableVirtualTerminalProcessing | EnableProcessedOutput;
        return SetConsoleMode(handle, mode);
    }

    // ----------------------------------------------------------------- ancho --

    /// <summary>Ancho visible en la consola (emojis y caracteres anchos cuentan 2).</summary>
    public static int Width(string text)
    {
        int width = 0;
        for (int i = 0; i < text.Length; ++i)
        {
            char ch = text[i];

            if (char.IsHighSurrogate(ch) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                width += 2;
                ++i;
                continue;
            }

            if (ch >= 0x1100 && (ch <= 0x115F || ch == 0x2329 || ch == 0x232A))
            {
                width += 2;
                continue;
            }

            width += 1;
        }

        return width;
    }

    private static string Repeat(string text, int times)
    {
        var sb = new StringBuilder(text.Length * Math.Max(times, 0));
        for (int i = 0; i < times; ++i) sb.Append(text);
        return sb.ToString();
    }

    public static string PadRight(string text, int width)
    {
        int current = Width(text);
        return current >= width ? text : text + Repeat(" ", width - current);
    }

    public static string PadLeft(string text, int width)
    {
        int current = Width(text);
        return current >= width ? text : Repeat(" ", width - current) + text;
    }

    // ---------------------------------------------------------------- cabecera --

    public static void Banner()
    {
        const string border = "\u001b[38;5;39m";
        string title = Lang.T("  🗂  ORGANIZADOR DE CARPETAS", "  🗂  FOLDER ORGANIZER");
        string version = "v" + AppInfo.Version;

        int pad = BoxWidth - Width(title) - Width(version) - 1;
        if (pad < 1) pad = 1;

        string content = title + Repeat(" ", pad) + version + " ";
        if (Width(content) < BoxWidth) content += Repeat(" ", BoxWidth - Width(content));

        Line();
        Line("  " + C(border, "╔" + Repeat("═", BoxWidth) + "╗"));
        Line("  " + C(border, "║") + C(Bold, content) + C(border, "║"));
        Line("  " + C(border, "╚" + Repeat("═", BoxWidth) + "╝"));
        Line();
    }

    // ------------------------------------------------------------------ menu --

    public static void Menu()
    {
        Banner();
        Line("   " + C("\u001b[96m", "[1]") + Lang.T("  Elegir una carpeta y organizarla   ", "  Choose a folder and organize it   ")
             + C(Grey, Lang.T("(simula y luego pregunta)", "(simulates, then asks)")));
        Line("   " + C("\u001b[96m", "[2]") + Lang.T("  Simular sobre mi carpeta Descargas", "  Simulate on my Downloads folder"));
        Line("   " + C("\u001b[96m", "[3]") + Lang.T("  Aplicar en mi carpeta Descargas", "  Apply to my Downloads folder"));
        Line("   " + C("\u001b[96m", "[4]") + Lang.T("  Deshacer la ultima ejecucion de Descargas", "  Undo the last run on Downloads"));
        Line("   " + C("\u001b[96m", "[5]") + Lang.T("  Deshacer indicando la carpeta", "  Undo specifying the folder"));
        Line("   " + C(Grey, Lang.T("[0]  Salir", "[0]  Exit")));
        Line();
        Line("   " + C(Grey, Lang.T("Consejo: también hay una versión con ventana: ", "Tip: there is also a window version: "))
             + C(Bold, "Organizador de carpetas.exe"));
        Line();
    }

    // ------------------------------------------------------------------ ayuda --

    public static void Help()
    {
        Banner();
        Line("  " + C(Bold, Lang.T("USO", "USAGE")));
        Line(Lang.T("    organizador.exe [opciones]", "    organizador.exe [options]"));
        Line();
        Line("  " + C(Bold, Lang.T("OPCIONES", "OPTIONS")));
        Line(Lang.T("    --dir <ruta>        Carpeta a organizar (por defecto: tu carpeta Descargas)",
                    "    --dir <path>        Folder to organize (default: your Downloads folder)"));
        Line(Lang.T("    --apply             Aplica los cambios. Sin esto solo SIMULA (no toca nada)",
                    "    --apply             Applies the changes. Without it, it only SIMULATES (nothing is touched)"));
        Line(Lang.T("    --by-date           Dentro de cada categoria crea subcarpetas Anio\\Mes",
                    "    --by-date           Inside each category, creates Year\\Month subfolders"));
        Line(Lang.T("    --recursive, -r     Incluye tambien los archivos de las subcarpetas",
                    "    --recursive, -r     Also includes files inside subfolders"));
        Line(Lang.T("    --rule .ext=Carpeta Anade o modifica una regla (ej: --rule .psd=Diseno)",
                    "    --rule .ext=Folder  Adds or changes a rule (e.g. --rule .psd=Design)"));
        Line(Lang.T("                        Ademas de la extension vale un patron, y la carpeta admite variables:",
                    "                        Besides an extension you can use a pattern, and the folder accepts variables:"));
        Line(Lang.T("                        --rule \"factura*.pdf=Facturas\\{año}\"   ({año} {mes} {dia} {ext} {nombre})",
                    "                        --rule \"invoice*.pdf=Invoices\\{year}\"    ({year} {month} {day} {ext} {name})"));
        Line(Lang.T("    --undo <log.csv>    Deshace una ejecucion usando su archivo de registro",
                    "    --undo <log.csv>    Undoes a run using its log file"));
        Line(Lang.T("    --color / --no-color   Fuerza o desactiva los colores",
                    "    --color / --no-color   Forces or disables colors"));
        Line(Lang.T("    --quiet, -q         Menos salida por pantalla", "    --quiet, -q         Less output on screen"));
        Line(Lang.T("    --menu              Muestra el menu principal (lo usa Organizador.bat)",
                    "    --menu              Shows the main menu (used by Organizador.bat)"));
        Line(Lang.T("    --help, -h          Muestra esta ayuda", "    --help, -h          Shows this help"));
        Line();
        Line("  " + C(Bold, Lang.T("CATEGORIAS POR DEFECTO", "DEFAULT CATEGORIES")));
        foreach (var cat in Categories.Default)
        {
            string extensions = string.Join(", ", cat.Extensions);
            Line("    " + cat.Icon + " " + C(cat.AnsiColor, PadRight(cat.Name, 14)) + C(Grey, extensions));
        }

        Line("    " + Categories.IconOf(Categories.OtherName) + " "
             + C(Grey, PadRight(Categories.OtherName, 14))
             + C(Grey, Lang.T("el resto de extensiones", "everything else")));
        Line();
    }

    // ------------------------------------------------------------- organizar --

    public static int Organize(Options options, bool apply, bool quiet)
    {
        if (!Directory.Exists(options.Directory))
        {
            Banner();
            Line("  " + C("\u001b[91m", Lang.T("✖ ERROR: la carpeta no existe o no es una carpeta:",
                                               "✖ ERROR: the folder does not exist or is not a folder:")));
            Line("      " + options.Directory);
            Line();
            return 2;
        }

        string dir = Path.GetFullPath(options.Directory);

        Banner();
        KeyValue("📁", Lang.T("Carpeta", "Folder"), C(Bold, dir));
        KeyValue("⚙", Lang.T("Modo", "Mode"), apply
            ? C("\u001b[93m", Lang.T("APLICAR", "APPLY")) + C(Grey, Lang.T("  ·  se moverán archivos", "  ·  files will be moved"))
            : C("\u001b[96m", Lang.T("SIMULACIÓN", "SIMULATION")) + C(Grey, Lang.T("  ·  no se moverá nada", "  ·  nothing will be moved")));
        KeyValue("📅", Lang.T("Fecha", "Date"), options.ByDate
            ? Lang.T("sí", "yes") + C(Grey, Lang.T("  ·  agrupar por Año\\Mes", "  ·  group by Year\\Month"))
            : C(Grey, Lang.T("no", "no")));
        KeyValue("🔁", Lang.T("Subcarpetas", "Subfolders"), options.Recursive
            ? Lang.T("sí", "yes") + C(Grey, Lang.T("  ·  recursivo", "  ·  recursive"))
            : C(Grey, Lang.T("no", "no")));
        Line();

        Plan plan = Planner.Build(options);

        string stats = "   " + C(Bold, plan.Analyzed.ToString()) + Lang.T(" analizados", " analyzed")
                     + C(Grey, "   ·   ") + C(Bold, plan.Moves.Count.ToString()) + Lang.T(" a clasificar", " to sort");
        if (plan.Skipped > 0) stats += C(Grey, "   ·   ") + plan.Skipped + Lang.T(" omitidos", " skipped");
        Line(stats);
        Line();

        if (plan.Moves.Count == 0)
        {
            Line("  " + C("\u001b[92m", Lang.T("✔ Nada que organizar: la carpeta ya está en orden.",
                                              "✔ Nothing to organize: the folder is already tidy.")));
            Line();
            return 0;
        }

        if (!quiet)
        {
            BarChart(plan.ByCategory);
            if (!apply)
            {
                Line("   " + C(Grey, Lang.T("ARCHIVOS", "FILES")));
                FileList(plan.Moves, dir, 40);
            }
        }

        if (!apply)
        {
            Line("  " + C("\u001b[93m", Lang.T("⚠ SIMULACIÓN", "⚠ SIMULATION")) + C(Grey, Lang.T(": no se movió nada.", ": nothing was moved.")));
            Line("   " + C(Grey, Lang.T("Para aplicarlo: ", "To apply it: ")) + C(Bold, "--apply")
                 + C(Grey, Lang.T("  ·  o abriendo ", "  ·  or by opening ")) + C(Bold, "\"Organizador.bat\""));
            Line();
            return 0;
        }

        long total = plan.Moves.Count;
        long progressCount = 0;

        ApplyResult result = Applier.Apply(plan, dir, (move, ok, error) =>
        {
            if (!ok)
            {
                if (Ansi) Out("\u001b[K");
                Line("   " + C("\u001b[91m", "✖ ") + move.FileName + C(Grey, ": " + error));
            }

            ++progressCount;
            ProgressBar(progressCount, total);
        });

        if (Ansi) { Out("\u001b[K"); Line(); }
        Line();

        Line("  " + C("\u001b[92m", Repeat("─", BoxWidth + 2)));
        Line("  " + C("\u001b[92m", Lang.T("✔ COMPLETADO", "✔ COMPLETED")) + "    "
             + C(Grey, Lang.T("Movidos: ", "Moved: ")) + C(Bold, result.Moved.ToString()) + "    "
             + C(Grey, Lang.T("Errores: ", "Errors: "))
             + (result.Errors > 0 ? C("\u001b[91m", result.Errors.ToString()) : C(Bold, "0")));
        Line("  " + C(Grey, Lang.T("Registro: ", "Log: ")) + result.LogName);
        Line("  " + C("\u001b[92m", Repeat("─", BoxWidth + 2)));
        Line();
        Line("   " + C(Grey, Lang.T("Para deshacer: ", "To undo: ")) + C(Bold, "\"Organizador.bat\"")
             + C(Grey, Lang.T("  ·  o bien: ", "  ·  or else: ")) + C(Bold, "organizador.exe --undo \"" + result.LogPath + "\""));
        Line();

        return result.Errors > 0 ? 1 : 0;
    }

    // --------------------------------------------------------------- deshacer --

    public static int Undo(string logPath)
    {
        Banner();

        if (!File.Exists(logPath))
        {
            Line("  " + C("\u001b[91m", Lang.T("✖ ERROR: no encuentro el archivo de registro:", "✖ ERROR: log file not found:")));
            Line("      " + logPath);
            Line();
            return 2;
        }

        UndoResult result = Undoer.Undo(logPath, (path, ok) =>
        {
            if (!ok) Line("   " + C("\u001b[91m", "✖ ") + Path.GetFileName(path));
        });

        Line("  " + C("\u001b[92m", Repeat("─", BoxWidth + 2)));
        Line("  " + C("\u001b[92m", Lang.T("✔ DESHACER COMPLETADO", "✔ UNDO COMPLETED")) + "    "
             + C(Grey, Lang.T("Restaurados: ", "Restored: ")) + C(Bold, result.Restored.ToString())
             + (result.Skipped > 0 ? C(Grey, Lang.T("    Omitidos: ", "    Skipped: ")) + result.Skipped : "")
             + (result.Errors > 0 ? C(Grey, Lang.T("    Errores: ", "    Errors: ")) + C("\u001b[91m", result.Errors.ToString()) : ""));
        Line("  " + C("\u001b[92m", Repeat("─", BoxWidth + 2)));
        Line();

        return result.Errors > 0 ? 1 : 0;
    }

    // ------------------------------------------------------------------ piezas --

    private static void KeyValue(string icon, string key, string value)
        => Line("   " + icon + " " + C(Grey, PadRight(key, 13)) + " " + value);

    private static void BarChart(SortedDictionary<string, long> counts)
    {
        long max = 1;
        int maxName = 5;

        foreach (var pair in counts)
        {
            max = Math.Max(max, pair.Value);
            maxName = Math.Max(maxName, Width(pair.Key));
        }

        Line("   " + C(Grey, Lang.T("CATEGORÍAS", "CATEGORIES")));

        foreach (var pair in counts)
        {
            int filled = (int)((pair.Value * 20) / max);
            if (filled == 0 && pair.Value > 0) filled = 1;

            var bar = new StringBuilder(20);
            for (int i = 0; i < 20; ++i) bar.Append(i < filled ? '█' : '░');

            Line("   " + Categories.IconOf(pair.Key) + " "
                 + C(Categories.AnsiOf(pair.Key), PadRight(pair.Key, maxName)) + "  "
                 + C(Bold, PadLeft(pair.Value.ToString(), 3)) + "  "
                 + C(Categories.AnsiOf(pair.Key), bar.ToString()));
        }

        Line();
    }

    private static void FileList(IReadOnlyList<MoveItem> moves, string baseDir, int limit)
    {
        int maxName = 0;
        int count = Math.Min(limit, moves.Count);

        for (int i = 0; i < count; ++i)
            maxName = Math.Max(maxName, Math.Min(Width(moves[i].FileName), 32));

        for (int i = 0; i < count; ++i)
        {
            MoveItem move = moves[i];
            string shown = move.FileName;

            if (Width(shown) > 32)
            {
                while (shown.Length > 0 && Width(shown) > 29) shown = shown[..^1];
                shown += "...";
            }

            string relative = Paths.RelativeTo(move.To, baseDir);
            Line("   " + Categories.IconOf(move.Category) + " " + PadRight(shown, maxName)
                 + C(Grey, "  →  ") + C(Categories.AnsiOf(move.Category), relative));
        }

        if (moves.Count > count)
            Line("   " + C(Grey, Lang.T("   … y ", "   … and ") + (moves.Count - count) + Lang.T(" archivo(s) más", " more file(s)")));

        Line();
    }

    private static void ProgressBar(long done, long total)
    {
        if (!Ansi) return;

        const int width = 28;
        int filled = total > 0 ? (int)((done * width) / total) : width;

        var bar = new StringBuilder(width);
        for (int i = 0; i < width; ++i) bar.Append(i < filled ? '█' : '░');

        int percent = total > 0 ? (int)((done * 100) / total) : 100;

        Out("\r   " + C("\u001b[94m", bar.ToString()) + "  " + C(Bold, PadLeft(percent.ToString(), 3) + "%")
            + "  " + C(Grey, done + "/" + total) + "\u001b[K");
        Console.Out.Flush();
    }

    // ------------------------------------------------------------------- win32 --

    private const int StdOutputHandle = -11;
    private const uint EnableProcessedOutput = 0x0001;
    private const uint EnableVirtualTerminalProcessing = 0x0004;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr handle, out uint mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr handle, uint mode);
}
