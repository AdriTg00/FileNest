using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace FileNest.App.Services;

/// <summary>
/// Aplica el estilo propio a las barras de scroll.
///
/// Hace falta hacerlo a mano: el ScrollViewer crea sus ScrollBar con un estilo
/// explicito del tema, que gana al estilo implicito de la aplicacion. Por eso en
/// el tema oscuro se veia la barra clara del sistema.
/// </summary>
public static class ScrollBarStyler
{
    private const string StyleKey = "FileNestScrollBar";

    public static void Apply(DependencyObject root)
    {
        if (Application.Current.TryFindResource(StyleKey) is not Style style) return;
        Walk(root, style);
    }

    private static void Walk(DependencyObject node, Style style)
    {
        if (node is ScrollBar bar)
        {
            if (!ReferenceEquals(bar.Style, style)) bar.Style = style;

            // El tema del sistema fija MinWidth/MinHeight = 17 (ancho nativo de la
            // barra). Es un valor local, asi que gana a nuestro Width: hay que quitarlo.
            bar.MinWidth = 0;
            bar.MinHeight = 0;
        }

        int count = VisualTreeHelper.GetChildrenCount(node);
        for (int i = 0; i < count; ++i)
            Walk(VisualTreeHelper.GetChild(node, i), style);
    }

}

