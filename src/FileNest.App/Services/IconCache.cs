using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FileNest.App.Services;

/// <summary>
/// Iconos del Explorador de Windows para cada tipo de archivo.
///
/// Se pide uno por EXTENSION (no por archivo): SHGetFileInfo es una llamada cara
/// al shell y con 1200 archivos pesaba segundos (fue un bug real en la version C++).
/// </summary>
public static class IconCache
{
    private static readonly Dictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);

    private const uint ShgfiIcon = 0x000000100;
    private const uint ShgfiSmallIcon = 0x000000001;
    private const uint ShgfiUseFileAttributes = 0x000000010;
    private const uint FileAttributeNormal = 0x00000080;

    public static ImageSource? ForFile(string path)
    {
        string extension = Path.GetExtension(path);
        string key = extension.Length == 0 ? "" : extension.ToLowerInvariant();

        if (Cache.TryGetValue(key, out var cached)) return cached;

        // Un nombre falso con la extension: SHGFI_USEFILEATTRIBUTES evita tocar el disco.
        string fake = key.Length == 0 ? "archivo" : "archivo" + key;
        ImageSource? icon = Load(fake, FileAttributeNormal);
        Cache[key] = icon;
        return icon;
    }

    public static ImageSource? ForFolder()
    {
        if (Cache.TryGetValue("<carpeta>", out var cached)) return cached;
        ImageSource? icon = Load("carpeta", 0x00000010 /* FILE_ATTRIBUTE_DIRECTORY */);
        Cache["<carpeta>"] = icon;
        return icon;
    }

    private static ImageSource? Load(string fakeName, uint attributes)
    {
        var info = new ShFileInfo();
        IntPtr result = SHGetFileInfo(fakeName, attributes, ref info, (uint)Marshal.SizeOf<ShFileInfo>(),
                                      ShgfiIcon | ShgfiSmallIcon | ShgfiUseFileAttributes);

        if (result == IntPtr.Zero || info.Icon == IntPtr.Zero) return null;

        try
        {
            var source = Imaging.CreateBitmapSourceFromHIcon(
                info.Icon, Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(16, 16));
            source.Freeze();
            return source;
        }
        catch
        {
            return null;
        }
        finally
        {
            DestroyIcon(info.Icon);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShFileInfo
    {
        public IntPtr Icon;
        public int IconIndex;
        public uint Attributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string DisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string TypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(string path, uint fileAttributes, ref ShFileInfo info,
                                               uint infoSize, uint flags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr icon);
}
