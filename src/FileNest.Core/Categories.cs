namespace FileNest.Core;

/// <summary>
/// Una categoria (carpeta destino). <paramref name="AnsiColor"/> es el color que
/// usa la version de consola; la ventana colorea las filas por estado, no por categoria.
/// </summary>
public sealed record CategoryDef(string Name, string Icon, string AnsiColor, string[] Extensions);

/// <summary>
/// Tabla de categorias por defecto, en los dos idiomas.
/// Las extensiones son las mismas en ambos; solo cambia el nombre de la carpeta.
/// </summary>
public static class Categories
{
    private static readonly CategoryDef[] Spanish =
    {
        new("Imágenes", "📷", "\u001b[96m",
            new[] { "jpg","jpeg","png","gif","bmp","webp","tif","tiff","svg","ico","heic","raw","cr2","nef","psd","avif" }),
        new("Documentos", "📄", "\u001b[94m",
            new[] { "pdf","doc","docx","txt","rtf","odt","xls","xlsx","ods","ppt","pptx","odp","csv","md","epub" }),
        new("Vídeos", "🎬", "\u001b[95m",
            new[] { "mp4","mkv","avi","mov","wmv","flv","webm","m4v","mpg","mpeg","3gp","mts","m2ts" }),
        new("Audio", "🎵", "\u001b[92m",
            new[] { "mp3","wav","flac","aac","ogg","wma","m4a","opus","mid","midi" }),
        new("Comprimidos", "📦", "\u001b[93m",
            new[] { "zip","rar","7z","tar","gz","bz2","xz","iso","cab","zst" }),
        new("Programas", "🔧", "\u001b[91m",
            new[] { "exe","msi","msix","appx","bat","cmd","com","apk","deb","rpm" }),
        new("Código", "💻", "\u001b[38;5;208m",
            new[] { "c","cpp","cc","h","hpp","cs","java","py","js","ts","html","htm","css","php","rs","go","rb","sh","json","xml","yml","yaml","sql","ini","toml" }),
        new("Fuentes", "🔠", "\u001b[38;5;213m",
            new[] { "ttf","otf","woff","woff2" }),
    };

    private static readonly CategoryDef[] English =
    {
        new("Images", "📷", "\u001b[96m",
            new[] { "jpg","jpeg","png","gif","bmp","webp","tif","tiff","svg","ico","heic","raw","cr2","nef","psd","avif" }),
        new("Documents", "📄", "\u001b[94m",
            new[] { "pdf","doc","docx","txt","rtf","odt","xls","xlsx","ods","ppt","pptx","odp","csv","md","epub" }),
        new("Videos", "🎬", "\u001b[95m",
            new[] { "mp4","mkv","avi","mov","wmv","flv","webm","m4v","mpg","mpeg","3gp","mts","m2ts" }),
        new("Audio", "🎵", "\u001b[92m",
            new[] { "mp3","wav","flac","aac","ogg","wma","m4a","opus","mid","midi" }),
        new("Archives", "📦", "\u001b[93m",
            new[] { "zip","rar","7z","tar","gz","bz2","xz","iso","cab","zst" }),
        new("Programs", "🔧", "\u001b[91m",
            new[] { "exe","msi","msix","appx","bat","cmd","com","apk","deb","rpm" }),
        new("Code", "💻", "\u001b[38;5;208m",
            new[] { "c","cpp","cc","h","hpp","cs","java","py","js","ts","html","htm","css","php","rs","go","rb","sh","json","xml","yml","yaml","sql","ini","toml" }),
        new("Fonts", "🔠", "\u001b[38;5;213m",
            new[] { "ttf","otf","woff","woff2" }),
    };

    /// <summary>Categorias del idioma actual.</summary>
    public static IReadOnlyList<CategoryDef> Default => Lang.IsSpanish ? Spanish : English;

    /// <summary>Carpeta para lo que no encaja en ninguna categoria.</summary>
    public static string OtherName => Lang.IsSpanish ? "Otros" : "Other";

    /// <summary>Icono (emoji) de una categoria.</summary>
    public static string IconOf(string category)
    {
        if (category == OtherName) return "📎";
        foreach (var d in Default)
            if (d.Name == category) return d.Icon;
        return "🏷";
    }

    /// <summary>Codigo ANSI de color de una categoria (solo consola).</summary>
    public static string AnsiOf(string category)
    {
        if (category == OtherName) return "\u001b[90m";
        foreach (var d in Default)
            if (d.Name == category) return d.AnsiColor;
        return "\u001b[36m";
    }

    /// <summary>
    /// Nombres de carpeta de categoria en LOS DOS idiomas (incluido "Otros"/"Other").
    /// Asi nunca se entra en una carpeta de categoria, aunque cambie el idioma.
    /// </summary>
    public static HashSet<string> AllNames()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in Spanish) set.Add(d.Name);
        foreach (var d in English) set.Add(d.Name);
        set.Add("Otros");
        set.Add("Other");
        return set;
    }
}
