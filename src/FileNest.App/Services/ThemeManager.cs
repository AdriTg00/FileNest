using System.Windows;
using System.Windows.Media;

namespace FileNest.App;

/// <summary>Paleta claro / oscuro. Los colores viven en Themes/Light.xaml y Dark.xaml.</summary>
public static class ThemeManager
{
    public const int Light = 0;
    public const int Dark = 1;

    /// <summary>Tema actual (0 = claro, 1 = oscuro).</summary>
    public static int Current { get; private set; } = Light;

    /// <summary>Se dispara al cambiar de tema (la ventana repinta su barra de titulo).</summary>
    public static event Action? Changed;

    public static void Apply(int theme)
    {
        Current = theme == Dark ? Dark : Light;

        var dictionaries = Application.Current.Resources.MergedDictionaries;
        string wanted = Current == Dark ? "Themes/Dark.xaml" : "Themes/Light.xaml";

        for (int i = 0; i < dictionaries.Count; ++i)
        {
            string? source = dictionaries[i].Source?.OriginalString;
            if (source == null) continue;
            if (!source.EndsWith("Light.xaml", StringComparison.OrdinalIgnoreCase)
                && !source.EndsWith("Dark.xaml", StringComparison.OrdinalIgnoreCase)) continue;

            if (!source.EndsWith(wanted.Replace("Themes/", ""), StringComparison.OrdinalIgnoreCase))
                dictionaries[i] = new ResourceDictionary
                {
                    Source = new Uri(wanted, UriKind.Relative),
                };

            break;
        }

        Changed?.Invoke();
    }

    public static void Toggle() => Apply(Current == Dark ? Light : Dark);

    public static Color Color(string key) =>
        (Application.Current.TryFindResource(key) as Color?) ?? Colors.Transparent;

    public static Brush Brush(string key) =>
        (Application.Current.TryFindResource(key) as Brush) ?? Brushes.Transparent;
}
