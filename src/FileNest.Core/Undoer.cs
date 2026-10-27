namespace FileNest.Core;

/// <summary>Deshace una ejecucion usando su registro CSV.</summary>
public static class Undoer
{
    /// <summary>
    /// Devuelve cada archivo a su sitio. <paramref name="progress"/> recibe (rutaOrigen, ok).
    /// Igual que en la version en C++: si el destino ya no existe o el origen ya existe,
    /// ese movimiento se omite (no se pisa nada).
    /// </summary>
    public static UndoResult Undo(string logPath, Action<string, bool>? progress = null)
    {
        var result = new UndoResult();
        if (!File.Exists(logPath)) return result;

        foreach (var (from, to) in CsvLog.ReadMoves(logPath))
        {
            try
            {
                if (!File.Exists(to))
                {
                    ++result.Skipped;
                    continue;
                }

                if (File.Exists(from))
                {
                    ++result.Skipped;
                    continue;
                }

                string? parent = Path.GetDirectoryName(from);
                if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);

                File.Move(to, from);
                ++result.Restored;
                progress?.Invoke(from, true);
            }
            catch
            {
                ++result.Errors;
                progress?.Invoke(from, false);
            }
        }

        return result;
    }

    /// <summary>Registros de una carpeta, del mas reciente al mas antiguo.</summary>
    public static List<string> FindLogs(string folder)
    {
        var logs = new List<string>();
        if (!Directory.Exists(folder)) return logs;

        try
        {
            foreach (string path in Directory.EnumerateFiles(folder, Paths.LogPrefix + "*.csv"))
                logs.Add(path);
        }
        catch
        {
            return logs;
        }

        logs.Sort(StringComparer.Ordinal);
        logs.Reverse();
        return logs;
    }
}
