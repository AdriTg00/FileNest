using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using FileNest.App.Dialogs;
using FileNest.App.Services;
using FileNest.Core;
using Microsoft.Win32;

namespace FileNest.App;

public partial class MainWindow : Window
{
    private readonly Options _options = new();
    private readonly ObservableCollection<RowItem> _rows = new();

    private Plan _plan = new();
    private bool _busy;
    private bool _phoneMode;
    private bool _lastWasPhone;

    private string _phoneDevice = "";
    private string _phoneFolder = "";
    private string _phoneLog = "";
    private List<PhoneFile> _phoneFiles = new();

    public MainWindow(string[] args)
    {
        InitializeComponent();

        FileList.ItemsSource = _rows;

        Title = $"FileNest {AppInfo.Version} · " +
                Lang.T("Organizador de carpetas", "Folder organizer");

        _options.Directory = PickInitialFolder(args);
        Settings.LastFolder = _options.Directory;
        FolderBox.Text = _options.Directory;

        LoadRules();
        UpdateThemeText();
        UpdateButtons();
        ResetProgress();

        Loaded += (_, _) =>
        {
            Simulate();
            _ = CheckUpdatesAsync(silent: true);
        };

        ThemeManager.Changed += OnThemeChanged;
        Closed += (_, _) => ThemeManager.Changed -= OnThemeChanged;

        RestoreWindowBounds();
        Closing += (_, _) => SaveWindowBounds();

        TitleBar.Attach(this);
    }

    // ============================================================ geometria ==

