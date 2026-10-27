using System.IO;
using System.Windows;
using FileNest.App.Services;

namespace FileNest.App.Dialogs;

/// <summary>Historial de ejecuciones de una carpeta para elegir cual deshacer.</summary>
public partial class HistoryWindow : Window
{
    private readonly List<string> _logs;

    /// <summary>Registro elegido (null si se cancela).</summary>
    public string? SelectedLog { get; private set; }

    public HistoryWindow(List<string> logs)
    {
        InitializeComponent();

        _logs = logs;
        Title = Strings.HistoryTitle;
        TitleBar.Attach(this);

        LogList.ItemsSource = logs.Select(Path.GetFileName).ToList();
        if (logs.Count > 0) LogList.SelectedIndex = 0;
    }

    private void OnAccept(object sender, RoutedEventArgs e)
    {
        if (LogList.SelectedIndex < 0) return;

        SelectedLog = _logs[LogList.SelectedIndex];
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
