namespace FileNest.Core;

/// <summary>
/// Aplica un plan (mueve archivos) y deja el registro para deshacerlo.
///
/// El registro se escribe ANTES de cada movimiento. Si no se puede crear, no se
/// mueve nada; si se corta a mitad, lo ya movido sigue apuntado y se puede
/// deshacer. Nunca se mueve un archivo sin su fila en el registro.
/// </summary>
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

        // Sin movimientos no hay nada que registrar (ni un CSV vacio que luego confunda).
        if (plan.Moves.Count == 0) return result;

        CsvLog.LocalLog log;
        try
        {
            log = CsvLog.LocalLog.Create(baseDirectory);
        }
        catch (Exception ex)
        {
            // Sin registro no hay deshacer: se prefiere no tocar nada.
            result.LogFailed = true;
            result.LogError = ex.Message;
            return result;
        }

        result.LogPath = log.LogPath;
        result.LogName = Path.GetFileName(log.LogPath);

        using (log)
        {
            foreach (var move in plan.Moves)
            {
                // 1) Apuntar. Si esto falla (disco lleno, permisos...), se para aqui:
                //    lo que quede sin mover es mejor que moverlo sin poder deshacerlo.
                try
                {
                    log.Record(move);
                }
                catch (Exception ex)
                {
                    result.LogFailed = true;
                    result.LogError = ex.Message;
                    break;
                }

                // 2) Mover. Si falla, la fila ya escrita se queda y Undoer la saltara.
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
        }

        return result;
    }
}
