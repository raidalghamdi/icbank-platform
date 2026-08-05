using FluentAssertions;
using Icbank.Platform.Application.InternationalDays;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.InternationalDays;

/// <summary>
/// Proves DEFECT-LOG.md SEC-21/H-1 is closed: AI-generated content containing an HTML/script
/// payload is encoded, not executed, in the exported document.
/// </summary>
public sealed class InternationalDayHtmlExportBuilderTests
{
    private const string MaliciousPayload = "<script>alert('xss')</script>";

    [Fact]
    public void Build_HistorySummaryContainsScriptTag_IsHtmlEncodedNotExecutable()
    {
        InternationalDayExportModel model = BuildModel(historySummary: MaliciousPayload);

        var html = InternationalDayHtmlExportBuilder.Build(model);

        html.Should().NotContain("<script>alert('xss')</script>");
        html.Should().Contain("&lt;script&gt;");
    }

    [Fact]
    public void Build_ActivationDescriptionContainsScriptTag_IsHtmlEncoded()
    {
        var activation = new InternationalDayExportActivation("Entity", "حكومي", "منشور", MaliciousPayload, "السعودية", 2025, null, false);
        InternationalDayExportModel model = BuildModel(activations: new[] { activation });

        var html = InternationalDayHtmlExportBuilder.Build(model);

        html.Should().NotContain("<script>alert('xss')</script>");
        html.Should().Contain("&lt;script&gt;");
    }

    [Fact]
    public void Build_SuggestionContainsScriptTag_IsHtmlEncoded()
    {
        InternationalDayExportModel model = BuildModel(suggestions: new[] { MaliciousPayload });

        var html = InternationalDayHtmlExportBuilder.Build(model);

        html.Should().NotContain("<script>alert('xss')</script>");
    }

    [Fact]
    public void Build_SourceTitleContainsScriptTag_IsHtmlEncoded()
    {
        var source = new InternationalDayExportSource("https://example.com", MaliciousPayload, "Publisher");
        InternationalDayExportModel model = BuildModel(sources: new[] { source });

        var html = InternationalDayHtmlExportBuilder.Build(model);

        html.Should().NotContain("<script>alert('xss')</script>");
    }

    private static InternationalDayExportModel BuildModel(
        string? historySummary = null,
        IReadOnlyList<InternationalDayExportActivation>? activations = null,
        IReadOnlyList<string>? suggestions = null,
        IReadOnlyList<InternationalDayExportSource>? sources = null) => new(
        "اليوم العالمي للغة العربية",
        "World Arabic Language Day",
        "18 ديسمبر",
        "UNESCO",
        "لغات",
        historySummary,
        null,
        "2026",
        "Theme",
        "Theme EN",
        null,
        activations ?? Array.Empty<InternationalDayExportActivation>(),
        suggestions ?? Array.Empty<string>(),
        sources ?? Array.Empty<InternationalDayExportSource>(),
        "5 أغسطس 2026");
}
