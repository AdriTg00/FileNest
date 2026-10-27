namespace FileNest.Core;

/// <summary>Aplica un plan (mueve archivos) y deja el registro para deshacerlo.</summary>
public static class Applier
{
    /// <summary>
    /// Mueve los archivos del plan dejando el registro en <paramref name="baseDirectory"/>.
    /// <paramref name="progress"/> recibe cada movimiento: (movimiento, ok, mensajeDeError).
    /// </summary>
    public static ApplyResult Apply(Plan plan, string baseDirectory,
                                    Action<MoveItem, bool, string>? progress = null)
    {
        var result = new ApplyResult();

        foreach (var move in plan.Moves)
        {
            try
            {
                string? parent = Path.GetDirectoryName(move.To);
                if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);

                File.Move(move.From, move.To);
                ++result.Moved;
                result.Done.Add(move);
                progress?.Invoke(move, true, "");
            }
            catch (Exception ex)
            {
                ++result.Errors;
                progress?.Invoke(move, false, ex.Message);
            }
        }

        string stamp = Paths.NowStamp();
        result.LogName = Paths.LogPrefix + stamp + ".csv";
        result.LogPath = Path.Combine(baseDirectory, result.LogName);

        try
        {
            result.LogPath = CsvLog.WriteLocalLog(baseDirectory, result.Done, stamp);
            result.LogName = Path.GetFileName(result.LogPath);
        }
        catch
        {
            // Si no se puede escribir el registro, los archivos ya estan movidos:
            // no se cuenta como error de la operacion, pero no habra deshacer.
        }

        return result;
    }
}
