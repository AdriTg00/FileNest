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

        ThemeManager.Apply(Settings.Theme);

        var window = new MainWindow(e.Args);
        MainWindow = window;
        window.Show();
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
