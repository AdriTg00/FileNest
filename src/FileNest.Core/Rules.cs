using System.Text;

namespace FileNest.Core;

/// <summary>
/// Reglas propias del usuario (%LOCALAPPDATA%\FileNest\reglas.txt).
/// Formato: una linea "extension=carpeta". Las lineas con # o ; son comentarios.
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

            string ext = Planner.NormalizeExtension(line[..eq]);
            string category = line[(eq + 1)..].Trim();
            if (category.Length == 0) continue;

            rules.Add(new Rule(ext, category));
        }

        return rules;
    }

    public static void Save(IEnumerable<Rule> rules) => Save(rules, FilePath);

    public static void Save(IEnumerable<Rule> rules, string path)
    {
        var sb = new StringBuilder();
        sb.Append("# FileNest: reglas de extension=carpeta (una por linea)\r\n");
        foreach (var rule in rules)
            sb.Append(Planner.NormalizeExtension(rule.Extension)).Append('=').Append(rule.Category).Append("\r\n");

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Paths.DataDirectory);
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }
}
