namespace FileNest.Core;

/// <summary>Rutas de trabajo de la aplicacion.</summary>
public static class Paths
{
    /// <summary>Marca de tiempo de los registros: 20261027-183045.</summary>
    public static string NowStamp() => DateTime.Now.ToString("yyyyMMdd-HHmmss");

    /// <summary>Carpeta de datos: %LOCALAPPDATA%\FileNest (se crea si no existe).</summary>
    public static string DataDirectory
    {
        get
        {
            string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = Path.Combine(baseDir, "FileNest");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    /// <summary>Carpeta por defecto: Descargas, o el perfil si no existe.</summary>
    public static string DefaultTargetDirectory
    {
        get
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string downloads = Path.Combine(home, "Downloads");
            return Directory.Exists(downloads) ? downloads : home;
        }
    }

    /// <summary>Nombre del registro que permite deshacer una ejecucion.</summary>
    public const string LogPrefix = ".organizador-log-";

    /// <summary>Ruta relativa (sin tocar el disco) para mostrar en la lista.</summary>
    public static string RelativeTo(string fullPath, string baseDir)
    {
        try
        {
            string rel = Path.GetRelativePath(baseDir, fullPath);
            return rel == "." ? fullPath : rel;
        }
        catch
        {
            return fullPath;
        }
    }

    /// <summary>Tamanio legible: 3 B, 12,4 KB, 1,2 GB...</summary>
    public static string HumanSize(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = bytes;
        int i = 0;
        while (value >= 1024.0 && i < units.Length - 1)
        {
            value /= 1024.0;
            ++i;
        }

        return i == 0
            ? $"{bytes} B"
            : string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:0.#} {1}", value, units[i]);
    }
}
