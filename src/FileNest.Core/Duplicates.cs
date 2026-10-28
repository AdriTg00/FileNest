using System.Security.Cryptography;

namespace FileNest.Core;

/// <summary>Un archivo candidato dentro de un grupo de duplicados.</summary>
public sealed record DuplicateFile(string Path, long Size, DateTime Written);

/// <summary>Archivos con el mismo contenido.</summary>
public sealed class DuplicateGroup
{
    public DuplicateGroup(long size, List<DuplicateFile> files)
    {
        Size = size;
        Files = files;
    }

    public long Size { get; }
    public List<DuplicateFile> Files { get; }

    /// <summary>Lo que se recuperaria dejando una sola copia.</summary>
    public long WastedBytes => Size * (Files.Count - 1);
}

public sealed record DuplicateProgress(int Scanned, int ToHash, int Hashed);

public sealed class DuplicateReport
{
    public DuplicateReport(List<DuplicateGroup> groups, int scanned, int unreadable = 0)
    {
        Groups = groups;
        Scanned = scanned;
        Unreadable = unreadable;
    }

    /// <summary>Grupos con 2 o mas archivos iguales (de mas grande a mas pequeno).</summary>
    public List<DuplicateGroup> Groups { get; }

    /// <summary>Archivos analizados (los que tienen tamano).</summary>
    public int Scanned { get; }

    /// <summary>Archivos que no se pudieron leer al calcular el hash (bloqueados, sin permisos).</summary>
    public int Unreadable { get; }

    /// <summary>Archivos que sobran (total de copias menos una por grupo).</summary>
    public int ExtraFiles => Groups.Sum(group => group.Files.Count - 1);

    public long WastedBytes => Groups.Sum(group => group.WastedBytes);

    public bool Any => Groups.Count > 0;
}

/// <summary>
/// Busca archivos repetidos por CONTENIDO (nunca por nombre).
///
/// Para no leer todo el disco: primero se agrupa por tamano y solo se calcula el hash
/// de los archivos que comparten tamano con otro. Despues se comparan los hashes.
/// Mover nunca borra: los sobrantes van a una carpeta "Duplicados" y quedan en el
/// registro, asi que se pueden deshacer como cualquier otra organizacion.
/// </summary>
public static class Duplicates
{
    /// <summary>
    /// Nombres de la carpeta de cuarentena en LOS DOS idiomas. Se excluye siempre
    /// del escaneo y de la organizacion recursiva: si no, al volver a analizar la
    /// carpeta se encontrarian otra vez las copias que ya se apartaron.
    /// </summary>
    public static readonly string[] FolderNames = { "Duplicados", "Duplicates" };

    /// <summary>Nombre de la carpeta destino (en el idioma de la interfaz).</summary>
    public static string FolderName => Lang.T("Duplicados", "Duplicates");

    /// <summary>True para "Duplicados" o "Duplicates", en cualquier idioma.</summary>
    public static bool IsFolderName(string name)
    {
        foreach (string candidate in FolderNames)
            if (string.Equals(name, candidate, StringComparison.OrdinalIgnoreCase)) return true;

        return false;
    }

    public static DuplicateReport Find(Options options, IProgress<DuplicateProgress>? progress = null,
                                       CancellationToken cancellationToken = default)
    {
        var files = new List<DuplicateFile>();
        int unreadable = 0;

        foreach (string path in Enumerate(options.Directory, options.Recursive))
        {
            cancellationToken.ThrowIfCancellationRequested();

            long size;
            DateTime written;
            try
            {
                var info = new FileInfo(path);
                size = info.Length;
                written = info.LastWriteTime;
            }
            catch
            {
                continue;
            }

            if (size == 0) continue;          // dos archivos vacios no son un problema
            files.Add(new DuplicateFile(path, size, written));
        }

        // Solo merece la pena leer los que comparten tamano con otro.
        var sameSize = files
            .GroupBy(file => file.Size)
            .Where(group => group.Count() > 1)
            .ToList();

        int toHash = sameSize.Sum(group => group.Count());
        int hashed = 0;

        var groups = new List<DuplicateGroup>();

        foreach (var sizeGroup in sameSize)
        {
            var byHash = new Dictionary<string, List<DuplicateFile>>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in sizeGroup)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string? hash = TryHash(file.Path);
                ++hashed;
                progress?.Report(new DuplicateProgress(files.Count, toHash, hashed));

                if (hash == null)
                {
                    ++unreadable;                   // bloqueado, sin permisos... se informa
                    continue;
                }

                if (!byHash.TryGetValue(hash, out var list))
                {
                    list = new List<DuplicateFile>();
                    byHash[hash] = list;
                }

                list.Add(file);
            }

