using System.Text;

namespace FileNest.Core;

/// <summary>
/// Formato de los registros (CSV) que permiten deshacer.
/// Se mantiene IDENTICO al de la version en C++ para que los registros antiguos
/// (.organizador-log-*.csv y movil-*.csv) se puedan seguir deshaciendo.
/// </summary>
public static class CsvLog
{
    public const string Header = "accion,ruta_original,ruta_destino,fecha";

    /// <summary>Escapa un campo solo si hace falta (coma, comilla o salto).</summary>
    public static string Field(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>Escribe el registro de una ejecucion local.</summary>
    public static string WriteLocalLog(string folder, IReadOnlyList<MoveItem> done, string stamp)
    {
        string name = Paths.LogPrefix + stamp + ".csv";
        string path = Path.Combine(folder, name);

        var sb = new StringBuilder();
        sb.Append(Header).Append("\r\n");
        foreach (var m in done)
            sb.Append("MOVE,").Append(Field(m.From)).Append(',').Append(Field(m.To))
              .Append(',').Append(Field(stamp)).Append("\r\n");

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        return path;
    }

    /// <summary>Escribe el registro de una organizacion hecha DENTRO del movil.</summary>
    public static string WritePhoneLog(string remoteFolder, IReadOnlyList<(string Name, string Category)> done)
    {
        string path = Path.Combine(Paths.DataDirectory, "movil-" + Paths.NowStamp() + ".csv");

        var sb = new StringBuilder();
        sb.Append("accion,ruta_original,ruta_destino\r\n");
        foreach (var (name, category) in done)
        {
            string origin = remoteFolder + "/" + name;
            string target = remoteFolder + "/" + category + "/" + name;
            sb.Append("MOVE,").Append(Field(origin)).Append(',').Append(Field(target)).Append("\r\n");
        }

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        return path;
    }

    /// <summary>Filas del CSV (cabecera incluida). Tolera comillas y saltos dentro de campos.</summary>
    public static List<List<string>> Parse(string text)
    {
        var rows = new List<List<string>>();
        var fields = new List<string>();
        var field = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; ++i)
        {
            char ch = text[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); ++i; }
                    else inQuotes = false;
                }
                else field.Append(ch);
            }
            else
            {
                switch (ch)
                {
                    case '"': inQuotes = true; break;
                    case ',': fields.Add(field.ToString()); field.Clear(); break;
                    case '\n': fields.Add(field.ToString()); field.Clear(); rows.Add(fields); fields = new List<string>(); break;
                    case '\r': break;
                    default: field.Append(ch); break;
                }
            }
        }

        if (field.Length > 0 || fields.Count > 0)
        {
            fields.Add(field.ToString());
            rows.Add(fields);
        }

        return rows;
    }

    /// <summary>Movimientos (origen, destino) de un registro, sin la cabecera ni ruido.</summary>
    public static List<(string From, string To)> ReadMoves(string logPath)
    {
        var moves = new List<(string, string)>();
        if (!File.Exists(logPath)) return moves;

        string text = File.ReadAllText(logPath, Encoding.UTF8);
        bool first = true;
        foreach (var row in Parse(text))
        {
            if (row.Count < 3) continue;
            if (first) { first = false; continue; }
            if (row[0] != "MOVE") continue;
            moves.Add((row[1], row[2]));
        }

        return moves;
    }
}
