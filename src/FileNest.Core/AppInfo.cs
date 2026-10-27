namespace FileNest.Core;

/// <summary>Datos fijos del producto.</summary>
public static class AppInfo
{
    /// <summary>Repositorio de GitHub que se usa para las actualizaciones.</summary>
    public const string Repo = "AdriTg00/FileNest";

    public const string Name = "FileNest";

    public const string SiteUrl = "https://github.com/" + Repo;

    /// <summary>
    /// Version del producto ("2.0.0"). Viene de Directory.Build.props a traves de
    /// AssemblyVersion, asi que no hay ningun numero escrito a mano en el codigo.
    /// </summary>
    public static string Version { get; } = ReadVersion();

    /// <summary>Version ya lista para mostrar ("v2.0.0").</summary>
    public static string Tag => "v" + Version;

    private static string ReadVersion()
    {
        Version? v = typeof(AppInfo).Assembly.GetName().Version;
        if (v == null) return "0.0.0";
        return $"{v.Major}.{v.Minor}.{v.Build}";
    }
}
