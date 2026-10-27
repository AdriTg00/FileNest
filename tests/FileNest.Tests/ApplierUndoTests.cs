using FileNest.Core;
using Xunit;

namespace FileNest.Tests;

public class ApplierUndoTests
{
    [Fact]
    public void ApplyMovesFilesAndWritesLog()
    {
        using var temp = new TempDir();
        temp.File("a.pdf", "contenido");

        var options = new Options { Directory = temp.Path };
        Plan plan = Planner.Build(options);

        ApplyResult result = Applier.Apply(plan, temp.Path);

        Assert.Equal(1, result.Moved);
        Assert.Equal(0, result.Errors);
        Assert.True(File.Exists(result.LogPath));
        Assert.False(File.Exists(Path.Combine(temp.Path, "a.pdf")));
        Assert.True(File.Exists(Path.Combine(temp.Path, plan.Moves[0].Category, "a.pdf")));
    }

    [Fact]
    public void LogIsFoundInTheFolder()
    {
        using var temp = new TempDir();
        temp.File("a.pdf");

        Plan plan = Planner.Build(new Options { Directory = temp.Path });
        Applier.Apply(plan, temp.Path);

        List<string> logs = Undoer.FindLogs(temp.Path);

        Assert.Single(logs);
        Assert.StartsWith(Paths.LogPrefix, Path.GetFileName(logs[0]));
    }

    [Fact]
    public void UndoRestoresFiles()
    {
        using var temp = new TempDir();
        temp.File("a.pdf", "contenido");
        temp.File("b.jpg", "foto");

        var options = new Options { Directory = temp.Path };
        Plan plan = Planner.Build(options);
        ApplyResult applied = Applier.Apply(plan, temp.Path);

        UndoResult undone = Undoer.Undo(applied.LogPath);

        Assert.Equal(2, undone.Restored);
        Assert.Equal(0, undone.Errors);
        Assert.True(File.Exists(Path.Combine(temp.Path, "a.pdf")));
        Assert.True(File.Exists(Path.Combine(temp.Path, "b.jpg")));
        Assert.Equal("contenido", File.ReadAllText(Path.Combine(temp.Path, "a.pdf")));
    }

    [Fact]
    public void UndoDoesNotOverwriteAnExistingFile()
    {
        using var temp = new TempDir();
        temp.File("a.pdf", "original");

        Plan plan = Planner.Build(new Options { Directory = temp.Path });
        ApplyResult applied = Applier.Apply(plan, temp.Path);

        // Alguien vuelve a crear el archivo en su sitio antes de deshacer
        temp.File("a.pdf", "nuevo");

        UndoResult undone = Undoer.Undo(applied.LogPath);

        Assert.Equal(0, undone.Restored);
        Assert.Equal(1, undone.Skipped);
        Assert.Equal("nuevo", File.ReadAllText(Path.Combine(temp.Path, "a.pdf")));
    }

    [Fact]
    public void UndoSkipsFilesThatAreGone()
    {
        using var temp = new TempDir();
        temp.File("a.pdf");

        Plan plan = Planner.Build(new Options { Directory = temp.Path });
        ApplyResult applied = Applier.Apply(plan, temp.Path);

        File.Delete(plan.Moves[0].To);

        UndoResult undone = Undoer.Undo(applied.LogPath);

        Assert.Equal(0, undone.Restored);
        Assert.Equal(1, undone.Skipped);
    }

    [Fact]
    public void MissingLogDoesNothing()
    {
        UndoResult result = Undoer.Undo(Path.Combine(Path.GetTempPath(), "no-existe-" + Guid.NewGuid() + ".csv"));

        Assert.Equal(0, result.Restored + result.Skipped + result.Errors);
    }
}
