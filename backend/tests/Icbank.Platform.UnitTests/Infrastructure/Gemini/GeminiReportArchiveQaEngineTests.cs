using FluentAssertions;
using Icbank.Platform.Infrastructure.Gemini;
using Icbank.Platform.Infrastructure.MediaMonitoring;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.Gemini;

/// <summary>
/// Tests for <see cref="GeminiReportArchiveQaEngine"/>: verbatim BUSINESS-RULES.md §5.5 dual-mode
/// Q&amp;A prompt (<c>final-media-reports.ts:704-711</c>), info-mode only — full mode never reaches
/// this adapter per its own documentation.
/// </summary>
public sealed class GeminiReportArchiveQaEngineTests
{
    private readonly IGeminiClient _client = Substitute.For<IGeminiClient>();
    private readonly GeminiReportArchiveQaEngine _sut;

    public GeminiReportArchiveQaEngineTests()
    {
        _sut = new GeminiReportArchiveQaEngine(_client, new GeminiOptions());
    }

    [Fact]
    public async Task AnswerAsync_BuildsVerbatimPrompt_WithQueryAndContextInterpolated()
    {
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("إجابة"));

        await _sut.AnswerAsync("ما أبرز الأخبار هذا الشهر؟", "سياق من التقارير المحفوظة", CancellationToken.None);

        await _client.Received(1).GenerateTextAsync(
            Arg.Is<string>(p =>
                p.Contains("أنت مساعد بحث ذكي في أرشيف تقارير الرصد الإعلامي للهيئة العامة للمنافسة.", StringComparison.Ordinal) &&
                p.Contains("السؤال: ما أبرز الأخبار هذا الشهر؟", StringComparison.Ordinal) &&
                p.Contains("السياق من التقارير المحفوظة:\nسياق من التقارير المحفوظة", StringComparison.Ordinal) &&
                p.Contains("أضف في النهاية قائمة \"المصادر:\"", StringComparison.Ordinal)),
            Arg.Any<GeminiCallOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AnswerAsync_ReturnsResultTextVerbatim()
    {
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("الإجابة الكاملة مع المصادر"));

        var result = await _sut.AnswerAsync("سؤال", "سياق", CancellationToken.None);

        result.Should().Be("الإجابة الكاملة مع المصادر");
    }

    [Fact]
    public async Task AnswerAsync_UsesConfiguredTextModel()
    {
        var options = new GeminiOptions { TextModel = "gemini-qa-model" };
        var sut = new GeminiReportArchiveQaEngine(_client, options);
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("x"));

        await sut.AnswerAsync("q", "c", CancellationToken.None);

        await _client.Received(1).GenerateTextAsync(
            Arg.Any<string>(),
            Arg.Is<GeminiCallOptions>(o => o.Model == "gemini-qa-model"),
            Arg.Any<CancellationToken>());
    }
}
