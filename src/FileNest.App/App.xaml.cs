using System.Windows;
using System.Windows.Threading;
using FileNest.App.Services;
using FileNest.Core;

namespace FileNest.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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
