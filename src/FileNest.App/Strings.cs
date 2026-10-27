using FileNest.Core;

namespace FileNest.App;

/// <summary>Textos de la interfaz, en el idioma que toque (segun Windows o FILENEST_LANG).</summary>
public static class Strings
{
    // ventana principal
    public static string Folder => Lang.T("Carpeta:", "Folder:");
    public static string Watch => Lang.T("Vigilar esta carpeta", "Watch this folder");
    public static string WatchTitle => Lang.T("Vigilar esta carpeta", "Watch this folder");
    public static string Duplicates => Lang.T("Duplicados…", "Duplicates…");
    public static string DuplicatesTitle => Lang.T("Duplicados por contenido", "Duplicates by content");
    public static string LookingForDuplicates => Lang.T("Buscando duplicados…", "Looking for duplicates…");
    public static string NoDuplicates => Lang.T("No hay archivos repetidos en esta carpeta.",
                                               "There are no repeated files in this folder.");
    public static string MoveToDuplicates => Lang.T("Mover a Duplicados", "Move to Duplicates");
    public static string DuplicatesHint => Lang.T(
        "Compara por contenido, no por nombre. Los que sobran se mueven a la carpeta " +
        "«Duplicados»: nunca se borra nada y se puede deshacer.",
        "Compares by content, not by name. The extra ones are moved to the " +
        "«Duplicates» folder: nothing is ever deleted and it can be undone.");
    public static string TrayOpen => Lang.T("Abrir FileNest", "Open FileNest");
    public static string TrayStopWatching => Lang.T("Dejar de vigilar", "Stop watching");
    public static string TrayExit => Lang.T("Salir", "Quit");
    public static string ChooseFolder => Lang.T("Elegir carpeta…", "Choose folder…");
    public static string Phone => Lang.T("Móvil", "Phone");
    public static string Rules => Lang.T("Reglas…", "Rules…");
    public static string ThemeLight => Lang.T("Tema: claro", "Theme: light");
    public static string ThemeDark => Lang.T("Tema: oscuro", "Theme: dark");
    public static string GroupByDate => Lang.T("Agrupar por fecha (Año\\Mes)", "Group by date (Year\\Month)");
    public static string IncludeSubfolders => Lang.T("Incluir subcarpetas", "Include subfolders");

    // columnas
    public static string ColFile => Lang.T("Archivo", "File");
    public static string ColSize => Lang.T("Tamaño", "Size");
    public static string ColCategory => Lang.T("Categoría", "Category");
    public static string ColTarget => Lang.T("Destino", "Target");
    public static string ColStatus => Lang.T("Estado", "Status");

    // botones
    public static string Simulate => Lang.T("Simular", "Simulate");
    public static string Apply => Lang.T("Aplicar", "Apply");
    public static string Undo => Lang.T("Deshacer", "Undo");
    public static string OpenFolder => Lang.T("Abrir carpeta", "Open folder");

    // menu
    public static string Help => Lang.T("Ayuda", "Help");
    public static string Manual => Lang.T("Manual de instrucciones…", "Instructions…");
    public static string ManualTitle => Lang.T("Manual de FileNest", "FileNest manual");
    public static string Close => Lang.T("Cerrar", "Close");
    public static string CheckUpdates => Lang.T("Buscar actualizaciones…", "Check for updates…");
    public static string About => Lang.T("Acerca de FileNest", "About FileNest");

    // estados de fila
    public static string Pending => Lang.T("pendiente", "pending");
    public static string Moved => Lang.T("movido", "moved");
    public static string Failed => Lang.T("error", "error");
    public static string Seen => Lang.T("visto", "seen");

    // historial
    public static string HistoryTitle => Lang.T("Deshacer: elige una ejecución", "Undo: choose a run");
    public static string Cancel => Lang.T("Cancelar", "Cancel");

    // reglas
    public static string RulesTitle => Lang.T("Reglas (patrón → carpeta)", "Rules (pattern → folder)");
    public static string ExtensionLabel => Lang.T("Archivo o patrón:", "File or pattern:");
    public static string FolderLabel => Lang.T("Carpeta destino:", "Target folder:");
    public static string Add => Lang.T("Añadir", "Add");
    public static string Remove => Lang.T("Quitar", "Remove");
    public static string MoveUp => Lang.T("Subir", "Up");
    public static string MoveDown => Lang.T("Bajar", "Down");
    public static string RulesHint => Lang.T(
        "Se aplica la primera regla que coincide (usa Subir/Bajar para el orden).\n" +
        "Patrones: *.psd, factura*.pdf, informe.pdf, .pdf.\n" +
        "Variables en la carpeta: {año} {mes} {día} {ext} {nombre}.",
        "The first matching rule wins (use Up/Down to reorder).\n" +
        "Patterns: *.psd, invoice*.pdf, report.pdf, .pdf.\n" +
        "Folder variables: {year} {month} {day} {ext} {name}.");
    public static string Save => Lang.T("Guardar", "Save");

    // carpeta del movil
    public static string PhoneFolderTitle => Lang.T("Carpeta del móvil", "Phone folder");
    public static string PathLabel => Lang.T("Ruta:", "Path:");
    public static string Up => Lang.T("Subir", "Up");
    public static string UseFolder => Lang.T("Usar esta carpeta", "Use this folder");
    public static string DoubleClickHint => Lang.T("Doble clic en una carpeta para entrar.",
                                                   "Double-click a folder to go into it.");

    // avisos
    public static string UpdateTitle => Lang.T("Actualización disponible", "Update available");
    public static string UpdatesTitle => Lang.T("Actualizaciones", "Updates");
    public static string UpdateTitleShort => Lang.T("Actualización", "Update");
    public static string AboutTitle => Lang.T("Acerca de FileNest", "About FileNest");
    public static string PhoneTitle => Lang.T("Móvil", "Phone");
}
