using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using FileNest.App.Services;

namespace FileNest.App.Services;

/// <summary>Pinta la barra de titulo en oscuro (DWM) y la mantiene al dia con el tema.</summary>
public static class TitleBar
{
    private const int DwmwaUseImmersiveDarkMode = 20;      // Windows 10 20H1+
    private const int DwmwaUseImmersiveDarkModeLegacy = 19;

    public static void Attach(Window window)
    {
        void Update() => Apply(window, ThemeManager.Current == ThemeManager.Dark);

        window.SourceInitialized += (_, _) => Update();
        ThemeManager.Changed += Update;
        window.Closed += (_, _) => ThemeManager.Changed -= Update;
    }

    private static void Apply(Window window, bool dark)
    {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;

        int value = dark ? 1 : 0;
        if (DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref value, sizeof(int)) != 0)
            DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkModeLegacy, ref value, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
