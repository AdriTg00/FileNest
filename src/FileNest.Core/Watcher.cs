namespace FileNest.Core;

/// <summary>Resultado de una pasada del modo vigilancia.</summary>
public sealed record WatchReport(int Moved, int Errors, string LogName)
{
    public bool SomethingMoved => Moved > 0;
}

/// <summary>
/// Modo vigilancia: organiza SOLO lo que llega mientras esta activo y lleva un rato
/// sin tocarse (2 minutos por defecto). Asi no se mueve una descarga a medias ni un
/// archivo que se esta copiando, y no se toca el monton que ya habia en la carpeta.
/// </summary>
public static class Watcher
{
    public static readonly TimeSpan DefaultIdle = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Plan normal, pero solo con los archivos que:
    ///   - no son anteriores a "notBefore" (lo que ya estaba no se toca), y
    ///   - llevan al menos "idle" sin cambios (no se estan escribiendo).
    /// </summary>
    public static Plan BuildDue(Options options, TimeSpan idle, DateTime? now = null, DateTime? notBefore = null)
    {
        DateTime reference = now ?? DateTime.Now;
        Plan plan = Planner.Build(options);

        var due = new Plan
        {
            Analyzed = plan.Analyzed,
            Skipped = plan.Skipped,
        };

        foreach (var move in plan.Moves)
        {
            DateTime written;
            try
            {
                written = File.GetLastWriteTime(move.From);
            }
            catch
            {
                continue;
            }

            if (notBefore.HasValue && written < notBefore.Value) continue;
            if (reference - written < idle) continue;

            due.Moves.Add(move);
        }

        foreach (var move in due.Moves)
            due.ByCategory[move.Category] = due.ByCategory.GetValueOrDefault(move.Category) + 1;

        return due;
    }

    /// <summary>Una pasada completa: calcula lo que toca y lo mueve (con su registro para deshacer).</summary>
    public static WatchReport Sweep(Options options, TimeSpan? idle = null, DateTime? now = null,
                                    DateTime? notBefore = null)
    {
        Plan due = BuildDue(options, idle ?? DefaultIdle, now, notBefore);
        if (due.Moves.Count == 0) return new WatchReport(0, 0, "");

        ApplyResult result = Applier.Apply(due, options.Directory);
        return new WatchReport((int)result.Moved, (int)result.Errors, result.LogName);
    }
}
