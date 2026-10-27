using FileNest.Core;
using Xunit;

namespace FileNest.Tests;

public class PlannerTests
{
    private static string CategoryFor(string extension)
    {
        foreach (var category in Categories.Default)
            if (category.Extensions.Contains(extension)) return category.Name;

        throw new InvalidOperationException("extension desconocida: " + extension);
    }

    private static Options OptionsFor(string directory) => new() { Directory = directory };

    [Fact]
    public void ClassifiesByExtension()
    {
        using var temp = new TempDir();
        temp.File("informe.pdf");
        temp.File("foto.jpg");
        temp.File("raro.xyz");

        Plan plan = Planner.Build(OptionsFor(temp.Path));

        Assert.Equal(3, plan.Analyzed);
        Assert.Equal(3, plan.Moves.Count);

        Assert.Equal(CategoryFor("pdf"), Move(plan, "informe.pdf").Category);
        Assert.Equal(CategoryFor("jpg"), Move(plan, "foto.jpg").Category);
        Assert.Equal(Categories.OtherName, Move(plan, "raro.xyz").Category);
    }

    [Fact]
    public void EmptyFolderIsTidy()
    {
        using var temp = new TempDir();

        Plan plan = Planner.Build(OptionsFor(temp.Path));

        Assert.Empty(plan.Moves);
        Assert.Equal(0, plan.Analyzed);
    }

    [Fact]
    public void GroupsByYearAndMonth()
    {
        using var temp = new TempDir();
        string file = temp.File("viejo.pdf");
        File.SetLastWriteTime(file, new DateTime(2021, 3, 15, 10, 30, 0));

        var options = OptionsFor(temp.Path);
        options.ByDate = true;

        Plan plan = Planner.Build(options);
        string target = Move(plan, "viejo.pdf").To;

        Assert.Contains(Path.Combine("2021", "03"), target);
    }

    [Fact]
    public void AvoidsCollisionsWithExistingFiles()
    {
        using var temp = new TempDir();
        temp.File("a.pdf", "nuevo");
        temp.File(Path.Combine(CategoryFor("pdf"), "a.pdf"), "ya existia");

        Plan plan = Planner.Build(OptionsFor(temp.Path));
        string target = Move(plan, "a.pdf").To;

        Assert.Equal("a (1).pdf", Path.GetFileName(target));
    }

    [Fact]
    public void SkipsHiddenAndSystemFiles()
    {
        using var temp = new TempDir();
        string hidden = temp.File("oculto.pdf");
        File.SetAttributes(hidden, FileAttributes.Hidden);

        Plan plan = Planner.Build(OptionsFor(temp.Path));

        Assert.Empty(plan.Moves);
        Assert.Equal(1, plan.Skipped);
    }

    [Fact]
    public void SkipsDotFilesAndWindowsJunk()
    {
        using var temp = new TempDir();
        temp.File(".gitignore");
        temp.File("desktop.ini");
        temp.File("Thumbs.db");
        temp.File("normal.pdf");

        Plan plan = Planner.Build(OptionsFor(temp.Path));

        Assert.Single(plan.Moves);
        Assert.Equal("normal.pdf", plan.Moves[0].FileName);
    }

    [Fact]
    public void RecursiveIncludesSubfoldersButNotCategoryFolders()
    {
        using var temp = new TempDir();
        temp.File("suelto.pdf");
        temp.File(Path.Combine("varios", "dentro.pdf"));
        temp.File(Path.Combine(CategoryFor("pdf"), "categoria.pdf"));

        var options = OptionsFor(temp.Path);
        options.Recursive = true;

        Plan plan = Planner.Build(options);

        var names = plan.Moves.Select(m => m.FileName).ToHashSet();
        Assert.Contains("suelto.pdf", names);
        Assert.Contains("dentro.pdf", names);
        Assert.DoesNotContain("categoria.pdf", names);
    }

    [Fact]
    public void NotRecursiveIgnoresSubfolders()
    {
        using var temp = new TempDir();
        temp.File(Path.Combine("varios", "dentro.pdf"));

        Plan plan = Planner.Build(OptionsFor(temp.Path));

        Assert.Empty(plan.Moves);
    }

    [Fact]
    public void CustomRuleOverridesDefault()
    {
        using var temp = new TempDir();
        temp.File("plano.pdf");

        var options = OptionsFor(temp.Path);
        options.CustomRules.Add(new Rule(".pdf", "Papeles"));

        Plan plan = Planner.Build(options);

        Assert.Equal("Papeles", plan.Moves[0].Category);
        Assert.Equal("Papeles", Path.GetFileName(Path.GetDirectoryName(plan.Moves[0].To)));
    }

    [Fact]
    public void CountsByCategory()
    {
        using var temp = new TempDir();
        temp.File("a.pdf");
        temp.File("b.pdf");
        temp.File("c.jpg");

        Plan plan = Planner.Build(OptionsFor(temp.Path));

        Assert.Equal(2, plan.ByCategory[CategoryFor("pdf")]);
        Assert.Equal(1, plan.ByCategory[CategoryFor("jpg")]);
    }

    private static MoveItem Move(Plan plan, string fileName) =>
        plan.Moves.Single(m => m.FileName == fileName);
}
