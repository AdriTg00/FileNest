using System.Windows;
using FileNest.App.Services;
using FileNest.Core;

namespace FileNest.App.Dialogs;

/// <summary>Archivos repetidos por contenido, con la opcion de mover los sobrantes.</summary>
public partial class DuplicatesWindow : Window
{
    private readonly Func<Options> _options;
    private readonly CancellationTokenSource _cancel = new();
    private readonly List<GroupVm> _groups = new();

    private bool _busy;

    public DuplicatesWindow(Func<Options> options)
    {
        InitializeComponent();

        _options = options;
        Title = Strings.DuplicatesTitle;
        TitleBar.Attach(this);
        HintText.Text = Strings.DuplicatesHint;

        Loaded += (_, _) => _ = ScanAsync();
        Closed += (_, _) => _cancel.Cancel();
    }

    // ================================================================ escaneo ==

    private async Task ScanAsync()
    {
        SetBusy(true, scanning: true);
        SummaryText.Text = Strings.LookingForDuplicates;
        EmptyText.Visibility = Visibility.Collapsed;

        var progress = new Progress<DuplicateProgress>(p =>
        {
            Progress.Maximum = Math.Max(p.ToHash, 1);
            Progress.Value = p.Hashed;
            ProgressText.Text = $"{p.Hashed}/{p.ToHash}";
        });

        try
        {
            Options options = _options();
            DuplicateReport report = await Task.Run(
                () => Duplicates.Find(options, progress, _cancel.Token), _cancel.Token);

            Fill(report, options.Directory);
        }
        catch (OperationCanceledException)
        {
            // se cierra la ventana: no hay nada que hacer
        }
        finally
        {
            SetBusy(false, scanning: false);
        }
    }

    private void Fill(DuplicateReport report, string folder)
    {
        _groups.Clear();

        foreach (var group in report.Groups)
        {
            var vm = new GroupVm(Lang.T(
                $"{group.Files.Count} copias · {Paths.HumanSize(group.Size)}",
                $"{group.Files.Count} copies · {Paths.HumanSize(group.Size)}"));

            foreach (var file in group.Files)
            {
                // El primero es el mas antiguo: se conserva. Los demas vienen marcados.
                bool keep = ReferenceEquals(file, group.Files[0]);
                vm.Files.Add(new FileVm(file.Path, Paths.RelativeTo(file.Path, folder), selected: !keep));
            }

            _groups.Add(vm);
        }

        GroupsHost.ItemsSource = null;
        GroupsHost.ItemsSource = _groups;

        if (!report.Any)
        {
            EmptyText.Text = Strings.NoDuplicates;
            EmptyText.Visibility = Visibility.Visible;
            SummaryText.Text = Lang.T(
                $"Analizados {report.Scanned} archivos: no hay repetidos.",
                $"{report.Scanned} files scanned: no duplicates found.");
            UpdateButtons();
            return;
        }

        SummaryText.Text = Lang.T(
            $"{report.Groups.Count} grupos · {report.ExtraFiles} archivos repetidos · " +
            $"{Paths.HumanSize(report.WastedBytes)} recuperables. Se conserva el más antiguo de cada grupo.",
            $"{report.Groups.Count} groups · {report.ExtraFiles} repeated files · " +
            $"{Paths.HumanSize(report.WastedBytes)} recoverable. The oldest of each group is kept.");

        UpdateButtons();
    }

    // ================================================================ acciones ==

    private void OnFileToggled(object sender, RoutedEventArgs e) => UpdateButtons();

    private async void OnMove(object sender, RoutedEventArgs e)
    {
        var selected = SelectedFiles();
        if (selected.Count == 0) return;

        string question = Lang.T(
            $"¿Mover {selected.Count} archivos a «{Duplicates.FolderName}»?\n\n" +
            "Se pueden recuperar con Deshacer.",
            $"Move {selected.Count} files to «{Duplicates.FolderName}»?\n\n" +
            "You can get them back with Undo.");

        if (MessageBox.Show(question, Strings.DuplicatesTitle,
                            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        SetBusy(true, scanning: false);

        Options options = _options();
        ApplyResult result = await Task.Run(() => Duplicates.MoveToFolder(options, selected));

        SetBusy(false, scanning: false);

        string text = Lang.T(
            $"{result.Moved} archivos movidos a «{Duplicates.FolderName}».",
            $"{result.Moved} files moved to «{Duplicates.FolderName}».");
        if (result.Errors > 0) text += Lang.T($", {result.Errors} con error", $", {result.Errors} failed");

        MessageBox.Show(text, Strings.DuplicatesTitle, MessageBoxButton.OK, MessageBoxImage.Information);

        await ScanAsync();
    }

    private List<string> SelectedFiles() =>
        _groups.SelectMany(group => group.Files)
               .Where(file => file.Selected)
               .Select(file => file.Path)
               .ToList();

    private void UpdateButtons()
    {
        int count = SelectedFiles().Count;
        MoveButton.IsEnabled = !_busy && count > 0;
        MoveButton.Content = count > 0
            ? Lang.T($"Mover {count} a {Duplicates.FolderName}", $"Move {count} to {Duplicates.FolderName}")
            : Strings.MoveToDuplicates;
    }

    private void SetBusy(bool busy, bool scanning)
    {
        _busy = busy;
        ProgressPanel.Visibility = scanning ? Visibility.Visible : Visibility.Collapsed;

        if (scanning)
        {
            Progress.Value = 0;
            ProgressText.Text = "";
        }

        UpdateButtons();
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}

/// <summary>Un grupo de copias, tal como se muestra en la lista.</summary>
public sealed class GroupVm
{
    public GroupVm(string title) => Title = title;

    public string Title { get; }
    public List<FileVm> Files { get; } = new();
}

/// <summary>Un archivo dentro del grupo (con su casilla).</summary>
public sealed class FileVm
{
    public FileVm(string path, string display, bool selected)
    {
        Path = path;
        Display = display;
        Selected = selected;
    }

    public string Path { get; }
    public string Display { get; }
    public bool Selected { get; set; }
}
