using FileNest.Core;

namespace FileNest.App;

/// <summary>Textos de la interfaz, en el idioma que toque (segun Windows o FILENEST_LANG).</summary>
public static class Strings
{
    // ventana principal
    public static string Folder => Lang.T("Carpeta:", "Folder:");
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
    public static string RulesTitle => Lang.T("Reglas (extensión → carpeta)", "Rules (extension → folder)");
    public static string ExtensionLabel => Lang.T("Extensión:", "Extension:");
    public static string FolderLabel => Lang.T("Carpeta:", "Folder:");
    public static string Add => Lang.T("Añadir", "Add");
    public static string Remove => Lang.T("Quitar", "Remove");
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
