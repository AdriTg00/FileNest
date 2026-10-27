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
