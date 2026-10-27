namespace FileNest.Core;

/// <summary>Deshace una ejecucion usando su registro CSV.</summary>
public static class Undoer
{
    /// <summary>
    /// Devuelve cada archivo a su sitio. <paramref name="progress"/> recibe (rutaOrigen, ok).
    /// Igual que en la version en C++: si el destino ya no existe o el origen ya existe,
    /// ese movimiento se omite (no se pisa nada).
    ///
    /// Al terminar, borra las carpetas de categoria (y de fecha) que se hayan quedado
    /// vacias, para que la carpeta quede como estaba. Nunca borra la carpeta organizada.
    /// </summary>
    public static UndoResult Undo(string logPath, Action<string, bool>? progress = null,
                                  bool cleanEmptyFolders = true)
    {
        var result = new UndoResult();
        if (!File.Exists(logPath)) return result;

        var moves = CsvLog.ReadMoves(logPath);

        foreach (var (from, to) in moves)
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

        // Solo si se ha podido restaurar algo: si no, las carpetas siguen en uso.
        if (cleanEmptyFolders && result.Restored > 0)
            DeleteEmptyFolders(moves.Select(m => Path.GetDirectoryName(m.To)), Path.GetDirectoryName(logPath));

        return result;
    }

    /// <summary>
    /// Borra las carpetas vacias de la lista, de la mas profunda a la menos
    /// (asi "Cat\2026\10" desaparece entera) y sube por los padres hasta la
    /// carpeta organizada, que nunca se borra.
    /// </summary>
    private static void DeleteEmptyFolders(IEnumerable<string?> folders, string? organizedFolder)
    {
        string basePath = "";
        try { basePath = Path.GetFullPath(organizedFolder ?? "").TrimEnd(Path.DirectorySeparatorChar); }
        catch { /* ruta rara */ }

        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string? folder in folders)
        {
            string? current = folder;

            while (!string.IsNullOrWhiteSpace(current))
            {
                string full;
                try { full = Path.GetFullPath(current).TrimEnd(Path.DirectorySeparatorChar); }
                catch { break; }

                // Fuera de la carpeta organizada: no se toca nada.
                if (basePath.Length > 0 &&
                    !full.StartsWith(basePath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    break;

                candidates.Add(full);
                current = Path.GetDirectoryName(current);
            }
        }

        foreach (string folder in candidates.OrderByDescending(f => f.Count(c => c == Path.DirectorySeparatorChar)))
        {
            try
            {
                if (string.Equals(folder, basePath, StringComparison.OrdinalIgnoreCase)) continue;
                if (!Directory.Exists(folder)) continue;
                if (Directory.EnumerateFileSystemEntries(folder).Any()) continue;

                Directory.Delete(folder);
            }
            catch
            {
                // Ocupada, sin permisos o ya no existe: no es un problema.
            }
        }
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
