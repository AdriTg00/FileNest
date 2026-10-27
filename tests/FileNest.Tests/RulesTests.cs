using System.Text;
using FileNest.Core;
using Xunit;

namespace FileNest.Tests;

public class RulesTests
{
    [Fact]
    public void SaveAndLoadRoundTrip()
    {
        using var temp = new TempDir();
        string path = Path.Combine(temp.Path, "reglas.txt");

        Rules.Save(new[]
        {
            new Rule("psd", "Diseno"),
            new Rule(".RAW", "Fotos"),
            new Rule("factura*.pdf", "Facturas\\{año}\\{mes}"),
            new Rule("informe.pdf", "Notas"),
        }, path);

        var loaded = Rules.Load(path);

        Assert.Equal(4, loaded.Count);
        Assert.Equal(".psd", loaded[0].Pattern);
        Assert.Equal("Diseno", loaded[0].Target);
        Assert.Equal(".raw", loaded[1].Pattern);
        Assert.Equal("Fotos", loaded[1].Target);
        Assert.Equal("factura*.pdf", loaded[2].Pattern);
        Assert.Equal("Facturas\\{año}\\{mes}", loaded[2].Target);
        Assert.Equal("informe.pdf", loaded[3].Pattern);
    }

    [Fact]
    public void CommentsAndBrokenLinesAreIgnored()
    {
        using var temp = new TempDir();
        string path = Path.Combine(temp.Path, "reglas.txt");

        string content =
            "# comentario\r\n" +
            "; otro comentario\r\n" +
            "\r\n" +
            "sin-igual\r\n" +
            "=sin-patron\r\n" +
            "sin-carpeta=\r\n" +
            " .pdf = Papeles \r\n";

        File.WriteAllText(path, content, new UTF8Encoding(false));

        var loaded = Rules.Load(path);

        Assert.Single(loaded);
        Assert.Equal(".pdf", loaded[0].Pattern);
        Assert.Equal("Papeles", loaded[0].Target);
    }

    [Fact]
    public void OldFormatStillWorks()
    {
        // Formato de la 1.x: ".ext=carpeta" (sin comodines ni variables)
        using var temp = new TempDir();
        string path = Path.Combine(temp.Path, "reglas.txt");
        File.WriteAllText(path, ".psd=Diseño\r\n.mp4=Vídeos\r\n", new UTF8Encoding(false));

        var loaded = Rules.Load(path);

        Assert.Equal(2, loaded.Count);
        Assert.Equal("Diseño", loaded[0].Target);
        Assert.True(RuleEngine.Matches(loaded[0], "cartel.psd"));
    }

    [Fact]
    public void MissingFileReturnsEmptyList()
    {
        using var temp = new TempDir();

        Assert.Empty(Rules.Load(Path.Combine(temp.Path, "no-existe.txt")));
    }
}
