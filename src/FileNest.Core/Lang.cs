using System.Runtime.InteropServices;

namespace FileNest.Core;

/// <summary>
/// Idioma de la interfaz.
///   - Por defecto: espanol si Windows esta en espanol; ingles en cualquier otro caso
///     (se mira el idioma de la interfaz del usuario, no el del sistema).
///   - Se puede forzar con la variable de entorno: FILENEST_LANG=es | en
/// </summary>
public static class Lang
{
    private const int LangSpanish = 0x0A;
    private const int PrimaryMask = 0x3FF;

    /// <summary>True si la interfaz va en espanol.</summary>
    public static bool IsSpanish { get; } = Detect();

    /// <summary>Codigo de dos letras: "es" o "en" (lo usa Organizador.bat).</summary>
    public static string Code => IsSpanish ? "es" : "en";

    /// <summary>Texto de la interfaz: T("espanol", "english").</summary>
    public static string T(string es, string en) => IsSpanish ? es : en;

    private static bool Detect()
    {
        string? forced = Environment.GetEnvironmentVariable("FILENEST_LANG");
        if (!string.IsNullOrWhiteSpace(forced))
        {
            if (forced.StartsWith("es", StringComparison.OrdinalIgnoreCase)) return true;
            if (forced.StartsWith("en", StringComparison.OrdinalIgnoreCase)) return false;
        }

        return (GetUserDefaultUILanguage() & PrimaryMask) == LangSpanish;
    }

    [DllImport("kernel32.dll")]
    private static extern ushort GetUserDefaultUILanguage();
}
