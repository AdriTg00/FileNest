using System.Windows;
using System.Windows.Controls;
using FileNest.App.Services;
using FileNest.Core;

namespace FileNest.App.Dialogs;

/// <summary>
/// Reglas propias: patron de archivo -> carpeta (con variables).
/// El orden manda: se aplica la primera que coincide.
/// </summary>
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
        Loaded += (_, _) => PatternBox.Focus();
    }

    private void Refresh(int selected = -1)
    {
        RulesList.ItemsSource = null;
        RulesList.ItemsSource = _rules.Select(r => $"{r.Pattern}   →   {r.Target}").ToList();

        if (_rules.Count == 0) return;

        int index = selected < 0 ? _rules.Count - 1 : Math.Min(selected, _rules.Count - 1);
        RulesList.SelectedIndex = index;
        RulesList.ScrollIntoView(RulesList.SelectedItem);
    }

    private void OnAdd(object sender, RoutedEventArgs e)
    {
        string pattern = RuleEngine.NormalizePattern(PatternBox.Text);
        string target = TargetBox.Text.Trim();

        if (pattern.Length == 0 || target.Length == 0)
        {
            MessageBox.Show(
                Lang.T("Escribe el archivo o patrón y la carpeta destino.",
                       "Enter the file or pattern and the target folder."),
                Strings.RulesTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _rules.Add(new Rule(pattern, target));

        PatternBox.Text = "";
        TargetBox.Text = "";
        Refresh();
        PatternBox.Focus();
    }

    private void OnRemove(object sender, RoutedEventArgs e)
    {
        int selected = RulesList.SelectedIndex;
        if (selected < 0 || selected >= _rules.Count) return;

        _rules.RemoveAt(selected);
        Refresh(selected);
    }

    private void OnUp(object sender, RoutedEventArgs e) => Move(-1);

    private void OnDown(object sender, RoutedEventArgs e) => Move(+1);

    private void Move(int delta)
    {
        int selected = RulesList.SelectedIndex;
        int destination = selected + delta;

        if (selected < 0 || destination < 0 || destination >= _rules.Count) return;

        (_rules[selected], _rules[destination]) = (_rules[destination], _rules[selected]);
        Refresh(destination);
    }

    private void OnSave(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
