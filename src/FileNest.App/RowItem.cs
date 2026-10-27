using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace FileNest.App;

/// <summary>Una fila de la lista de archivos.</summary>
public sealed class RowItem : INotifyPropertyChanged
{
    private string _size = "";
    private string _category = "";
    private string _target = "";
    private string _status = "";
    private int _state;

    public string Name { get; init; } = "";
    public ImageSource? Icon { get; init; }

    public string Size
    {
        get => _size;
        set => Set(ref _size, value);
    }

    public string Category
    {
        get => _category;
        set => Set(ref _category, value);
    }

    public string Target
    {
        get => _target;
        set => Set(ref _target, value);
    }

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    /// <summary>0 = pendiente, 1 = movido, 2 = error.</summary>
    public int State
    {
        get => _state;
        set => Set(ref _state, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? property = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }
}
