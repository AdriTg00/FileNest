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

    /// <summary>
    /// Registro local que se escribe FILA A FILA, justo antes de mover cada archivo.
    ///
    /// Es lo que hace que "Deshacer" sea de fiar: si el programa se cierra a mitad
    /// (o el disco se llena), todo lo que ya se movio esta apuntado. Si una fila se
    /// quedo apuntada pero su archivo no llego a moverse, <see cref="Undoer"/> la
    /// ignora porque el destino no existe.
    /// </summary>
    public sealed class LocalLog : IDisposable
    {
        private readonly StreamWriter _writer;

        private LocalLog(string logPath, string stamp, StreamWriter writer)
        {
            LogPath = logPath;
            Stamp = stamp;
            _writer = writer;
        }

        public string LogPath { get; }

        /// <summary>Marca de tiempo que va en la columna "fecha" (la misma del nombre).</summary>
        public string Stamp { get; }

        /// <summary>
        /// Crea el registro. El nombre lleva segundos; si ya hay uno de ese mismo
        /// segundo se anade "-2", "-3"... para no machacarlo (dos ejecuciones
        /// seguidas compartirian nombre y la primera perderia su deshacer).
        /// </summary>
        public static LocalLog Create(string folder)
        {
            string stamp = Paths.NowStamp();

            for (int i = 1; i < 1000; ++i)
            {
                string name = i == 1
                    ? Paths.LogPrefix + stamp + ".csv"
                    : $"{Paths.LogPrefix}{stamp}-{i}.csv";

                string path = Path.Combine(folder, name);

                FileStream stream;
                try
                {
                    stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                }
                catch (IOException) when (File.Exists(path))
                {
                    continue;                       // ya existe: se prueba el siguiente
                }

                var writer = new StreamWriter(stream, new UTF8Encoding(false));
                writer.Write(Header + "\r\n");
                writer.Flush();
                return new LocalLog(path, stamp, writer);
            }

            throw new IOException("no se pudo crear un nombre libre para el registro");
        }

        /// <summary>Anota un movimiento y lo deja escrito antes de devolver el control.</summary>
        public void Record(MoveItem move)
        {
            _writer.Write("MOVE," + Field(move.From) + "," + Field(move.To) + ","
                          + Field(Stamp) + "\r\n");
            _writer.Flush();
        }

        public void Dispose() => _writer.Dispose();
    }

    /// <summary>Escribe el registro de una organizacion hecha DENTRO del movil.</summary>
    public static string WritePhoneLog(string remoteFolder, IReadOnlyList<(string Name, string Folder)> done)
    {
        string path = Path.Combine(Paths.DataDirectory, "movil-" + Paths.NowStamp() + ".csv");

        var sb = new StringBuilder();
        sb.Append("accion,ruta_original,ruta_destino\r\n");
        foreach (var (name, folder) in done)
        {
            string origin = remoteFolder + "/" + name;
            string target = remoteFolder + "/" + folder + "/" + name;
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
