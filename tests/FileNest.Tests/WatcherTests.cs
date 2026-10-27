using FileNest.Core;
using Xunit;

namespace FileNest.Tests;

public class WatcherTests
{
    private static Options OptionsFor(string directory) => new() { Directory = directory };

    /// <summary>Nombre real de la categoria (depende del idioma del sistema: Documentos/Documents).</summary>
    /// <summary>Envejece un archivo (modificacion Y creacion: si solo se toca la de modificacion,
    /// cuenta como recien llegado porque las copias conservan la fecha del original).</summary>
    private static void Age(string path, DateTime when)
    {
        File.SetLastWriteTime(path, when);
        File.SetCreationTime(path, when);
    }

    private static string PdfCategory =>
        Categories.Default.First(category => category.Extensions.Contains("pdf")).Name;

    [Fact]
    public void OnlyFilesThatHaveBeenIdleAreDue()
    {
        using var temp = new TempDir();
        string old = temp.File("viejo.pdf");
        string fresh = temp.File("recien.pdf");
        Age(old, DateTime.Now.AddMinutes(-10));

        Plan plan = Watcher.BuildDue(OptionsFor(temp.Path), TimeSpan.FromMinutes(2));

        Assert.Single(plan.Moves);
        Assert.Equal("viejo.pdf", plan.Moves[0].FileName);
        Assert.NotEqual(fresh, plan.Moves[0].From);
    }

    [Fact]
    public void FilesOlderThanTheWatchStartAreNotTouched()
    {
        using var temp = new TempDir();
        string before = temp.File("de-antes.pdf");
        string after = temp.File("de-despues.pdf");

        DateTime old = DateTime.Now.AddMinutes(-30);
        File.SetLastWriteTime(before, old);
        File.SetCreationTime(before, old);
        Age(after, DateTime.Now.AddMinutes(-10));

        Plan plan = Watcher.BuildDue(OptionsFor(temp.Path), TimeSpan.FromMinutes(2),
                                     notBefore: DateTime.Now.AddMinutes(-20));

        Assert.Single(plan.Moves);
        Assert.Equal("de-despues.pdf", plan.Moves[0].FileName);
    }

    [Fact]
    public void CopiedFileWithAnOldDateStillCountsAsNew()
    {
        using var temp = new TempDir();
        string copied = temp.File("copiado.pdf");

        // Copiar un archivo antiguo conserva su fecha de modificacion, pero la de creacion es de ahora.
        File.SetLastWriteTime(copied, new DateTime(2024, 5, 5, 10, 0, 0));

        Plan plan = Watcher.BuildDue(OptionsFor(temp.Path), TimeSpan.FromMinutes(2),
                                     now: DateTime.Now.AddMinutes(3),
                                     notBefore: DateTime.Now.AddMinutes(-1));

        Assert.Single(plan.Moves);
        Assert.Equal("copiado.pdf", plan.Moves[0].FileName);
    }

    [Fact]
    public void RulesAndTemplatesAreApplied()
    {
        using var temp = new TempDir();
        string file = temp.File("factura-2024.pdf");
        Age(file, new DateTime(2024, 3, 15, 10, 0, 0));

        var options = OptionsFor(temp.Path);
        options.CustomRules.Add(new Rule("factura*.pdf", "Facturas\\{año}"));

        Plan plan = Watcher.BuildDue(options, TimeSpan.FromMinutes(2));

        Assert.Equal(Path.Combine(temp.Path, "Facturas", "2024", "factura-2024.pdf"), plan.Moves[0].To);
        Assert.Equal("Facturas", plan.Moves[0].Category);
    }

    [Fact]
    public void PlanIsEmptyWhenEverythingIsRecent()
    {
        using var temp = new TempDir();
        temp.File("a.pdf");
        temp.File("b.jpg");

        Plan plan = Watcher.BuildDue(OptionsFor(temp.Path), TimeSpan.FromMinutes(2));

        Assert.Empty(plan.Moves);
        Assert.Equal(2, plan.Analyzed);
    }

    [Fact]
    public void SweepMovesOnlyTheIdleFiles()
    {
        using var temp = new TempDir();
        string old = temp.File("viejo.pdf");
        temp.File("nuevo.pdf");
        Age(old, DateTime.Now.AddMinutes(-10));

        WatchReport report = Watcher.Sweep(OptionsFor(temp.Path), TimeSpan.FromMinutes(2));

        Assert.Equal(1, report.Moved);
        Assert.Equal(0, report.Errors);
        Assert.True(report.SomethingMoved);
        Assert.True(File.Exists(Path.Combine(temp.Path, PdfCategory, "viejo.pdf")));
        Assert.True(File.Exists(Path.Combine(temp.Path, "nuevo.pdf")));
        Assert.Single(Undoer.FindLogs(temp.Path));
    }

    [Fact]
    public void SweepDoesNothingWhenThereIsNothingDue()
    {
        using var temp = new TempDir();
        temp.File("a.pdf");

        WatchReport report = Watcher.Sweep(OptionsFor(temp.Path));

        Assert.Equal(0, report.Moved);
        Assert.False(report.SomethingMoved);
        Assert.False(File.Exists(Path.Combine(temp.Path, PdfCategory, "a.pdf")));
    }
}
