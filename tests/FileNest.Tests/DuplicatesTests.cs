using System.Text;
using FileNest.Core;
using Xunit;

namespace FileNest.Tests;

public class DuplicatesTests
{
    private static Options OptionsFor(string directory) => new() { Directory = directory };

    private static string Write(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(false));
        return path;
    }

    [Fact]
    public void FindsFilesWithTheSameContent()
    {
        using var temp = new TempDir();
        temp.File("a.pdf", "mismo contenido");
        temp.File("b.pdf", "mismo contenido");
        temp.File("c.pdf", "otra cosa");

        DuplicateReport report = Duplicates.Find(OptionsFor(temp.Path));

        Assert.True(report.Any);
        DuplicateGroup group = Assert.Single(report.Groups);
        Assert.Equal(2, group.Files.Count);
        Assert.Equal(report.WastedBytes, group.Size);
        Assert.Equal(1, report.ExtraFiles);
    }

    [Fact]
    public void SameSizeDifferentContentIsNotADuplicate()
    {
        using var temp = new TempDir();
        temp.File("a.txt", "AAAA");
        temp.File("b.txt", "BBBB");

        DuplicateReport report = Duplicates.Find(OptionsFor(temp.Path));

        Assert.False(report.Any);
    }

    [Fact]
    public void EmptyFilesAreIgnored()
    {
        using var temp = new TempDir();
        temp.File("a.txt", "");
        temp.File("b.txt", "");

        DuplicateReport report = Duplicates.Find(OptionsFor(temp.Path));

        Assert.False(report.Any);
    }

    [Fact]
    public void ThreeCopiesReportTwoExtras()
    {
        using var temp = new TempDir();
        temp.File("a.bin", "1234567890");
        temp.File("b.bin", "1234567890");
        temp.File("c.bin", "1234567890");

        DuplicateReport report = Duplicates.Find(OptionsFor(temp.Path));

        Assert.Single(report.Groups);
        Assert.Equal(2, report.ExtraFiles);
        Assert.Equal(20, report.WastedBytes);
    }

    [Fact]
    public void OldestFileComesFirst()
    {
        using var temp = new TempDir();
        string old = temp.File("viejo.bin", "iguales");
        string recent = temp.File("nuevo.bin", "iguales");
        File.SetLastWriteTime(old, new DateTime(2020, 1, 1, 10, 0, 0));
        File.SetLastWriteTime(recent, new DateTime(2025, 1, 1, 10, 0, 0));

        DuplicateReport report = Duplicates.Find(OptionsFor(temp.Path));

        Assert.Equal(Path.Combine(temp.Path, "viejo.bin"), report.Groups[0].Files[0].Path);
    }

    [Fact]
    public void RecursiveIsOptional()
    {
        using var temp = new TempDir();
        temp.File("a.bin", "contenido");
        temp.File(Path.Combine("sub", "b.bin"), "contenido");

        Assert.False(Duplicates.Find(OptionsFor(temp.Path)).Any);

        var options = OptionsFor(temp.Path);
        options.Recursive = true;

        Assert.True(Duplicates.Find(options).Any);
    }

    [Fact]
    public void MoveToFolderKeepsTheOthersAndWritesTheLog()
    {
        using var temp = new TempDir();
        temp.File("a.bin", "iguales");
        temp.File("b.bin", "iguales");

        DuplicateReport report = Duplicates.Find(OptionsFor(temp.Path));
        DuplicateGroup group = report.Groups[0];

        ApplyResult result = Duplicates.MoveToFolder(OptionsFor(temp.Path),
            group.Files.Skip(1).Select(file => file.Path));

        Assert.Equal(1, result.Moved);
        Assert.True(File.Exists(Path.Combine(temp.Path, "a.bin")));
        Assert.True(File.Exists(Path.Combine(temp.Path, Duplicates.FolderName, "b.bin")));
        Assert.Single(Undoer.FindLogs(temp.Path));
    }

    [Fact]
    public void MoveToFolderDoesNotOverwrite()
    {
        using var temp = new TempDir();
        temp.File("b.bin", "iguales");
        temp.File(Path.Combine(Duplicates.FolderName, "b.bin"), "ya estaba");

        Duplicates.MoveToFolder(OptionsFor(temp.Path), new[] { Path.Combine(temp.Path, "b.bin") });

        Assert.True(File.Exists(Path.Combine(temp.Path, Duplicates.FolderName, "b (1).bin")));
        Assert.Equal("ya estaba", File.ReadAllText(Path.Combine(temp.Path, Duplicates.FolderName, "b.bin")));
    }

    [Fact]
    public void MovingDuplicatesCanBeUndone()
    {
        using var temp = new TempDir();
        temp.File("a.bin", "iguales");
        temp.File("b.bin", "iguales");

        DuplicateReport report = Duplicates.Find(OptionsFor(temp.Path));
        ApplyResult result = Duplicates.MoveToFolder(OptionsFor(temp.Path),
            report.Groups[0].Files.Skip(1).Select(file => file.Path));

        UndoResult undone = Undoer.Undo(result.LogPath);

        Assert.Equal(1, undone.Restored);
        Assert.True(File.Exists(Path.Combine(temp.Path, "b.bin")));
        Assert.False(Directory.Exists(Path.Combine(temp.Path, Duplicates.FolderName)));
    }

    [Fact]
    public void TheQuarantineFolderIsNeverScannedAgain()
    {
        using var temp = new TempDir();
        temp.File("a.bin", "iguales");
        temp.File("b.bin", "iguales");

        // Las dos variantes (es/en), por si el idioma cambio entre ejecuciones.
        temp.File(Path.Combine("Duplicados", "b.bin"), "iguales");
        temp.File(Path.Combine("Duplicates", "b.bin"), "iguales");

        var options = OptionsFor(temp.Path);
        options.Recursive = true;

        DuplicateReport report = Duplicates.Find(options);

        DuplicateGroup group = Assert.Single(report.Groups);
        Assert.Equal(2, group.Files.Count);              // a.bin y b.bin, no las apartadas
    }

    [Fact]
    public void RescanningAfterMovingFindsNoDuplicates()
    {
        using var temp = new TempDir();
        temp.File("a.bin", "iguales");
        temp.File("b.bin", "iguales");

        var options = OptionsFor(temp.Path);
        options.Recursive = true;

        DuplicateReport first = Duplicates.Find(options);
        Duplicates.MoveToFolder(options, first.Groups[0].Files.Skip(1).Select(file => file.Path));

        DuplicateReport second = Duplicates.Find(options);

        Assert.False(second.Any);
        Assert.Equal(0, second.ExtraFiles);
    }

    [Fact]
    public void FilesThatCannotBeReadAreReported()
    {
        using var temp = new TempDir();
        string locked = temp.File("a.bin", "contenido");
        temp.File("b.bin", "contenido");

        DuplicateReport report;
        using (new FileStream(locked, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            report = Duplicates.Find(OptionsFor(temp.Path));
        }

        Assert.Equal(1, report.Unreadable);
        Assert.False(report.Any);
    }

    [Fact]
    public void TheFolderNameIsOneOfTheKnownNames()
    {
        Assert.Contains(Duplicates.FolderName, Duplicates.FolderNames);
        Assert.True(Duplicates.IsFolderName("duplicados"));
        Assert.True(Duplicates.IsFolderName("DUPLICATES"));
        Assert.False(Duplicates.IsFolderName("Duplicados 2"));
    }
}
