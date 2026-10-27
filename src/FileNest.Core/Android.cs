using System.Diagnostics;
using System.Text;

namespace FileNest.Core;

/// <summary>Un archivo visto en el movil y la carpeta (relativa) a la que iria.</summary>
public sealed record PhoneFile(string Name, string Folder);

/// <summary>
/// Organizar dentro de un movil Android por USB (adb).
///
/// Tecnica: se genera un script .sh en el PC, se sube con "adb push" a
/// /data/local/tmp, se ejecuta con "adb shell sh" y se borra. Asi se evita el
/// infierno de comillas de "adb shell" y funciona con acentos y espacios.
///
/// IMPORTANTE: adb se ejecuta redirigiendo la salida a un FICHERO, nunca con
/// tuberias: el proceso daemon de adb hereda los handles y una tuberia no se
/// cierra nunca (la ventana se quedaba congelada).
/// </summary>
public static class Android
{
    private const int DefaultTimeoutMs = 12_000;
    private static string? _adbPath;

    /// <summary>Ruta de adb.exe, o null si no esta instalado.</summary>
    public static string? AdbPath => _adbPath ??= Locate();

    public static bool IsAvailable => AdbPath != null;

    private static string? Locate()
    {
        // 1) En el PATH
        string? pathVar = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrEmpty(pathVar))
        {
            foreach (string dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    string candidate = Path.Combine(dir.Trim('"'), "adb.exe");
                    if (File.Exists(candidate)) return candidate;
                }
                catch
                {
                    // entradas raras del PATH: se ignoran
                }
            }
        }

        // 2) Instalado con winget (Google.PlatformTools)
        string packages = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WinGet", "Packages");

        try
        {
            if (Directory.Exists(packages))
            {
                foreach (string dir in Directory.EnumerateDirectories(packages, "Google.PlatformTools_*"))
                {
                    string candidate = Path.Combine(dir, "platform-tools", "adb.exe");
                    if (File.Exists(candidate)) return candidate;
                }
            }
        }
        catch
        {
            // sin acceso a la carpeta de winget
        }

        return null;
    }

    /// <summary>Ejecuta adb y devuelve su salida (vacia si no responde).</summary>
    public static string Run(string arguments, int timeoutMs = DefaultTimeoutMs)
    {
        string? adb = AdbPath;
        if (adb == null) return "";

        string temp = Path.Combine(Path.GetTempPath(), "filenest-adb-" + Guid.NewGuid().ToString("N") + ".txt");

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"\"{adb}\" {arguments} > \"{temp}\" 2>&1\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetTempPath(),
            };

            using (var process = Process.Start(psi))
            {
                if (process == null) return "";

                if (!process.WaitForExit(timeoutMs))
                {
                    try { process.Kill(entireProcessTree: true); } catch { /* ya murio */ }
                    return "";                     // vacio = "adb no responde"
                }

                process.WaitForExit();             // asegura que se han volcado los datos
            }

            return File.Exists(temp) ? File.ReadAllText(temp, Encoding.UTF8) : "";
        }
        catch
        {
            return "";
        }
        finally
        {
            try { File.Delete(temp); } catch { /* nada */ }
        }
    }

    /// <summary>Id del primer movil conectado (vacio si no hay).</summary>
    public static string DeviceId(out bool adbResponded)
    {
        adbResponded = false;
        string output = Run("devices");
        if (output.Length == 0) return "";

        adbResponded = true;
        foreach (string line in Lines(output))
        {
            if (line.Contains("List of devices", StringComparison.Ordinal)) continue;
            if (line.Contains("unauthorized", StringComparison.OrdinalIgnoreCase)) continue;
            if (!line.Contains("device", StringComparison.OrdinalIgnoreCase)) continue;

            int cut = line.IndexOfAny(new[] { '\t', ' ' });
            return cut > 0 ? line[..cut] : line;
        }

        return "";
    }

    /// <summary>Modelo del movil ("Pixel 7"), vacio si no se puede leer.</summary>
    public static string Model(string deviceId) =>
        Trim(Run($"-s {deviceId} shell getprop ro.product.model"));

    /// <summary>Entradas de una carpeta del movil (las carpetas acaban en '/').</summary>
    public static List<string> Entries(string deviceId, string folder) =>
        Lines(Run($"-s {deviceId} shell ls -p {Quote(folder)}"));

    /// <summary>Subcarpetas de una carpeta del movil (sin la barra final).</summary>
    public static List<string> Subfolders(string deviceId, string folder)
    {
        var result = new List<string>();
        foreach (string entry in Entries(deviceId, folder))
        {
            if (entry.Length < 2 || !entry.EndsWith('/')) continue;
            result.Add(entry[..^1]);
        }

        return result;
    }

    /// <summary>Que archivos hay en la carpeta del movil y en que carpeta caen (reglas incluidas).</summary>
    public static List<PhoneFile> Plan(Options options, string deviceId, string folder)
    {
        var map = Planner.ExtensionMap(options);
        var plan = new List<PhoneFile>();

        // El movil no da la fecha del archivo sin otra llamada: las variables de fecha
        // ({año}, {mes}...) se resuelven con la fecha de hoy.
        DateTime now = DateTime.Now;

        foreach (string entry in Entries(deviceId, folder))
        {
            if (entry.Length == 0 || entry.EndsWith('/')) continue;   // de momento, solo archivos sueltos

            Rule? rule = RuleEngine.FirstMatch(options.CustomRules, entry);
            string target = rule != null
                ? RuleEngine.Resolve(rule, entry, now).Folder.Replace('\\', '/')
                : Planner.CategoryOfName(map, entry);

            plan.Add(new PhoneFile(entry, target));
        }

        return plan;
    }

    /// <summary>
    /// Crea las subcarpetas y mueve los archivos DENTRO del movil.
    /// Devuelve cuantos se movieron, cuantos fallaron y el estado por archivo.
    /// </summary>
    public static (int Moved, int Failed, Dictionary<string, bool> Results, string LogPath)
        ApplyPlan(string deviceId, string folder, IReadOnlyList<PhoneFile> plan,
                  Action<int, int>? progress = null)
    {
        var results = new Dictionary<string, bool>(StringComparer.Ordinal);

        if (plan.Count == 0) return (0, 0, results, "");

        var sh = new StringBuilder();
        sh.Append("cd ").Append(Quote(folder)).Append(" || exit 1\n");

        foreach (string relative in plan.Select(p => p.Folder).Distinct(StringComparer.Ordinal))
            sh.Append("mkdir -p ").Append(Quote(relative)).Append(" 2>/dev/null\n");

        foreach (var file in plan)
        {
            string name = Quote(file.Name);
            string target = Quote(file.Folder + "/" + file.Name);
            sh.Append("mv ").Append(name).Append(' ').Append(target)
              .Append(" 2>/dev/null && printf 'OK\\t%s\\n' ").Append(name)
              .Append(" || printf 'ERR\\t%s\\n' ").Append(name).Append('\n');
        }

        string output = RunScript(deviceId, sh.ToString(), "filenet_tarea.sh", 180_000);

        // Se interpretan las lineas OK / ERR
        foreach (var file in plan)
            results[file.Name] = false;           // por si no aparece en la salida

        foreach (string line in Lines(output))
        {
            if (line.StartsWith("OK\t", StringComparison.Ordinal))
                results[line[3..]] = true;
            else if (line.StartsWith("ERR\t", StringComparison.Ordinal))
                results[line[4..]] = false;
        }

        for (int i = 0; i < plan.Count; ++i)
            progress?.Invoke(i + 1, plan.Count);

        var done = plan.Where(p => results.TryGetValue(p.Name, out bool ok) && ok)
                       .Select(p => (p.Name, p.Folder))
                       .ToList();

        string logPath = "";
        if (done.Count > 0)
        {
            try { logPath = CsvLog.WritePhoneLog(folder, done); }
            catch { /* sin registro no hay deshacer, pero los archivos ya estan movidos */ }
        }

        int moved = results.Count(kv => kv.Value);
        return (moved, plan.Count - moved, results, logPath);
    }

    /// <summary>Devuelve a su sitio los archivos movidos segun un registro del PC.</summary>
    public static (int Restored, int Failed) UndoFromLog(string deviceId, string logPath)
    {
        var moves = CsvLog.ReadMoves(logPath);
        if (moves.Count == 0) return (0, 0);

        var sh = new StringBuilder();
        foreach (var (from, to) in moves)
        {
            string origin = Quote(from);
            sh.Append("mv ").Append(Quote(to)).Append(' ').Append(origin)
              .Append(" 2>/dev/null && printf 'OK\\t%s\\n' ").Append(origin)
              .Append(" || printf 'ERR\\t%s\\n' ").Append(origin).Append('\n');
        }

        // rmdir solo borra si la carpeta esta vacia: limpia las de categoria y las de fecha.
        var folders = moves
            .Select(m => RemoteFolder(m.To))
            .Where(f => f.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(f => f.Count(c => c == '/'));

        foreach (string folder in folders)
            sh.Append("rmdir ").Append(Quote(folder)).Append(" 2>/dev/null\n");

        string output = RunScript(deviceId, sh.ToString(), "filenet_deshacer.sh", 180_000);

        int ok = 0, err = 0;
        foreach (string line in Lines(output))
        {
            if (line.StartsWith("OK\t", StringComparison.Ordinal)) ++ok;
            else if (line.StartsWith("ERR\t", StringComparison.Ordinal)) ++err;
        }

        return (ok, err);
    }

    /// <summary>Carpeta que contiene una ruta del movil ("/sdcard/Download/A/f.jpg" -> "/sdcard/Download/A").</summary>
    private static string RemoteFolder(string path)
    {
        int slash = path.LastIndexOf('/');
        return slash > 0 ? path[..slash] : "";
    }

    /// <summary>Sube el script, lo ejecuta y lo borra del movil.</summary>
    private static string RunScript(string deviceId, string content, string scriptName, int runTimeoutMs)
    {
        string local = Path.Combine(Path.GetTempPath(), "filenest-" + Guid.NewGuid().ToString("N") + ".sh");
        string remote = "/data/local/tmp/" + scriptName;

        try
        {
            // El script DEBE ir con saltos de linea LF, no CRLF.
            File.WriteAllText(local, content.Replace("\r\n", "\n"), new UTF8Encoding(false));

            Run($"-s {deviceId} push \"{local}\" {remote}", 60_000);
            string output = Run($"-s {deviceId} shell sh {remote}", runTimeoutMs);
            Run($"-s {deviceId} shell rm -f {remote}");
            return output;
        }
        catch
        {
            return "";
        }
        finally
        {
            try { File.Delete(local); } catch { /* nada */ }
        }
    }

    /// <summary>Comillas simples para la shell del movil.</summary>
    public static string Quote(string text) => "'" + text.Replace("'", "'\\''") + "'";

    /// <summary>Quita espacios y saltos de linea de los extremos.</summary>
    public static string Trim(string text) => text.Trim(' ', '\r', '\n', '\t');

    private static List<string> Lines(string text)
    {
        var lines = new List<string>();
        foreach (string raw in text.Split('\n'))
        {
            string line = Trim(raw);
            if (line.Length > 0) lines.Add(line);
        }

        return lines;
    }
}
