using FileNest.Core;
using Xunit;

namespace FileNest.Tests;

public class RuleEngineTests
{
    private static DateTime Date(int year, int month, int day) => new(year, month, day, 10, 0, 0);

    [Fact]
    public void ExtensionShortcutMatchesExtensionOnly()
    {
        var rule = new Rule(".pdf", "Papeles");

        Assert.True(RuleEngine.Matches(rule, "informe.pdf"));
        Assert.True(RuleEngine.Matches(rule, "INFORME.PDF"));
        Assert.True(RuleEngine.Matches(rule, "a.b.pdf"));
        Assert.False(RuleEngine.Matches(rule, "informe.pdf.txt"));
        Assert.False(RuleEngine.Matches(rule, "informe.docx"));
    }

    [Fact]
    public void WildcardsMatchTheFileName()
    {
        var rule = new Rule("factura*.pdf", "Facturas");

        Assert.True(RuleEngine.Matches(rule, "factura-2024.pdf"));
        Assert.True(RuleEngine.Matches(rule, "FACTURA.pdf"));
        Assert.False(RuleEngine.Matches(rule, "informe.pdf"));
        Assert.False(RuleEngine.Matches(rule, "factura-2024.pdf.bak"));
    }

    [Fact]
    public void QuestionMarkMatchesOneCharacter()
    {
        var rule = new Rule("informe?.txt", "Notas");

        Assert.True(RuleEngine.Matches(rule, "informe1.txt"));
        Assert.False(RuleEngine.Matches(rule, "informe12.txt"));
        Assert.False(RuleEngine.Matches(rule, "informe.txt"));
    }

    [Fact]
    public void ExactNameMatchesExactly()
    {
        var rule = new Rule("presupuesto 2026.pdf", "Presupuestos");

        Assert.True(RuleEngine.Matches(rule, "presupuesto 2026.pdf"));
        Assert.True(RuleEngine.Matches(rule, "PRESUPUESTO 2026.PDF"));
        Assert.False(RuleEngine.Matches(rule, "presupuesto 2025.pdf"));
    }

    [Fact]
    public void FirstMatchingRuleWins()
    {
        var rules = new List<Rule> { new("*.psd", "Diseno"), new(".psd", "Otros") };

        Assert.Equal("Diseno", RuleEngine.FirstMatch(rules, "cartel.psd")?.Target);
        Assert.Null(RuleEngine.FirstMatch(rules, "informe.pdf"));
    }

    [Fact]
    public void ResolvesTheTokens()
    {
        var rule = new Rule("*.pdf", "Facturas\\{año}\\{mes}\\{ext}\\{nombre}");

        RuleTarget target = RuleEngine.Resolve(rule, "factura marzo.pdf", Date(2024, 3, 15));

        Assert.Equal("Facturas\\2024\\03\\pdf\\factura marzo", target.Folder);
        Assert.Equal("Facturas", target.Category);
    }

    [Fact]
    public void EnglishTokensWork()
    {
        var rule = new Rule("*.pdf", "Invoices\\{year}\\{month}\\{day}\\{name}");

        RuleTarget target = RuleEngine.Resolve(rule, "invoice.pdf", Date(2024, 3, 7));

        Assert.Equal("Invoices\\2024\\03\\07\\invoice", target.Folder);
    }

    [Fact]
    public void UnknownTokensAreLeftAlone()
    {
        var rule = new Rule("*.pdf", "Cosas\\{desconocido}");

        Assert.Equal("Cosas\\{desconocido}", RuleEngine.Resolve(rule, "a.pdf", Date(2024, 1, 1)).Folder);
    }

    [Fact]
    public void InvalidCharactersAreReplaced()
    {
        var rule = new Rule("*.pdf", "A:B\\C?D");

        Assert.Equal("A_B\\C_D", RuleEngine.Resolve(rule, "a.pdf", Date(2024, 1, 1)).Folder);
    }

    [Fact]
    public void EmptyTargetFallsBackToOther()
    {
        var rule = new Rule("*.pdf", "   ");

        Assert.Equal(Categories.OtherName, RuleEngine.Resolve(rule, "a.pdf", Date(2024, 1, 1)).Category);
    }

    [Fact]
    public void DetectsDateTokens()
    {
        Assert.True(RuleEngine.HasDateTokens("Facturas\\{año}"));
        Assert.True(RuleEngine.HasDateTokens("Facturas\\{year}"));
        Assert.True(RuleEngine.HasDateTokens("Facturas\\{mes}"));
        Assert.False(RuleEngine.HasDateTokens("Facturas\\{ext}\\{nombre}"));
        Assert.False(RuleEngine.HasDateTokens("Facturas"));
    }

    [Fact]
    public void StaticRootIsTheFirstFixedFolder()
    {
        Assert.Equal("Facturas", RuleEngine.StaticRoot("Facturas\\{año}"));
        Assert.Equal("Facturas", RuleEngine.StaticRoot("Facturas"));
        Assert.Equal("", RuleEngine.StaticRoot("{año}\\Facturas"));
    }

    [Theory]
    [InlineData("pdf", ".pdf")]
    [InlineData("PDF", ".pdf")]
    [InlineData(".PDF", ".pdf")]
    [InlineData("factura*.PDF", "factura*.PDF")]
    [InlineData("informe.pdf", "informe.pdf")]
    [InlineData("  *.psd  ", "*.psd")]
    public void NormalizesPatterns(string input, string expected)
    {
        Assert.Equal(expected, RuleEngine.NormalizePattern(input));
    }
}
