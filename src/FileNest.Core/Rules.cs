using System.Text;

namespace FileNest.Core;

/// <summary>
/// Reglas propias del usuario (%LOCALAPPDATA%\FileNest\reglas.txt).
/// Formato: una linea "patron=carpeta" (el formato no ha cambiado desde la 1.x).
/// Las lineas que empiezan por # o ; son comentarios.
/// El patron admite comodines (* y ?) y la carpeta variables ({año}, {mes}, {ext}...);
/// ver RuleEngine.
/// </summary>
public static class Rules
{
    public static string FilePath => Path.Combine(Paths.DataDirectory, "reglas.txt");

    public static List<Rule> Load() => Load(FilePath);

    public static List<Rule> Load(string path)
    {
        var rules = new List<Rule>();
        if (!File.Exists(path)) return rules;

        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line[0] == '#' || line[0] == ';') continue;

            int eq = line.IndexOf('=');
            if (eq <= 0 || eq + 1 >= line.Length) continue;

            string pattern = RuleEngine.NormalizePattern(line[..eq]);
            string target = line[(eq + 1)..].Trim();
            if (pattern.Length == 0 || target.Length == 0) continue;

            rules.Add(new Rule(pattern, target));
        }

        return rules;
    }

    public static void Save(IEnumerable<Rule> rules) => Save(rules, FilePath);

    public static void Save(IEnumerable<Rule> rules, string path)
    {
        var sb = new StringBuilder();
        sb.Append("# FileNest: reglas patron=carpeta (una por linea, se aplica la primera que coincide)\r\n");
        sb.Append("# Patrones: *.psd, factura*.pdf, informe.pdf, .pdf\r\n");
        sb.Append("# Variables: {año} {mes} {día} {ext} {nombre}\r\n");

        foreach (var rule in rules)
            sb.Append(RuleEngine.NormalizePattern(rule.Pattern)).Append('=').Append(rule.Target).Append("\r\n");

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Paths.DataDirectory);
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }
}
