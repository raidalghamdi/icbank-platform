using FluentAssertions;
using Icbank.Platform.Infrastructure.Gemini;
using Icbank.Platform.Infrastructure.MediaMonitoring;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.Gemini;

/// <summary>
/// Tests for <see cref="GeminiExecutiveSummaryRegenerator"/>: verbatim BUSINESS-RULES.md §5.4
/// prompt interpolation (<c>final-media-reports.ts:626-637</c>) over the caller's pre-sliced JSON
/// fragments.
/// </summary>
public sealed class GeminiExecutiveSummaryRegeneratorTests
{
    private readonly IGeminiClient _client = Substitute.For<IGeminiClient>();
    private readonly GeminiExecutiveSummaryRegenerator _sut;

    public GeminiExecutiveSummaryRegeneratorTests()
    {
        _sut = new GeminiExecutiveSummaryRegenerator(_client, new GeminiOptions());
    }

    [Fact]
    public async Task RegenerateAsync_BuildsVerbatimPrompt_WithAllFiveFieldsInterpolated()
    {
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("## الملخص التنفيذي — الفترة"));

        await _sut.RegenerateAsync("عنوان التقرير", "١-٧ يناير", "{\"kpi\":1}", "[{\"title\":\"خبر\"}]", "[{\"rec\":\"توصية\"}]", CancellationToken.None);

        await _client.Received(1).GenerateTextAsync(
            Arg.Is<string>(p =>
                p.Contains("أنت محلل تنفيذي. ولّد ملخصاً تنفيذياً موجزاً (5-7 أسطر فقط) للقيادة العليا بصيغة Markdown عربية", StringComparison.Ordinal) &&
                p.Contains("العنوان: عنوان التقرير", StringComparison.Ordinal) &&
                p.Contains("الفترة: ١-٧ يناير", StringComparison.Ordinal) &&
                p.Contains("المؤشرات: {\"kpi\":1}", StringComparison.Ordinal) &&
                p.Contains("أبرز الأخبار: [{\"title\":\"خبر\"}]", StringComparison.Ordinal) &&
                p.Contains("التوصيات: [{\"rec\":\"توصية\"}]", StringComparison.Ordinal) &&
                p.Contains("## الملخص التنفيذي — ١-٧ يناير", StringComparison.Ordinal)),
            Arg.Any<GeminiCallOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegenerateAsync_UsesConfiguredTextModel()
    {
        var options = new GeminiOptions { TextModel = "gemini-regen-model" };
        var sut = new GeminiExecutiveSummaryRegenerator(_client, options);
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("x"));

        await sut.RegenerateAsync("t", "p", "{}", "[]", "[]", CancellationToken.None);

        await _client.Received(1).GenerateTextAsync(
            Arg.Any<string>(),
            Arg.Is<GeminiCallOptions>(o => o.Model == "gemini-regen-model"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegenerateAsync_ReturnsResultTextVerbatim()
    {
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("## الملخص التنفيذي — الفترة\n1. نقطة"));

        var result = await _sut.RegenerateAsync("t", "p", "{}", "[]", "[]", CancellationToken.None);

        result.Should().Be("## الملخص التنفيذي — الفترة\n1. نقطة");
    }
}
