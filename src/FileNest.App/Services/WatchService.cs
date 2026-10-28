using System.IO;
using System.Windows.Threading;
using FileNest.Core;

namespace FileNest.App.Services;

/// <summary>
/// Vigila una carpeta y organiza lo que llega, cuando lleva un rato quieto.
///
/// - FileSystemWatcher solo sirve para "algo ha cambiado": 10 segundos despues se
///   hace una pasada (asi una descarga que escribe tropecientas veces no dispara
///   tropecientas pasadas).
/// - Cada 30 segundos hay una pasada de seguridad por si se pierde algun evento.
/// - Solo se toca lo que llego DESPUES de activar la vigilancia y lleva 2 minutos
///   sin cambios (Watcher.Sweep se encarga de las dos cosas).
/// </summary>
public sealed class WatchService : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly DispatcherTimer _debounce;
    private readonly DispatcherTimer _safety;
    private readonly Func<Options> _options;
    private readonly DateTime _since = DateTime.Now;

    private bool _busy;
    private bool _disposed;

    public string Folder { get; }

    public TimeSpan Idle { get; }

    /// <summary>Se dispara solo cuando de verdad se ha movido algo.</summary>
    public event Action<WatchReport>? Organized;

    public event Action<string>? Failed;

    public WatchService(string folder, Func<Options> options, bool recursive, TimeSpan? idle = null)
    {
        Folder = folder;
        _options = options;
        Idle = idle ?? Watcher.DefaultIdle;

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _debounce.Tick += (_, _) => { _debounce.Stop(); Sweep(); };

        // Revision periodica: un archivo que llega se queda "demasiado nuevo" en la pasada
        // de los 10 s, asi que hay que volver a mirar (cada 30 s) hasta que cumpla los 2 min.
        _safety = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _safety.Tick += (_, _) => Sweep();

        _watcher = new FileSystemWatcher(folder)
        {
            IncludeSubdirectories = recursive,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = false,
        };

        _watcher.Created += OnChanged;
        _watcher.Changed += OnChanged;
        _watcher.Renamed += OnChanged;
    }

    public void Start()
    {
        try
        {
            _watcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            Failed?.Invoke(ex.Message);
            return;
        }

        _safety.Start();
    }

    public void Stop()
    {
        _debounce.Stop();
        _safety.Stop();

        try { _watcher.EnableRaisingEvents = false; }
        catch { /* la carpeta pudo desaparecer */ }
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        _debounce.Stop();
        _debounce.Start();
    }

    private async void Sweep()
    {
        if (_busy || _disposed) return;

        _busy = true;
        try
        {
            Options options = _options();
            WatchReport report = await Task.Run(() => Watcher.Sweep(options, Idle, null, _since));

            // Tambien se avisa si solo fallo el registro: el usuario debe enterarse.
            if (report.SomethingMoved || report.LogFailed) Organized?.Invoke(report);
        }
        catch (Exception ex)
        {
            Failed?.Invoke(ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }

    public void Dispose()
    {
        _disposed = true;
        Stop();
        _watcher.Dispose();
    }
}
