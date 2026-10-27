using System.Windows;
using System.Windows.Threading;
using FileNest.App.Services;
using FileNest.Core;

namespace FileNest.App;

public partial class App : Application
{
    private Mutex? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Solo puede haber una ventana abierta: si ya hay una, se trae al frente.
        _singleInstance = SingleInstance.Acquire();
        if (_singleInstance == null)
        {
            SingleInstance.NotifyAlreadyRunning(e.Args.Length > 0 ? e.Args[0] : null);
            Shutdown(0);
            return;
        }

        // Un fallo inesperado debe verse, no cerrar la app en silencio.
        DispatcherUnhandledException += OnUnhandledException;

        // Las barras de scroll del sistema no siguen el tema: se estilan a mano
        // en cuanto la ventana (o un dialogo) termina de colocarse.
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
                                          new RoutedEventHandler(OnAnyWindowLoaded));

        ThemeManager.Apply(Settings.Theme);

        var window = new MainWindow(e.Args);
        MainWindow = window;
        window.Show();
    }

    private static void OnAnyWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Window window) return;

        window.Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
        {
            ScrollBarStyler.Apply(window);
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            e.Exception.Message,
            Lang.T("Error inesperado", "Unexpected error"),
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }
}
