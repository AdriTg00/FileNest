using FileNest.Core;
using Xunit;

namespace FileNest.Tests;

public class WatcherTests
{
    private static Options OptionsFor(string directory) => new() { Directory = directory };

    [Fact]
    public void OnlyFilesThatHaveBeenIdleAreDue()
    {
        using var temp = new TempDir();
        string old = temp.File("viejo.pdf");
        string fresh = temp.File("recien.pdf");
        File.SetLastWriteTime(old, DateTime.Now.AddMinutes(-10));

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
        File.SetLastWriteTime(before, DateTime.Now.AddMinutes(-30));
        File.SetLastWriteTime(after, DateTime.Now.AddMinutes(-10));

        Plan plan = Watcher.BuildDue(OptionsFor(temp.Path), TimeSpan.FromMinutes(2),
                                     notBefore: DateTime.Now.AddMinutes(-20));

        Assert.Single(plan.Moves);
        Assert.Equal("de-despues.pdf", plan.Moves[0].FileName);
    }

    [Fact]
    public void RulesAndTemplatesAreApplied()
    {
        using var temp = new TempDir();
        string file = temp.File("factura-2024.pdf");
        File.SetLastWriteTime(file, new DateTime(2024, 3, 15, 10, 0, 0));

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
        File.SetLastWriteTime(old, DateTime.Now.AddMinutes(-10));

        WatchReport report = Watcher.Sweep(OptionsFor(temp.Path), TimeSpan.FromMinutes(2));

        Assert.Equal(1, report.Moved);
        Assert.Equal(0, report.Errors);
        Assert.True(report.SomethingMoved);
        Assert.True(File.Exists(Path.Combine(temp.Path, "Documentos", "viejo.pdf")));
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
        Assert.False(File.Exists(Path.Combine(temp.Path, "Documentos", "a.pdf")));
    }
}
