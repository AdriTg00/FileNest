using Microsoft.Win32;

namespace FileNest.Core;

/// <summary>
/// Preferencias en HKCU\Software\FileNest.
/// Se mantienen las MISMAS claves que la version en C++ para no perder nada
/// al actualizar: Tema (0/1), UltimaCarpeta, UltimaCarpetaMovil.
/// </summary>
public static class Settings
{
    private const string KeyPath = @"Software\FileNest";

    private const string ThemeValue = "Tema";
    private const string LastFolderValue = "UltimaCarpeta";
    private const string LastPhoneFolderValue = "UltimaCarpetaMovil";
    private const string WindowBoundsValue = "Ventana";
    private const string WindowMaximizedValue = "VentanaMaximizada";
    private const string WatchingValue = "Vigilando";

    /// <summary>0 = claro, 1 = oscuro.</summary>
    public static int Theme
    {
        get => ReadInt(ThemeValue, 0);
        set => WriteInt(ThemeValue, value);
    }

    public static string LastFolder
    {
        get => ReadString(LastFolderValue);
        set => WriteString(LastFolderValue, value);
    }

    public static string LastPhoneFolder
    {
        get => ReadString(LastPhoneFolderValue);
        set => WriteString(LastPhoneFolderValue, value);
    }

    /// <summary>Tamano y posicion de la ventana: "izquierda,arriba,ancho,alto" (vacio = por defecto).</summary>
    public static string WindowBounds
    {
        get => ReadString(WindowBoundsValue);
        set => WriteString(WindowBoundsValue, value);
    }

    public static bool WindowMaximized
    {
        get => ReadInt(WindowMaximizedValue, 0) == 1;
        set => WriteInt(WindowMaximizedValue, value ? 1 : 0);
    }

    /// <summary>Si el modo vigilancia estaba activado la ultima vez.</summary>
    public static bool Watching
    {
        get => ReadInt(WatchingValue, 0) == 1;
        set => WriteInt(WatchingValue, value ? 1 : 0);
    }

    private static int ReadInt(string name, int fallback)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            object? value = key?.GetValue(name);
            return value is int i ? i : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static void WriteInt(string name, int value)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(KeyPath, writable: true);
            key?.SetValue(name, value, RegistryValueKind.DWord);
        }
        catch
        {
            // Sin permisos de registro: se sigue funcionando, solo no se recuerda.
        }
    }

    private static string ReadString(string name)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            return key?.GetValue(name) as string ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static void WriteString(string name, string value)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(KeyPath, writable: true);
            key?.SetValue(name, value, RegistryValueKind.String);
        }
        catch
        {
            // idem
        }
    }
}
