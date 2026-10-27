namespace FileNest.Core;

/// <summary>
/// Motor de simulacion: decide que archivo va a que carpeta, SIN tocar el disco
/// (solo lee). Es el equivalente a planificar() de la version en C++.
/// </summary>
public static class Planner
{
    private const int MaxDepth = 64;

    public static Plan Build(Options options)
    {
        var ctx = new Context(options);
        Collect(ctx, options.Directory, 0);

        var plan = new Plan
        {
            Analyzed = ctx.Analyzed,
            Skipped = ctx.Skipped,
        };
        plan.Moves.AddRange(ctx.Moves);
        foreach (var m in plan.Moves)
            plan.ByCategory[m.Category] = plan.ByCategory.GetValueOrDefault(m.Category) + 1;

        return plan;
    }

    private sealed class Context
    {
        public Context(Options options)
        {
            Options = options;

            // Nombres de categoria de los DOS idiomas: nunca se entra en una de ellas.
            foreach (string name in Categories.AllNames())
                CategoryNames.Add(name);

            foreach (var cat in Categories.Default)
                foreach (string ext in cat.Extensions)
                    ExtensionToCategory["." + ext] = cat.Name;

            // Las reglas propias mandan sobre la tabla por defecto.
            foreach (var rule in options.CustomRules)
            {
                ExtensionToCategory[NormalizeExtension(rule.Extension)] = rule.Category;
                CategoryNames.Add(rule.Category);
            }
        }

        public Options Options { get; }
        public Dictionary<string, string> ExtensionToCategory { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> CategoryNames { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Reserved { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<MoveItem> Moves { get; } = new();
        public long Analyzed { get; set; }
        public long Skipped { get; set; }
    }

    /// <summary>".PDF" -> ".pdf"; "pdf" -> ".pdf".</summary>
    public static string NormalizeExtension(string extension)
    {
        string ext = extension.Trim().ToLowerInvariant();
        return ext.StartsWith('.') ? ext : "." + ext;
    }

    /// <summary>Categoria de un nombre de archivo segun su extension (lo usa el movil).</summary>
    public static string CategoryOfName(IReadOnlyDictionary<string, string> map, string fileName)
    {
        string ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext.Length == 0) return Categories.OtherName;
        return map.TryGetValue(ext, out string? cat) ? cat : Categories.OtherName;
    }

    /// <summary>Mapa extension -> categoria, incluidas las reglas propias.</summary>
    public static Dictionary<string, string> ExtensionMap(Options options)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var cat in Categories.Default)
            foreach (string ext in cat.Extensions)
                map["." + ext] = cat.Name;

        foreach (var rule in options.CustomRules)
            map[NormalizeExtension(rule.Extension)] = rule.Category;

        return map;
    }

    private static void Collect(Context ctx, string dir, int depth)
    {
        if (depth > MaxDepth) return;

        IEnumerable<FileSystemInfo> entries;
        try
        {
            var info = new DirectoryInfo(dir);
            entries = info.EnumerateFileSystemInfos("*", new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = false,
                AttributesToSkip = 0,          // los ocultos se cuentan a mano (como "omitidos")
                ReturnSpecialDirectories = false,
                MatchType = MatchType.Simple,
            });
        }
        catch
        {
            return;                            // sin permisos / no existe: se ignora
        }

        foreach (var entry in entries)
        {
            FileAttributes attrs;
            try
            {
                attrs = entry.Attributes;
            }
            catch
            {
                continue;
            }

            if ((attrs & FileAttributes.Directory) != 0)
            {
                if (!ctx.Options.Recursive) continue;
                if ((attrs & FileAttributes.ReparsePoint) != 0) continue;

                string dirName = entry.Name;
                if (dirName.Length == 0 || dirName[0] == '.') continue;
                if (ctx.CategoryNames.Contains(dirName)) continue;

                Collect(ctx, entry.FullName, depth + 1);
                continue;
            }

            string name = entry.Name;
            if (IsSkippedName(name)) continue;

            if ((attrs & (FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint)) != 0)
            {
                ++ctx.Skipped;
                continue;
            }

            ++ctx.Analyzed;

            string category = CategoryOfName(ctx.ExtensionToCategory, name);
            string target = Path.Combine(dir, category);

            if (ctx.Options.ByDate)
            {
                DateTime written = File.GetLastWriteTime(entry.FullName);
                target = Path.Combine(target, written.Year.ToString("0000"), written.Month.ToString("00"));
            }

            string destination = ReserveUnique(ctx, Path.Combine(target, name));
            ctx.Moves.Add(new MoveItem(entry.FullName, destination, category));
        }
    }

    /// <summary>Si ya existe, prueba "nombre (1).ext", "nombre (2).ext"...</summary>
    private static string ReserveUnique(Context ctx, string destination)
    {
        if (IsFree(ctx, destination))
        {
            ctx.Reserved.Add(destination);
            return destination;
        }

        string? directory = Path.GetDirectoryName(destination);
        string stem = Path.GetFileNameWithoutExtension(destination);
        string extension = Path.GetExtension(destination);

        for (int i = 1; i < 1_000_000; ++i)
        {
            string candidate = Path.Combine(directory ?? "", $"{stem} ({i}){extension}");
            if (IsFree(ctx, candidate))
            {
                ctx.Reserved.Add(candidate);
                return candidate;
            }
        }

        ctx.Reserved.Add(destination);
        return destination;
    }

    private static bool IsFree(Context ctx, string path)
    {
        if (ctx.Reserved.Contains(path)) return false;
        return !File.Exists(path) && !Directory.Exists(path);
    }

    /// <summary>Archivos que nunca se tocan: los que empiezan por punto y la basura de Windows.</summary>
    public static bool IsSkippedName(string name)
    {
        if (name.Length == 0 || name[0] == '.') return true;
        return name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)
            || name.Equals("thumbs.db", StringComparison.OrdinalIgnoreCase);
    }
}
