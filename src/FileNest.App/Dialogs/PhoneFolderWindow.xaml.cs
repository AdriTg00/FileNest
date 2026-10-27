using System.Windows;
using System.Windows.Input;
using FileNest.App.Services;
using FileNest.Core;

namespace FileNest.App.Dialogs;

/// <summary>Mini-explorador para elegir la carpeta del movil.</summary>
public partial class PhoneFolderWindow : Window
{
    private readonly string _deviceId;

    /// <summary>Carpeta elegida (solo valida si el dialogo devuelve true).</summary>
    public string Folder { get; private set; } = "";

    public PhoneFolderWindow(string deviceId)
    {
        InitializeComponent();

        _deviceId = deviceId;
        Title = Strings.PhoneFolderTitle;
        TitleBar.Attach(this);

        string last = Settings.LastPhoneFolder;
        PathBox.Text = last.Length > 0 ? last : "/sdcard/Download";

        Loaded += (_, _) => ListFolders();
    }

    private void ListFolders()
    {
        string path = PathBox.Text.Trim();
        if (path.Length == 0) return;

        FolderList.ItemsSource = Android.Subfolders(_deviceId, path);
    }

    private void OnUp(object sender, RoutedEventArgs e)
    {
        string path = PathBox.Text.Trim();

        while (path.Length > 1 && path.EndsWith('/')) path = path[..^1];

        int slash = path.LastIndexOf('/');
        if (slash > 0) path = path[..slash];

        PathBox.Text = path;
        ListFolders();
    }

    private void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FolderList.SelectedItem is not string name) return;

        string path = PathBox.Text.Trim().TrimEnd('/');
        PathBox.Text = path + "/" + name;
        ListFolders();
    }

    private void OnAccept(object sender, RoutedEventArgs e)
    {
        string path = PathBox.Text.Trim();
        if (path.Length == 0) return;

        Folder = path;
        Settings.LastPhoneFolder = path;
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