    /// <summary>Recupera tamano y posicion de la ultima vez (si siguen siendo validas).</summary>
    private void RestoreWindowBounds()
    {
        string saved = Settings.WindowBounds;
        if (saved.Length > 0)
        {
            string[] parts = saved.Split(',');
            if (parts.Length == 4
                && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double left)
                && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double top)
                && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double width)
                && double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double height)
                && width >= MinWidth && height >= MinHeight
                && IsOnSomeScreen(left, top, width, height))
            {
                WindowStartupLocation = WindowStartupLocation.Manual;
                Left = left;
                Top = top;
                Width = width;
                Height = height;
            }
        }

        if (Settings.WindowMaximized) WindowState = WindowState.Maximized;
    }

    /// <summary>Se guarda el tamano NORMAL (aunque este maximizada) para poder restaurarlo.</summary>
    private void SaveWindowBounds()
    {
        Rect bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, Width, Height)
            : RestoreBounds;

        if (bounds.Width < MinWidth || bounds.Height < MinHeight) return;

        Settings.WindowBounds = string.Format(CultureInfo.InvariantCulture, "{0:0},{1:0},{2:0},{3:0}",
                                              bounds.Left, bounds.Top, bounds.Width, bounds.Height);
        Settings.WindowMaximized = WindowState == WindowState.Maximized;
    }

    /// <summary>Evita que la ventana aparezca fuera de la pantalla (monitor desconectado).</summary>
    private static bool IsOnSomeScreen(double left, double top, double width, double height)
    {
        var window = new Rect(left, top, width, height);
        var screen = new Rect(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
                              SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);

        window.Intersect(screen);
        return window.Width > 120 && window.Height > 80;
    }

    // ================================================================ inicio ==

    private static string PickInitialFolder(string[] args)
    {
        // 1) carpeta pasada como argumento
        if (args.Length > 0 && Directory.Exists(args[0])) return args[0];

        // 2) la ultima que se uso
        string last = Settings.LastFolder;
        if (last.Length > 0 && Directory.Exists(last)) return last;

        // 3) Descargas
        return Paths.DefaultTargetDirectory;
    }

    private void LoadRules()
    {
        _options.CustomRules.Clear();
        _options.CustomRules.AddRange(Rules.Load());
    }

    private void OnThemeChanged()
    {
        UpdateThemeText();
    }

    private void UpdateThemeText()
    {
        ThemeButton.Content = ThemeManager.Current == ThemeManager.Dark
            ? Strings.ThemeDark
            : Strings.ThemeLight;
    }

    // ============================================================ simulacion ==

    private void Simulate()
    {
        _phoneMode = false;
        _phoneFiles = new List<PhoneFile>();
        ResetProgress();
        _rows.Clear();
        _plan = new Plan();

        if (!Directory.Exists(_options.Directory))
        {
            Status(Lang.T("Elige una carpeta válida para empezar.", "Choose a valid folder to start."));
            UpdateButtons();
            return;
        }

        _plan = Planner.Build(_options);

        foreach (var move in _plan.Moves)
        {
            long size = 0;
            try { size = new FileInfo(move.From).Length; } catch { /* sin permisos */ }

            _rows.Add(new RowItem
            {
                Name = move.FileName,
                Icon = IconCache.ForFile(move.From),
                Size = size > 0 ? Paths.HumanSize(size) : "—",
                Category = move.Category,
                Target = Paths.RelativeTo(move.To, _options.Directory),
                Status = Strings.Pending,
                State = 0,
            });
        }

        string text = $"{_plan.Analyzed} " + Lang.T("archivos · ", "files · ")
                    + $"{_plan.Moves.Count} " + Lang.T("a clasificar", "to sort");

        if (_plan.Skipped > 0) text += " · " + _plan.Skipped + Lang.T(" omitidos", " skipped");
        if (_plan.Moves.Count == 0)
            text = Lang.T("Nada que organizar: la carpeta ya está en orden.",
                          "Nothing to organize: the folder is already tidy.");

        Status(text);
        UpdateButtons();
    }

    // ================================================================ aplicar ==

    private async void OnApply(object sender, RoutedEventArgs e)
    {
        if (_phoneMode)
        {
            await ApplyPhoneAsync();
            return;
        }

        if (_plan.Moves.Count == 0) return;

        SetBusy(true);
        ResetProgress(_plan.Moves.Count);

        var results = new bool?[_plan.Moves.Count];
        int index = 0;

        IProgress<int> progress = new Progress<int>(i =>
        {
            ShowProgress(i + 1, _plan.Moves.Count);
            RepaintRow(i, results[i] == true);
        });

        ApplyResult result = await Task.Run(() => Applier.Apply(_plan, _options.Directory, (_, ok, _) =>
        {
            int i = index++;
            results[i] = ok;
            progress.Report(i);
        }));

        // Estado final (por si algun repintado llego tarde)
        for (int i = 0; i < results.Length && i < _rows.Count; ++i)
            RepaintRow(i, results[i] == true);

        foreach (var done in result.Done)
        {
            int i = _plan.Moves.FindIndex(m => ReferenceEquals(m, done));
            if (i >= 0 && i < _rows.Count)
                _rows[i].Target = Paths.RelativeTo(done.To, _options.Directory);
        }

        _lastWasPhone = false;
        ShowProgress(_plan.Moves.Count, _plan.Moves.Count);

        string text = Lang.T("Listo: ", "Done: ") + result.Moved + Lang.T(" movidos", " moved");
        if (result.Errors > 0) text += ", " + result.Errors + Lang.T(" con error", " with errors");
        text += Lang.T(".  Registro: ", ".  Log: ") + result.LogName;
        Status(text);

        SetBusy(false);
    }

    private void RepaintRow(int index, bool ok)
    {
        if (index < 0 || index >= _rows.Count) return;
        _rows[index].State = ok ? 1 : 2;
        _rows[index].Status = ok ? Strings.Moved : Strings.Failed;
    }

    // =============================================================== deshacer ==

    private async void OnUndo(object sender, RoutedEventArgs e)
    {
        if (_lastWasPhone)
        {
            await UndoPhoneAsync();
            return;
        }

        var logs = Undoer.FindLogs(_options.Directory);
        if (logs.Count == 0)
        {
            Status(Lang.T("No hay ninguna ejecución anterior en esta carpeta.",
                          "There is no previous run in this folder."));
            return;
        }

        var dialog = new HistoryWindow(logs) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.SelectedLog == null) return;

        UndoResult result = await Task.Run(() => Undoer.Undo(dialog.SelectedLog));

        string text = Lang.T("Deshecho: ", "Undone: ") + result.Restored + Lang.T(" restaurados", " restored");
        if (result.Skipped > 0) text += ", " + result.Skipped + Lang.T(" omitidos", " skipped");
        if (result.Errors > 0) text += ", " + result.Errors + Lang.T(" con error", " with errors");
        Status(text);

        Simulate();
    }

    // ================================================================== movil ==

    private async void OnPhone(object sender, RoutedEventArgs e)
    {
        if (!Android.IsAvailable)
        {
            MessageBox.Show(
                Lang.T("No encuentro adb.exe.\n\nSe instala con:\n    winget install Google.PlatformTools",
                       "Cannot find adb.exe.\n\nIt is installed with:\n    winget install Google.PlatformTools"),
                Strings.PhoneTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SetBusy(true);
        (string id, bool responded) = await Task.Run(DetectPhone);
        string model = id.Length > 0 ? await Task.Run(() => Android.Model(id)) : "";
        SetBusy(false);

        if (id.Length == 0)
        {
            MessageBox.Show(
                responded
                    ? Lang.T("No hay ningún móvil conectado (o está sin autorizar).\n\n" +
                             "1) En el móvil: Ajustes > Opciones de desarrollador > Depuración USB (activar).\n" +
                             "2) Conéctalo por USB y acepta el aviso «¿Permitir depuración USB?».\n" +
                             "3) Vuelve a pulsar «Móvil».",
                             "No phone connected (or it is not authorized).\n\n" +
                             "1) On the phone: Settings > Developer options > USB debugging (enable).\n" +
                             "2) Connect it via USB and accept the «Allow USB debugging?» prompt.\n" +
                             "3) Press «Phone» again.")
                    : Lang.T("adb no ha respondido.\n\nComprueba en una consola que funciona:\n    adb devices",
                             "adb did not respond.\n\nCheck in a console that it works:\n    adb devices"),
                Strings.PhoneTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _phoneDevice = id;

        var folderDialog = new PhoneFolderWindow(id) { Owner = this };
        if (folderDialog.ShowDialog() != true || folderDialog.Folder.Length == 0) return;

        string folder = folderDialog.Folder;
        _phoneFolder = folder;

        SetBusy(true);
        List<PhoneFile> plan = await Task.Run(() => Android.Plan(_options, id, folder));
        SetBusy(false);

        _rows.Clear();
        _phoneFiles = plan;
        _phoneMode = plan.Count > 0;
        ResetProgress(plan.Count);
        _lastWasPhone = false;

        foreach (var file in plan)
        {
            _rows.Add(new RowItem
            {
                Name = file.Name,
                Icon = IconCache.ForFile(file.Name),
                Size = "—",
                Category = file.Category,
                Target = folder + "/" + file.Category + "/" + file.Name,
                Status = Strings.Seen,
                State = 0,
            });
        }

        string text = Lang.T("Móvil: ", "Phone: ") + (model.Length > 0 ? model : id)
                    + "   ·   " + folder
                    + "   ·   " + plan.Count + Lang.T(" archivos", " files");

        text += plan.Count == 0
            ? Lang.T("   (nada que organizar)", "   (nothing to organize)")
            : Lang.T("   ·   pulsa «Aplicar» para organizarlos", "   ·   click «Apply» to organize them");

        Status(text);
        UpdateButtons();
    }

    private async Task ApplyPhoneAsync()
    {
        if (_phoneFiles.Count == 0) return;

        var question = Lang.T("¿Mover ", "Move ") + _phoneFiles.Count
                     + Lang.T(" archivos DENTRO del móvil?\n\n", " files INSIDE the phone?\n\n")
                     + _phoneFolder
                     + Lang.T("\n\nSe crearán subcarpetas por tipo y los archivos se moverán ahí.",
                              "\n\nSubfolders will be created by type and the files will be moved there.");

        if (MessageBox.Show(question, Strings.PhoneTitle,
                            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        SetBusy(true);
        ResetProgress(_phoneFiles.Count);

        IProgress<(int Index, int Total)> progress = new Progress<(int Index, int Total)>(p => ShowProgress(p.Index, p.Total));

        var outcome = await Task.Run(() => Android.ApplyPlan(
            _phoneDevice, _phoneFolder, _phoneFiles,
            (index, total) => progress.Report((index, total))));

        for (int i = 0; i < _phoneFiles.Count; ++i)
        {
            bool moved = outcome.Results.TryGetValue(_phoneFiles[i].Name, out bool ok) && ok;
            RepaintRow(i, moved);
        }

        _phoneMode = false;
        _phoneLog = outcome.LogPath;
        _lastWasPhone = outcome.LogPath.Length > 0;

        string text = Lang.T("Móvil: ", "Phone: ") + outcome.Moved + Lang.T(" movidos", " moved");
        if (outcome.Failed > 0) text += ", " + outcome.Failed + Lang.T(" con error", " with errors");
        text += Lang.T(".   Pulsa «Deshacer» si te arrepientes.", ".   Click «Undo» if you change your mind.");
        Status(text);

        SetBusy(false);
    }

    private async Task UndoPhoneAsync()
    {
        if (_phoneLog.Length == 0) return;

        (string id, bool responded) = await Task.Run(DetectPhone);

        if (id.Length == 0)
        {
            MessageBox.Show(
                responded
                    ? Lang.T("No hay ningún móvil conectado.\n\nConecta el mismo móvil y vuelve a intentarlo.",
                             "No phone connected.\n\nConnect the same phone and try again.")
                    : Lang.T("adb no ha respondido.", "adb did not respond."),
                Lang.T("Deshacer en el móvil", "Undo on phone"),
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show(
                Lang.T("¿Devolver los archivos a su sitio DENTRO del móvil?",
                       "Put the files back in their place INSIDE the phone?"),
                Lang.T("Deshacer en el móvil", "Undo on phone"),
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        SetBusy(true);
        (int restored, int failed) = await Task.Run(() => Android.UndoFromLog(id, _phoneLog));
        SetBusy(false);

        string text = Lang.T("Móvil: ", "Phone: ") + restored + Lang.T(" restaurados", " restored");
        if (failed > 0) text += ", " + failed + Lang.T(" con error", " with errors");
        Status(text + ".");

        _lastWasPhone = false;
    }

    // ================================================================ carpetas ==

    private void OnChooseFolder(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = Lang.T("Elige la carpeta que quieres organizar", "Choose the folder you want to organize"),
            Multiselect = false,
        };

        try { dialog.InitialDirectory = _options.Directory; } catch { /* ruta rara */ }

        if (dialog.ShowDialog(this) == true) SetFolder(dialog.FolderName);
    }

    private void SetFolder(string folder)
    {
        if (!Directory.Exists(folder)) return;

        _options.Directory = folder;
        Settings.LastFolder = folder;
        FolderBox.Text = folder;
        Simulate();
    }

    private void OnOpenFolder(object sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(_options.Directory)) return;

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_options.Directory}\"") { UseShellExecute = true });
        }
        catch
        {
            Status(Lang.T("No se pudo abrir la carpeta.", "Could not open the folder."));
        }
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        if (e.Data.GetData(DataFormats.FileDrop) is string[] items && items.Length > 0 && Directory.Exists(items[0]))
            SetFolder(items[0]);
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    // ================================================================= opciones ==

    private void OnSimulate(object sender, RoutedEventArgs e) => Simulate();

    private void OnOptionsChanged(object sender, RoutedEventArgs e)
    {
        _options.ByDate = ByDateCheck.IsChecked == true;
        _options.Recursive = RecursiveCheck.IsChecked == true;
        Simulate();
    }

    private void OnTheme(object sender, RoutedEventArgs e)
    {
        ThemeManager.Toggle();
        Settings.Theme = ThemeManager.Current;
    }

    private void OnRules(object sender, RoutedEventArgs e)
    {
        var dialog = new RulesWindow(_options.CustomRules) { Owner = this };
        if (dialog.ShowDialog() != true) return;

        Rules.Save(dialog.Rules);
        LoadRules();
        Simulate();
    }

    // ============================================================== actualizar ==

    private async void OnCheckUpdates(object sender, RoutedEventArgs e)
    {
        await CheckUpdatesAsync(silent: false);
    }

    private async Task CheckUpdatesAsync(bool silent)
    {
        if (!silent) Status(Lang.T("Buscando actualizaciones…", "Checking for updates…"));

        UpdateInfo? info = await Updater.CheckAsync();

        if (info == null)
        {
            if (!silent)
                MessageBox.Show(
                    Lang.T("No se pudo comprobar si hay actualizaciones.", "Could not check for updates."),
                    Strings.UpdatesTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!Updater.IsNewer(info.Tag))
        {
            if (!silent)
            {
                MessageBox.Show(
                    Lang.T($"Ya tienes la última versión ({AppInfo.Version}).",
                           $"You already have the latest version ({AppInfo.Version})."),
                    Strings.UpdatesTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }

            return;
        }

        var question = Lang.T($"Hay una versión nueva disponible: {info.Tag}\n\nLa tuya es la {AppInfo.Version}.\n\n¿Descargarla e instalarla ahora?",
                              $"A new version is available: {info.Tag}\n\nYours is {AppInfo.Version}.\n\nDownload and install it now?");

        if (MessageBox.Show(question, Strings.UpdateTitle,
                            MessageBoxButton.YesNo, MessageBoxImage.Information) != MessageBoxResult.Yes)
            return;

        SetBusy(true);
        Status(Lang.T("Descargando la actualización…", "Downloading the update…"));

        IProgress<double> progress = new Progress<double>(p => ShowProgress((int)(p * 100), 100));
        string? installer = await Updater.DownloadAsync(info, progress);

        SetBusy(false);

        if (installer == null)
        {
            Status("");
            MessageBox.Show(
                Lang.T("No se pudo descargar la actualización.", "Could not download the update."),
                Strings.UpdateTitleShort, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show(
                Lang.T("Se va a ejecutar el instalador de la nueva versión.\n\nFileNest se cerrará para que pueda actualizarse. ¿Continuar?",
                       "The installer for the new version will now run.\n\nFileNest will close so it can update. Continue?"),
                Strings.UpdateTitleShort,
                MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK)
            return;

        if (Updater.RunInstaller(installer)) Close();
    }

    private void OnAbout(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            Lang.T($"FileNest {AppInfo.Version}\n\nOrganiza carpetas por tipo de archivo.\n{AppInfo.SiteUrl}",
                   $"FileNest {AppInfo.Version}\n\nOrganizes folders by file type.\n{AppInfo.SiteUrl}"),
            Strings.AboutTitle, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ================================================================== estado ==

    private void Status(string text) => StatusText.Text = text;

    private void ResetProgress(int maximum = 0)
    {
        Progress.Maximum = maximum > 0 ? maximum : 1;
        Progress.Value = 0;
        ProgressText.Text = "";
    }

    private void ShowProgress(int done, int total)
    {
        if (total <= 0) return;
        Progress.Maximum = total;
        Progress.Value = Math.Min(done, total);

        int percent = (int)(100L * done / total);
        ProgressText.Text = $"{percent}%   {done}/{total}";
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        Mouse.OverrideCursor = busy ? System.Windows.Input.Cursors.Wait : null;
        UpdateButtons();
    }

    /// <summary>Busca el movil conectado (bloquea: por eso se llama dentro de Task.Run).</summary>
    private static (string Id, bool Responded) DetectPhone()
    {
        string id = Android.DeviceId(out bool responded);
        return (id, responded);
    }

    private void UpdateButtons()
    {
        SimulateButton.IsEnabled = !_busy;
        UndoButton.IsEnabled = !_busy;
        OpenButton.IsEnabled = !_busy;
        ChooseButton.IsEnabled = !_busy;
        RulesButton.IsEnabled = !_busy;
        ThemeButton.IsEnabled = !_busy;
        PhoneButton.IsEnabled = !_busy;

        bool canApply = _phoneMode ? _phoneFiles.Count > 0 : _plan.Moves.Count > 0;
        ApplyButton.IsEnabled = !_busy && canApply;
    }
}
