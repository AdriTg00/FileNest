using System.Windows;
using FileNest.App.Services;
using FileNest.Core;

namespace FileNest.App.Dialogs;

/// <summary>Reglas propias: extension -> carpeta.</summary>
public partial class RulesWindow : Window
{
    private readonly List<Rule> _rules;

    /// <summary>Reglas resultantes (solo valido si el dialogo devuelve true).</summary>
    public IReadOnlyList<Rule> Rules => _rules;

    public RulesWindow(IEnumerable<Rule> rules)
    {
        InitializeComponent();

        _rules = rules.ToList();
        Title = Strings.RulesTitle;
        TitleBar.Attach(this);

        Refresh();
        Loaded += (_, _) => ExtensionBox.Focus();
    }

    private void Refresh()
    {
        RulesList.ItemsSource = _rules.Select(r => $"{r.Extension}   →   {r.Category}").ToList();
    }

    private void OnAdd(object sender, RoutedEventArgs e)
    {
        string extension = ExtensionBox.Text.Trim();
        string category = CategoryBox.Text.Trim();

        if (extension.Length == 0 || category.Length == 0)
        {
            MessageBox.Show(
                Lang.T("Escribe la extensión y la carpeta.", "Enter the extension and the folder."),
                Strings.RulesTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _rules.Add(new Rule(Planner.NormalizeExtension(extension), category));

        ExtensionBox.Text = "";
        CategoryBox.Text = "";
        Refresh();
        ExtensionBox.Focus();
    }

    private void OnRemove(object sender, RoutedEventArgs e)
    {
        int selected = RulesList.SelectedIndex;
        if (selected < 0 || selected >= _rules.Count) return;

        _rules.RemoveAt(selected);
        Refresh();
    }

    private void OnSave(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
