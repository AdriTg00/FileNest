using System.Drawing;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using FileNest.Core;
using Forms = System.Windows.Forms;

namespace FileNest.App.Services;

/// <summary>
/// Icono de la bandeja del sistema mientras el modo vigilancia esta activo.
/// El menu es un ContextMenu de WPF para que respete el tema claro/oscuro.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly System.Windows.Controls.ContextMenu _menu;

    public event Action? OpenRequested;
    public event Action? StopWatchingRequested;
    public event Action? ExitRequested;

    public TrayIcon()
    {
        _menu = new System.Windows.Controls.ContextMenu();
        _menu.Items.Add(Item(Strings.TrayOpen, () => OpenRequested?.Invoke()));
        _menu.Items.Add(Item(Strings.TrayStopWatching, () => StopWatchingRequested?.Invoke()));
        _menu.Items.Add(new Separator());
        _menu.Items.Add(Item(Strings.TrayExit, () => ExitRequested?.Invoke()));

        _icon = new Forms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = AppInfo.Name,
            Visible = true,
        };

        _icon.MouseDoubleClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left) OpenRequested?.Invoke();
        };

        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Right) ShowMenu();
        };

        _icon.BalloonTipClicked += (_, _) => OpenRequested?.Invoke();
    }

    /// <summary>Aviso tipo toast (globo del icono de la bandeja).</summary>
    public void ShowMessage(string title, string text) =>
        _icon.ShowBalloonTip(6000, title, text, Forms.ToolTipIcon.Info);

    public void SetTooltip(string text)
    {
        // NotifyIcon.Text tiene un limite de 63 caracteres.
        _icon.Text = text.Length > 63 ? text[..60] + "..." : text;
    }

    private void ShowMenu()
    {
        _menu.Placement = PlacementMode.MousePoint;
        _menu.IsOpen = true;
    }

    private static MenuItem Item(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    private static Icon LoadIcon()
    {
        try
        {
            return Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? SystemIcons.Application;
        }
        catch
        {
            return SystemIcons.Application;
        }
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
