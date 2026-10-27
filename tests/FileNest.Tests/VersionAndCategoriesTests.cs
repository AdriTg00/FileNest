using FileNest.Core;
using Xunit;

namespace FileNest.Tests;

public class VersionAndCategoriesTests
{
    [Theory]
    [InlineData("v1.0.2", "1.0.1", 1)]
    [InlineData("v1.0.3", "1.0.3", 0)]
    [InlineData("1.0.10", "1.0.9", 1)]
    [InlineData("v2.0.0", "1.9.9", 1)]
    [InlineData("1.2", "1.2.0", 0)]
    [InlineData("v0.9", "1.0.0", -1)]
    public void ComparesVersionsByNumbers(string a, string b, int expectedSign)
    {
        int result = Updater.CompareVersions(a, b);

        Assert.Equal(expectedSign, Math.Sign(result));
    }

    [Fact]
    public void CurrentVersionIsNewerThanTheOldCppRelease()
    {
        Assert.True(Updater.CompareVersions(AppInfo.Version, "1.0.3") > 0);
    }

    [Fact]
    public void AppInfoVersionLooksLikeXyz()
    {
        string[] parts = AppInfo.Version.Split('.');

        Assert.Equal(3, parts.Length);
        Assert.All(parts, part => Assert.True(int.TryParse(part, out _)));
    }

    [Fact]
    public void BothLanguagesShareTheSameExtensions()
    {
        // Las tablas es/en solo deben cambiar los NOMBRES de carpeta, no las extensiones.
        var spanish = GetTable(isSpanish: true);
        var english = GetTable(isSpanish: false);

        Assert.Equal(spanish.Count, english.Count);

        for (int i = 0; i < spanish.Count; ++i)
            Assert.Equal(spanish[i].Extensions, english[i].Extensions);
    }

    [Fact]
    public void NoExtensionBelongsToTwoCategories()
    {
        var seen = new Dictionary<string, string>();

        foreach (var category in Categories.Default)
        {
            foreach (string extension in category.Extensions)
            {
                Assert.False(seen.ContainsKey(extension),
                    $"la extension .{extension} esta en '{seen.GetValueOrDefault(extension)}' y en '{category.Name}'");
                seen[extension] = category.Name;
            }
        }
    }

    [Fact]
    public void CategoryNamesCoverSpanishAndEnglish()
    {
        var names = Categories.AllNames();

        Assert.Contains("Imágenes", names);
        Assert.Contains("Images", names);
        Assert.Contains("Otros", names);
        Assert.Contains("Other", names);
    }

    [Fact]
    public void UnknownCategoryHasGenericIcon()
    {
        Assert.Equal("🏷", Categories.IconOf("Categoria inventada"));
        Assert.Equal("📎", Categories.IconOf(Categories.OtherName));
    }

    [Fact]
    public void HumanSizeIsReadable()
    {
        Assert.Equal("512 B", Paths.HumanSize(512));
        Assert.Contains("KB", Paths.HumanSize(2048));
        Assert.Contains("MB", Paths.HumanSize(5 * 1024 * 1024));
    }

    /// <summary>Accede a la tabla del otro idioma sin cambiar el idioma del proceso.</summary>
    private static IReadOnlyList<CategoryDef> GetTable(bool isSpanish)
    {
        var field = typeof(Categories).GetField(isSpanish ? "Spanish" : "English",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(field);
        return (IReadOnlyList<CategoryDef>)field!.GetValue(null)!;
    }
}
