using FluentAssertions;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Infrastructure.Gemini;
using Icbank.Platform.Infrastructure.MediaMonitoring;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.Gemini;

/// <summary>
/// Tests for <see cref="GeminiFinalReportSectionGenerator"/> — the highest-stakes, longest-output
/// call in the system (pro model tier, 8192 max output tokens, the canonical 8-section
/// BUSINESS-RULES.md §5.3 prompt). Covers the call shape, the top-level field mapping via
/// <see cref="FinalReportSectionsMapper"/>, and its documented all-empty-defaults behaviour for
/// every absent nested section (not an exhaustive per-field enumeration of all 8 sections — the
/// mapper's <c>?? []</c>/<c>?? string.Empty</c> defaulting is structurally uniform and read
/// directly during adapter review; see GEMINI-ADAPTER-NOTES.md).
/// </summary>
public sealed class GeminiFinalReportSectionGeneratorTests
{
    private readonly IGeminiClient _client = Substitute.For<IGeminiClient>();
    private readonly GeminiFinalReportSectionGenerator _sut;

    public GeminiFinalReportSectionGeneratorTests()
    {
        _sut = new GeminiFinalReportSectionGenerator(_client, new GeminiOptions());
    }

    [Fact]
    public async Task GenerateAsync_UsesProModel_At8192MaxOutputTokens()
    {
        var options = new GeminiOptions { TextModel = "gemini-text", ProModel = "gemini-pro-final" };
        var sut = new GeminiFinalReportSectionGenerator(_client, options);
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("{}"));

        await sut.GenerateAsync("١-٧ يناير", "manager", null, "بيانات", CancellationToken.None);

        await _client.Received(1).GenerateJsonAsync(
            Arg.Any<string>(),
            Arg.Is<GeminiCallOptions>(o => o.Model == "gemini-pro-final" && o.MaxOutputTokens == 8192),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_CapsThinkingBudget_SoReasoningTokensCannotStarveTheJsonAnswer()
    {
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("{}"));

        await _sut.GenerateAsync("١-٧ يناير", "manager", null, "بيانات", CancellationToken.None);

        await _client.Received(1).GenerateJsonAsync(
            Arg.Any<string>(),
            Arg.Is<GeminiCallOptions>(o => o.ThinkingBudget == 512),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_BuildsVerbatimPrompt_WithPeriodAudienceAndFeedInterpolated()
    {
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("{}"));

        await _sut.GenerateAsync("١-٧ يناير", "manager", "التركيز على الرياضة", "بيانات الفترة", CancellationToken.None);

        await _client.Received(1).GenerateJsonAsync(
            Arg.Is<string>(p =>
                p.Contains("أنت محلل إعلامي خبير يعمل لدى الهيئة العامة للمنافسة في المملكة العربية السعودية", StringComparison.Ordinal) &&
                p.Contains("الفترة: ١-٧ يناير", StringComparison.Ordinal) &&
                p.Contains("الجمهور المستهدف: manager", StringComparison.Ordinal) &&
                p.Contains("بيانات الفترة", StringComparison.Ordinal)),
            Arg.Any<GeminiCallOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_MapsTopLevelFields()
    {
        const string json = """
            {
              "executiveSummary": "ملخص شامل",
              "kpis": {"totalNews":10,"positivePercent":60,"mediaOutlets":5,"keyTopics":3,"reach":"1M","alertsCount":1},
              "methodology": "منهجية جمع البيانات"
            }
            """;
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text(json));

        FinalReportSections result = await _sut.GenerateAsync("period", "manager", null, "feed", CancellationToken.None);

        result.ExecutiveSummary.Should().Be("ملخص شامل");
        result.Kpis.TotalNews.Should().Be(10);
        result.Kpis.Reach.Should().Be("1M");
        result.Methodology.Should().Be("منهجية جمع البيانات");
    }

    [Fact]
    public async Task GenerateAsync_AllSectionsAbsent_DefaultsToEmptyListsAndEmptyStrings_NeverNull()
    {
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("{}"));

        FinalReportSections result = await _sut.GenerateAsync("period", "manager", null, "feed", CancellationToken.None);

        result.ExecutiveSummary.Should().BeEmpty();
        result.Methodology.Should().BeEmpty();
        result.TopNews.Should().NotBeNull().And.BeEmpty();
        result.Timeline.Should().NotBeNull().And.BeEmpty();
        result.RegionalComparison.Should().NotBeNull().And.BeEmpty();
        result.Recommendations.Should().NotBeNull().And.BeEmpty();
        result.Alerts.Should().NotBeNull().And.BeEmpty();
        result.QuotesAppendix.Should().NotBeNull().And.BeEmpty();
        result.Sources.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_NullJsonPayload_ThrowsGeminiUnavailable()
    {
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("null"));

        Func<Task> act = async () => await _sut.GenerateAsync("period", "manager", null, "feed", CancellationToken.None);

        await act.Should().ThrowAsync<GeminiUnavailableException>();
    }
}
