using System.IO;
using System.Runtime.InteropServices;
using FileNest.Core;

namespace FileNest.App.Services;

/// <summary>
/// Una sola instancia de la ventana a la vez.
///
/// Si el usuario abre FileNest otra vez, la instancia que ya esta corriendo se
/// trae al frente y la nueva se cierra. Si la segunda vez se abrio con una
/// carpeta (arrastrar sobre el acceso directo, "Abrir con..."), esa carpeta se
/// le pasa a la que ya estaba abierta con un fichero temporal.
/// </summary>
public static class SingleInstance
{
    private const string MutexName = @"Local\FileNest.SingleInstance";
    private const string MessageName = "FileNest.Activate";
    private const string PendingFile = "pendiente.txt";

    private static readonly IntPtr Broadcast = new(0xFFFF);   // HWND_BROADCAST

    /// <summary>Mensaje que reciben todas las ventanas al intentar abrir otra instancia.</summary>
    public static int ActivateMessage { get; } = (int)RegisterWindowMessage(MessageName);

    /// <summary>
    /// Devuelve el mutex si esta es la primera instancia, o null si ya hay una abierta.
    /// </summary>
    public static Mutex? Acquire()
    {
        var mutex = new Mutex(initiallyOwned: true, MutexName, out bool created);

        if (created) return mutex;

        mutex.Dispose();
        return null;
    }

    /// <summary>Avisa a la instancia que ya esta abierta (y le deja la carpeta, si hay).</summary>
    public static void NotifyAlreadyRunning(string? folder)
    {
        if (!string.IsNullOrWhiteSpace(folder))
        {
            try
            {
                Directory.CreateDirectory(Paths.DataDirectory);
                File.WriteAllText(Path.Combine(Paths.DataDirectory, PendingFile), folder);
            }
            catch
            {
                // sin carpeta: al menos se trae al frente
            }
        }

        if (ActivateMessage != 0)
            PostMessage(Broadcast, (uint)ActivateMessage, IntPtr.Zero, IntPtr.Zero);
    }

    /// <summary>Carpeta que dejo la segunda instancia (se borra al leerla).</summary>
    public static string? TakePendingFolder()
    {
        string path = Path.Combine(Paths.DataDirectory, PendingFile);

        try
        {
            if (!File.Exists(path)) return null;

            string folder = File.ReadAllText(path).Trim();
            File.Delete(path);
            return folder.Length > 0 ? folder : null;
        }
        catch
        {
            return null;
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterWindowMessage(string message);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint message, IntPtr wParam, IntPtr lParam);
}
