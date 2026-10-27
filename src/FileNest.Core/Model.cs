namespace FileNest.Core;

/// <summary>
/// Regla propia: patron de archivo (o extension) -> carpeta destino
/// (la carpeta puede llevar variables: {año}, {mes}, {ext}, {nombre}...).
/// </summary>
public sealed record Rule(string Pattern, string Target);

/// <summary>Opciones de una organizacion.</summary>
public sealed class Options
{
    public string Directory { get; set; } = Paths.DefaultTargetDirectory;
    public bool ByDate { get; set; }
    public bool Recursive { get; set; }
    public List<Rule> CustomRules { get; } = new();
}

/// <summary>Un movimiento previsto: de donde a donde y en que categoria cae.</summary>
public sealed record MoveItem(string From, string To, string Category)
{
    public string FileName => Path.GetFileName(From);
}

/// <summary>Resultado de simular: que se moveria y estadisticas.</summary>
public sealed class Plan
{
    public List<MoveItem> Moves { get; } = new();
    public long Analyzed { get; set; }
    public long Skipped { get; set; }
    public SortedDictionary<string, long> ByCategory { get; } = new(StringComparer.Ordinal);
}

/// <summary>Resultado de aplicar.</summary>
public sealed class ApplyResult
{
    public long Moved { get; set; }
    public long Errors { get; set; }
    public List<MoveItem> Done { get; } = new();
    public string LogPath { get; set; } = "";
    public string LogName { get; set; } = "";
}

/// <summary>Resultado de deshacer.</summary>
public sealed class UndoResult
{
    public long Restored { get; set; }
    public long Skipped { get; set; }
    public long Errors { get; set; }
}
