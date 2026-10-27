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
        }, path);

        var loaded = Rules.Load(path);

        Assert.Equal(2, loaded.Count);
        Assert.Equal(".psd", loaded[0].Extension);
        Assert.Equal("Diseno", loaded[0].Category);
        Assert.Equal(".raw", loaded[1].Extension);
        Assert.Equal("Fotos", loaded[1].Category);
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
            "=sin-extension\r\n" +
            "sin-carpeta=\r\n" +
            " .pdf = Papeles \r\n";

        File.WriteAllText(path, content, new UTF8Encoding(false));

        var loaded = Rules.Load(path);

        Assert.Single(loaded);
        Assert.Equal(".pdf", loaded[0].Extension);
        Assert.Equal("Papeles", loaded[0].Category);
    }

    [Fact]
    public void MissingFileReturnsEmptyList()
    {
        using var temp = new TempDir();

        Assert.Empty(Rules.Load(Path.Combine(temp.Path, "no-existe.txt")));
    }
}
