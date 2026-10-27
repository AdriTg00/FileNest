using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace FileNest.Core;

/// <summary>Destino resuelto de una regla: carpeta relativa y su primer nivel (la "categoria").</summary>
public sealed record RuleTarget(string Folder, string Category);

/// <summary>
/// Reglas propias con patron y plantilla.
///
///   patron  = nombre de archivo con comodines (* y ?) o una extension (.pdf).
///             Ejemplos: "*.psd", "factura*.pdf", "informe.pdf", ".pdf"
///   destino = carpeta, con variables:
///             {año}/{year}, {mes}/{month}, {día}/{day}, {ext}, {nombre}/{name}
///             Ejemplos: "Diseno", "Facturas\{año}", "Archivo\{año}\{mes}"
///
/// Se aplica la PRIMERA regla que coincide, en el orden en que estan en la lista.
/// </summary>
public static class RuleEngine
{
    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly char[] Wildcards = { '*', '?' };

    private static readonly char[] Separators = { '\\', '/' };

    private static readonly Regex DateToken = new(
        @"\{(año|ano|year|mes|month|día|dia|day)\}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>Primera regla que casa con el nombre de archivo (null si ninguna).</summary>
    public static Rule? FirstMatch(IEnumerable<Rule> rules, string fileName)
    {
        foreach (var rule in rules)
            if (Matches(rule, fileName)) return rule;

        return null;
    }

    public static bool Matches(Rule rule, string fileName)
    {
        string pattern = rule.Pattern.Trim();
        if (pattern.Length == 0) return false;

        bool hasWildcards = pattern.IndexOfAny(Wildcards) >= 0;

        // ".pdf" -> extension (lo de siempre)
        if (pattern[0] == '.' && !hasWildcards)
            return string.Equals(Path.GetExtension(fileName), pattern, StringComparison.OrdinalIgnoreCase);

        // Sin comodines: nombre exacto
        if (!hasWildcards)
            return string.Equals(fileName, pattern, StringComparison.OrdinalIgnoreCase);

        return RegexCache.GetOrAdd(pattern, BuildRegex).IsMatch(fileName);
    }

    private static Regex BuildRegex(string pattern)
    {
        var sb = new StringBuilder("^");

        foreach (char c in pattern)
        {
            if (c == '*') sb.Append(".*");
            else if (c == '?') sb.Append('.');
            else sb.Append(Regex.Escape(c.ToString()));
        }

        sb.Append('$');
        return new Regex(sb.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    /// <summary>Resuelve la plantilla para un archivo (escritura local) y limpia la ruta.</summary>
    public static RuleTarget Resolve(Rule rule, string fileName, DateTime written)
    {
        string expanded = Expand(rule.Target, fileName, written);

        var parts = expanded
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(Sanitize)
            .Where(part => part.Length > 0)
            .ToArray();

        if (parts.Length == 0)
            return new RuleTarget(Categories.OtherName, Categories.OtherName);

        return new RuleTarget(string.Join("\\", parts), parts[0]);
    }

    /// <summary>Primer nivel de la plantilla, si es fijo (sin variables): sirve para no entrar en esa carpeta.</summary>
    public static string StaticRoot(string target)
    {
        string? first = target
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        if (first == null || first.Contains('{')) return "";
        return Sanitize(first);
    }

    /// <summary>True si la plantilla ya lleva la fecha (para no anadir tambien Anio\Mes).</summary>
    public static bool HasDateTokens(string target) => DateToken.IsMatch(target);

    /// <summary>"pdf" -> ".pdf"; el resto se queda igual (solo se recorta).</summary>
    public static string NormalizePattern(string pattern)
    {
        string value = pattern.Trim();
        if (value.Length == 0) return value;

        bool hasWildcards = value.IndexOfAny(Wildcards) >= 0;

        // "pdf" (sin punto y sin comodines) era una extension; "informe.pdf" es un nombre.
        if (!hasWildcards && value[0] != '.' && value.IndexOf('.') < 0)
            value = "." + value;

        if (!hasWildcards && value[0] == '.')
            value = value.ToLowerInvariant();

        return value;
    }

    private static string Expand(string template, string fileName, DateTime written)
    {
        var sb = new StringBuilder(template.Length + 16);

        for (int i = 0; i < template.Length; ++i)
        {
            if (template[i] == '{')
            {
                int end = template.IndexOf('}', i + 1);
                if (end > i)
                {
                    string? value = TokenValue(template[(i + 1)..end].Trim(), fileName, written);
                    if (value != null)
                    {
                        sb.Append(value);
                        i = end;
                        continue;
                    }
                }
            }

            sb.Append(template[i]);
        }

        return sb.ToString();
    }

    private static string? TokenValue(string token, string fileName, DateTime written)
    {
        switch (token.ToLowerInvariant())
        {
            case "año":
            case "ano":
            case "year":
                return written.Year.ToString("0000");

            case "mes":
            case "month":
                return written.Month.ToString("00");

            case "día":
            case "dia":
            case "day":
                return written.Day.ToString("00");

            case "ext":
            {
                string extension = Path.GetExtension(fileName).TrimStart('.');
                return extension.Length > 0 ? extension.ToLowerInvariant() : Categories.OtherName.ToLowerInvariant();
            }

            case "nombre":
            case "name":
                return Path.GetFileNameWithoutExtension(fileName);

            default:
                return null;                      // variable desconocida: se deja tal cual
        }
    }

    /// <summary>Quita caracteres que Windows no admite en un nombre de carpeta.</summary>
    private static string Sanitize(string part)
    {
        char[] invalid = Path.GetInvalidFileNameChars();

        var cleaned = new StringBuilder(part.Length);
        foreach (char c in part)
            cleaned.Append(invalid.Contains(c) ? '_' : c);

        return cleaned.ToString().Trim().TrimEnd('.');
    }
}