            foreach (var list in byHash.Values)
            {
                if (list.Count < 2) continue;

                // Se conserva el mas antiguo; el resto son los que sobran.
                list.Sort((a, b) => a.Written.CompareTo(b.Written));
                groups.Add(new DuplicateGroup(list[0].Size, list));
            }
        }

        groups.Sort((a, b) => b.WastedBytes.CompareTo(a.WastedBytes));
        return new DuplicateReport(groups, files.Count, unreadable);
    }

    /// <summary>
    /// Mueve los archivos indicados a "Duplicados" (creando nombres nuevos si hace falta)
    /// y escribe el registro para poder deshacerlo.
    /// </summary>
    public static ApplyResult MoveToFolder(Options options, IEnumerable<string> paths)
    {
        string folder = Path.Combine(options.Directory, FolderName);
        var plan = new Plan();
        var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string path in paths)
        {
            string target = UniquePath(folder, Path.GetFileName(path), reserved);
            reserved.Add(target);
            plan.Moves.Add(new MoveItem(path, target, FolderName));
        }

        return Applier.Apply(plan, options.Directory);
    }

    private static string UniquePath(string folder, string fileName, HashSet<string> reserved)
    {
        string candidate = Path.Combine(folder, fileName);
        if (!File.Exists(candidate) && !reserved.Contains(candidate)) return candidate;

        string stem = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);

        for (int i = 1; i < 1_000_000; ++i)
        {
            candidate = Path.Combine(folder, $"{stem} ({i}){extension}");
            if (!File.Exists(candidate) && !reserved.Contains(candidate)) return candidate;
        }

        return Path.Combine(folder, fileName);
    }

    private static string? TryHash(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                                              1024 * 1024, FileOptions.SequentialScan);
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(stream);
            return Convert.ToHexString(hash);
        }
        catch
        {
            return null;                      // bloqueado, sin permisos... se ignora
        }
    }

    /// <summary>Archivos candidatos: los mismos que organizaria FileNest (sin ocultos ni basura).</summary>
    private static IEnumerable<string> Enumerate(string folder, bool recursive)
    {
        var pending = new Stack<string>();
        pending.Push(folder);

        while (pending.Count > 0)
        {
            string current = pending.Pop();

            IEnumerable<FileSystemInfo> entries;
            try
            {
                entries = new DirectoryInfo(current).EnumerateFileSystemInfos("*", new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = false,
                    AttributesToSkip = 0,
                    ReturnSpecialDirectories = false,
                    MatchType = MatchType.Simple,
                });
            }
            catch
            {
                continue;
            }

            foreach (var entry in entries)
            {
                FileAttributes attributes;
                try
                {
                    attributes = entry.Attributes;
                }
                catch
                {
                    continue;
                }

                if ((attributes & FileAttributes.Directory) != 0)
                {
                    if (!recursive) continue;
                    if ((attributes & FileAttributes.ReparsePoint) != 0) continue;

                    string name = entry.Name;
                    if (name.Length == 0 || name[0] == '.') continue;
                    if (IsFolderName(name)) continue;      // la cuarentena no se vuelve a mirar

                    pending.Push(entry.FullName);
                    continue;
                }

                if (Planner.IsSkippedName(entry.Name)) continue;
                if ((attributes & (FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint)) != 0) continue;

                yield return entry.FullName;
            }
        }
    }
}
