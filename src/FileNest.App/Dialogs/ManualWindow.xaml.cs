using System.Windows;
using System.Windows.Controls;
using FileNest.App.Services;
using FileNest.Core;

namespace FileNest.App.Dialogs;

/// <summary>
/// Manual de uso dentro de la propia aplicacion (Ayuda -> Manual, o F1).
/// El texto se construye aqui para no duplicarlo en el XAML y poder tenerlo
/// en los dos idiomas con Lang.T.
/// </summary>
public partial class ManualWindow : Window
{
    public ManualWindow()
    {
        InitializeComponent();

        Title = Strings.ManualTitle;
        TitleBar.Attach(this);

        Build();
    }

    private void Build()
    {
        // ---------------------------------------------------------- que hace --
        Section(Lang.T("Qué hace FileNest", "What FileNest does"));
        Paragraph(Lang.T(
            "Ordena una carpeta: mueve cada archivo a una subcarpeta según su extensión " +
            "(Imágenes, Documentos, Vídeos, Audio…) y, si quieres, dentro de Año\\Mes.",
            "It tidies a folder: every file is moved into a subfolder based on its extension " +
            "(Images, Documents, Videos, Audio…) and, optionally, into Year\\Month."));
        Paragraph(Lang.T(
            "Por defecto solo SIMULA: puedes ver qué haría sin mover nada. Nada se toca " +
            "hasta que pulsas Aplicar.",
            "By default it only SIMULATES: you see what it would do without moving anything. " +
            "Nothing is touched until you press Apply."));

        // -------------------------------------------------------- uso rapido --
        Section(Lang.T("Uso rápido", "Quick start"));
        Bullet(Lang.T("Elige la carpeta con el botón o arrástrala sobre la ventana.",
                      "Choose the folder with the button or drag it onto the window."));
        Bullet(Lang.T("Revisa la lista: archivo, tamaño, categoría y destino.",
                      "Check the list: file, size, category and target."));
        Bullet(Lang.T("Simular recalcula la lista sin tocar nada.",
                      "Simulate recalculates the list without touching anything."));
        Bullet(Lang.T("Aplicar mueve los archivos y escribe un registro para poder deshacer.",
                      "Apply moves the files and writes a log so you can undo it."));
        Bullet(Lang.T("Si te arrepientes, Deshacer los devuelve a su sitio.",
                      "If you change your mind, Undo puts them back."));

        // ----------------------------------------------------------- botones --
        Section(Lang.T("Botones", "Buttons"));
        Bullet(Lang.T("Elegir carpeta…: diálogo de Windows. También puedes arrastrar una carpeta.",
                      "Choose folder…: native Windows dialog. You can also drag a folder in."));
        Bullet(Lang.T("Móvil: organiza dentro de un teléfono Android por USB (necesita adb).",
                      "Phone: organizes inside an Android phone over USB (needs adb)."));
        Bullet(Lang.T("Reglas…: tus propias reglas extensión → carpeta.",
                      "Rules…: your own extension → folder rules."));
        Bullet(Lang.T("Tema: cambia entre claro y oscuro. Se recuerda.",
                      "Theme: switches light/dark. It is remembered."));
        Bullet(Lang.T("Simular: calcula y rellena la lista. No mueve nada.",
                      "Simulate: calculates and fills the list. It moves nothing."));
        Bullet(Lang.T("Aplicar: mueve los archivos. Cada fila se pone verde (movido) o roja (error).",
                      "Apply: moves the files. Each row turns green (moved) or red (failed)."));
        Bullet(Lang.T("Deshacer: abre el historial de esa carpeta y revierte la ejecución que elijas.",
                      "Undo: opens that folder's history and reverts the run you choose."));
        Bullet(Lang.T("Abrir carpeta: abre la carpeta en el Explorador de Windows.",
                      "Open folder: opens the folder in Windows Explorer."));

        // --------------------------------------------------------- opciones --
        Section(Lang.T("Opciones", "Options"));
        Bullet(Lang.T("Agrupar por fecha (Año\\Mes): dentro de cada categoría crea subcarpetas por año y mes.",
                      "Group by date (Year\\Month): inside each category it creates year and month subfolders."));
        Bullet(Lang.T("Incluir subcarpetas: organiza también lo que hay dentro. Nunca entra en las carpetas de categoría.",
                      "Include subfolders: also organizes what is inside. It never goes into category folders."));

        // ------------------------------------------------------- categorias --
        Section(Lang.T("Categorías", "Categories"));
        foreach (var category in Categories.Default)
            Bullet($"{category.Icon}  {category.Name}: {string.Join(", ", category.Extensions)}");

        Bullet(Lang.T($"Lo que no encaja en ninguna va a «{Categories.OtherName}».",
                      $"Anything that fits nowhere goes to «{Categories.OtherName}»."));

        // -------------------------------------------------------- registros --
        Section(Lang.T("Registros y deshacer", "Logs and undo"));
        Paragraph(Lang.T(
            "Cada vez que aplicas se escribe un archivo .organizador-log-FECHA.csv en la " +
            "carpeta organizada. Ese archivo es lo que permite deshacer: no lo borres si " +
            "quieres poder revertir la operación.",
            "Every time you apply, a .organizador-log-DATE.csv file is written in the organized " +
            "folder. That file is what makes undo possible: do not delete it if you want to be " +
            "able to revert the operation."));
        Paragraph(Lang.T(
            "El historial de Deshacer muestra los registros de la carpeta actual, del más " +
            "reciente al más antiguo.",
            "The Undo history lists the logs of the current folder, newest first."));

        // ----------------------------------------------------------- reglas --
        Section(Lang.T("Reglas propias", "Your own rules"));
        Paragraph(Lang.T(
            "En Reglas… puedes crear reglas propias: una extensión (.psd), un patrón (factura*.pdf) " +
            "o un nombre exacto, y a qué carpeta van. La carpeta admite variables: " +
            "{año}, {mes}, {día}, {ext} y {nombre}; por ejemplo factura*.pdf → Facturas\\{año}. " +
            "Se aplica la primera regla que coincide, así que el orden importa (Subir/Bajar).",
            "In Rules… you can create your own rules: an extension (.psd), a pattern (invoice*.pdf) " +
            "or an exact name, and which folder they go to. The folder accepts variables: " +
            "{year}, {month}, {day}, {ext} and {name}; for example invoice*.pdf → Invoices\\{year}. " +
            "The first matching rule wins, so order matters (Up/Down)."));
        Paragraph(Lang.T(
            "Se guardan en %LOCALAPPDATA%\\FileNest\\reglas.txt y se aplican al momento, sin reiniciar.",
            "They are stored in %LOCALAPPDATA%\\FileNest\\reglas.txt and applied immediately, no restart needed."));

        // ------------------------------------------------------------ movil --
        Section(Lang.T("Vigilar una carpeta", "Watch a folder"));
        Paragraph(Lang.T(
            "Marca «Vigilar esta carpeta» y FileNest se queda en la bandeja del sistema " +
            "ordenando lo que llega: solo mueve archivos que aparecen mientras vigila y que " +
            "llevan 2 minutos sin cambios, así que nunca mueve una descarga a medias.",
            "Tick «Watch this folder» and FileNest stays in the system tray sorting what " +
            "arrives: it only moves files that appear while watching and have been idle for " +
            "2 minutes, so a download in progress is never moved."));
        Paragraph(Lang.T(
            "No toca lo que ya estaba en la carpeta (para eso, pulsa Aplicar). Cada tanda " +
            "escribe su registro (Deshacer funciona igual) y avisa con una notificación. " +
            "Al cerrar la ventana sigue en la bandeja: clic derecho en el icono para abrirlo, " +
            "dejar de vigilar o salir.",
            "It does not touch what was already in the folder (press Apply for that). Each " +
            "batch writes its own log (Undo works as usual) and shows a notification. Closing " +
            "the window keeps it in the tray: right-click the icon to open it, stop watching " +
            "or quit."));

        // ------------------------------------------------------------ movil --
        Section(Lang.T("Duplicados", "Duplicates"));
        Paragraph(Lang.T(
            "Duplicados… compara el CONTENIDO de los archivos (tamaño y después SHA-256), " +
            "no el nombre: dos archivos con distinto nombre pero iguales salen como repetidos. " +
            "Se conserva el más antiguo de cada grupo y los demás se pueden mover a la carpeta " +
            "«Duplicados». Nunca se borra nada y el movimiento queda en el registro, así que " +
            "Deshacer lo revierte.",
            "Duplicates… compares the CONTENT of the files (size, then SHA-256), not the name: " +
            "two files with different names but the same content show up as repeats. The oldest " +
            "of each group is kept and the rest can be moved to the «Duplicates» folder. Nothing " +
            "is ever deleted and the move goes into the log, so Undo reverts it."));
        Paragraph(Lang.T(
            "Marca o desmarca cada copia antes de mover. Con «Incluir subcarpetas» se mira " +
            "también dentro de las subcarpetas.",
            "Tick or untick each copy before moving. With «Include subfolders» it also looks " +
            "inside subfolders."));

        // ------------------------------------------------------------ movil --
        Section(Lang.T("Móvil (Android)", "Phone (Android)"));
        Paragraph(Lang.T(
            "Requisitos: en el teléfono, Ajustes > Opciones de desarrollador > Depuración USB; " +
            "conéctalo por USB y acepta el aviso; y ten adb instalado en el PC " +
            "(winget install Google.PlatformTools).",
            "Requirements: on the phone, Settings > Developer options > USB debugging; connect " +
            "it over USB and accept the prompt; and have adb installed on the PC " +
            "(winget install Google.PlatformTools)."));
        Paragraph(Lang.T(
            "Pulsa Móvil, elige la carpeta del teléfono, revisa la simulación y pulsa Aplicar. " +
            "Se crean subcarpetas y se mueven los archivos dentro del teléfono. Deshacer " +
            "revierte la última organización hecha en el móvil.",
            "Press Phone, choose the phone folder, check the simulation and press Apply. " +
            "Subfolders are created and the files are moved inside the phone. Undo reverts the " +
            "last organization done on the phone."));

        // ---------------------------------------------------- actualizacion --
        Section(Lang.T("Actualizaciones", "Updates"));
        Paragraph(Lang.T(
            "Al arrancar comprueba si hay una versión nueva en GitHub y, si la hay, te ofrece " +
            "descargarla e instalarla. También puedes buscarla a mano en Ayuda > Buscar " +
            "actualizaciones.",
            "On start-up it checks GitHub for a newer version and, if there is one, offers to " +
            "download and install it. You can also check manually in Help > Check for updates."));

        // ---------------------------------------------------------- consola --
        Section(Lang.T("Versión de consola", "Console version"));
        Paragraph(Lang.T(
            "organizador.exe hace lo mismo sin ventana (Organizador.bat abre un menú). " +
            "Opciones: --dir <ruta>, --apply, --by-date, --recursive, --rule .ext=Carpeta, " +
            "--undo <registro.csv>, --color / --no-color, --quiet, --help.",
            "organizador.exe does the same without a window (Organizador.bat opens a menu). " +
            "Options: --dir <path>, --apply, --by-date, --recursive, --rule .ext=Folder, " +
            "--undo <log.csv>, --color / --no-color, --quiet, --help."));

        // -------------------------------------------------- donde se guarda --
        Section(Lang.T("Dónde se guarda todo", "Where things are stored"));
        Bullet(Lang.T("Registros para deshacer: en la propia carpeta organizada (.organizador-log-*.csv).",
                      "Undo logs: in the organized folder itself (.organizador-log-*.csv)."));
        Bullet(Lang.T("Reglas y registros del móvil: %LOCALAPPDATA%\\FileNest.",
                      "Rules and phone logs: %LOCALAPPDATA%\\FileNest."));
        Bullet(Lang.T("Preferencias (tema, última carpeta, tamaño de ventana): HKCU\\Software\\FileNest.",
                      "Preferences (theme, last folder, window size): HKCU\\Software\\FileNest."));

        Paragraph(Lang.T("La versión de este programa es la " + AppInfo.Version + ".",
                         "This program is version " + AppInfo.Version + "."));
    }

    // ------------------------------------------------------------- helpers --

    private void Section(string text)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 18, 0, 6),
        };
        block.SetResourceReference(TextBlock.ForegroundProperty, "AccentBr");
        Body.Children.Add(block);
    }

    private void Paragraph(string text) => Add(text, new Thickness(0, 0, 0, 6));

    private void Bullet(string text) => Add("•   " + text, new Thickness(10, 0, 0, 4));

    private void Add(string text, Thickness margin)
    {
        Body.Children.Add(new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Margin = margin,
        });
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
