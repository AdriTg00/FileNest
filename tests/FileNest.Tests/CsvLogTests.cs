using System.Text;
using FileNest.Core;
using Xunit;

namespace FileNest.Tests;

public class CsvLogTests
{
    [Fact]
    public void PlainFieldsAreNotQuoted()
    {
        Assert.Equal("C:\\a\\b.pdf", CsvLog.Field("C:\\a\\b.pdf"));
    }

    [Fact]
    public void FieldsWithCommasOrQuotesAreEscaped()
    {
        Assert.Equal("\"a,b\"", CsvLog.Field("a,b"));
        Assert.Equal("\"dice \"\"hola\"\"\"", CsvLog.Field("dice \"hola\""));
        Assert.Equal("\"linea\r\nnueva\"", CsvLog.Field("linea\r\nnueva"));
    }

    [Fact]
    public void ParserRoundTripsQuotedFields()
    {
        string text = "accion,ruta_original,ruta_destino\r\n" +
                      "MOVE,\"C:\\a,b\\f.pdf\",\"C:\\x\"\"y\\f.pdf\"\r\n";

        var rows = CsvLog.Parse(text);

        Assert.Equal(2, rows.Count);
        Assert.Equal("MOVE", rows[1][0]);
        Assert.Equal("C:\\a,b\\f.pdf", rows[1][1]);
        Assert.Equal("C:\\x\"y\\f.pdf", rows[1][2]);
    }

    [Fact]
    public void ReadMovesIgnoresHeaderAndOtherRows()
    {
        using var temp = new TempDir();
        string log = Path.Combine(temp.Path, ".organizador-log-20260101-120000.csv");

        string content =
            "accion,ruta_original,ruta_destino,fecha\r\n" +
            "MOVE,C:\\a\\uno.pdf,C:\\b\\uno.pdf,20260101-120000\r\n" +
            "OTRA,C:\\a\\dos.pdf,C:\\b\\dos.pdf,20260101-120000\r\n";

        File.WriteAllText(log, content, new UTF8Encoding(false));

        var moves = CsvLog.ReadMoves(log);

        Assert.Single(moves);
        Assert.Equal("C:\\a\\uno.pdf", moves[0].From);
        Assert.Equal("C:\\b\\uno.pdf", moves[0].To);
    }

    [Fact]
    public void PhoneLogHasThreeColumns()
    {
        using var temp = new TempDir();
        var done = new List<(string Name, string Folder)> { ("foto.jpg", "Imágenes") };

        // Se escribe en la carpeta de datos real (no queremos tocar el %LOCALAPPDATA% del usuario
        // mas de lo necesario): se comprueba el formato leyendo el archivo generado.
        string path = CsvLog.WritePhoneLog("/sdcard/Download", done);
        try
        {
            string[] lines = File.ReadAllLines(path);
            Assert.Equal(2, lines.Length);
            Assert.Equal("accion,ruta_original,ruta_destino", lines[0]);
            Assert.Equal("MOVE,/sdcard/Download/foto.jpg,/sdcard/Download/Imágenes/foto.jpg", lines[1]);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
