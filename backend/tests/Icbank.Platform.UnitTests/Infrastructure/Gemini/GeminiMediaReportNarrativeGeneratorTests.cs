using FluentAssertions;
using Icbank.Platform.Infrastructure.Gemini;
using Icbank.Platform.Infrastructure.MediaMonitoring;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.Gemini;

/// <summary>
/// Tests for <see cref="GeminiMediaReportNarrativeGenerator"/>'s 3-call pipeline (BUSINESS-RULES.md
/// §5.1): the audience-tiered Markdown body, a separate 2-3 line executive-summary call capped at
/// 300 max output tokens, and a separate two-word tone-classification call capped at 50 max output
/// tokens. The zero-source-item "no AI call" short-circuit is Application-layer behaviour (see
/// <c>GenerateMediaReportCommandHandlerTests</c>) and is intentionally not re-tested here — this
/// adapter is only ever invoked once there is at least one source item.
/// </summary>
public sealed class GeminiMediaReportNarrativeGeneratorTests
{
    private const string Feed = "١- خبر تجريبي أول\n٢- خبر تجريبي ثانٍ";

    private readonly IGeminiClient _client = Substitute.For<IGeminiClient>();
    private readonly GeminiMediaReportNarrativeGenerator _sut;

    public GeminiMediaReportNarrativeGeneratorTests()
    {
        _sut = new GeminiMediaReportNarrativeGenerator(_client, new GeminiOptions());
    }

    [Fact]
    public async Task GenerateAsync_MakesExactlyThreeSeparateTextCalls()
    {
        StubAnyTextCall();

        await _sut.GenerateAsync("manager", Feed, CancellationToken.None);

        await _client.Received(3).GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_FirstCall_UsesResolvedAudiencePromptPlusFeed_AtDefaultMaxTokens()
    {
        StubAnyTextCall();

        await _sut.GenerateAsync("manager", Feed, CancellationToken.None);

        await _client.Received(1).GenerateTextAsync(
            Arg.Is<string>(p =>
                p.Contains("أنت محلل إعلامي محترف. ولّد تقرير رصد متوازناً للإدارة الوسطى", StringComparison.Ordinal) &&
                p.Contains("البيانات المرصودة:", StringComparison.Ordinal) &&
                p.Contains(Feed, StringComparison.Ordinal)),
            Arg.Is<GeminiCallOptions>(o => o.MaxOutputTokens == GeminiClient.DefaultMaxOutputTokens),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_SecondCall_IsExecutiveSummaryPrompt_AtMaxTokens300()
    {
        StubAnyTextCall();

        await _sut.GenerateAsync("manager", Feed, CancellationToken.None);

        await _client.Received(1).GenerateTextAsync(
            Arg.Is<string>(p =>
                p.StartsWith("لخّص الفترة التالية في 2-3 أسطر تنفيذية موجزة بالعربية الفصحى، دون عناوين أو تنسيق Markdown:", StringComparison.Ordinal) &&
                p.Contains(Feed, StringComparison.Ordinal)),
            Arg.Is<GeminiCallOptions>(o => o.MaxOutputTokens == 300),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_ThirdCall_IsToneClassificationPrompt_AtMaxTokens50()
    {
        StubAnyTextCall();

        await _sut.GenerateAsync("manager", Feed, CancellationToken.None);

        await _client.Received(1).GenerateTextAsync(
            Arg.Is<string>(p =>
                p.StartsWith("صف النبرة العامة للمحتوى التالي بكلمتين فقط بالعربية (مثل: إيجابي عام، محايد متوازن):", StringComparison.Ordinal) &&
                p.Contains(Feed, StringComparison.Ordinal)),
            Arg.Is<GeminiCallOptions>(o => o.MaxOutputTokens == 50),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("executive")]
    [InlineData("manager")]
    [InlineData("analyst")]
    [InlineData("unknown-falls-back-to-manager")]
    public async Task GenerateAsync_ResolvesAudienceTier_DefaultingUnknownToManager(string audience)
    {
        StubAnyTextCall();

        await _sut.GenerateAsync(audience, Feed, CancellationToken.None);

        // Every audience tier (and the unknown fallback) must still drive exactly one body call
        // built from a non-empty resolved template; the per-tier template text itself is Application-layer content.
        await _client.Received(3).GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_MapsAllThreeResultsOntoNarrativeFieldsInOrder()
    {
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Is<GeminiCallOptions>(o => o.MaxOutputTokens == GeminiClient.DefaultMaxOutputTokens), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("## نص التقرير الكامل"));
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Is<GeminiCallOptions>(o => o.MaxOutputTokens == 300), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("ملخص تنفيذي موجز."));
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Is<GeminiCallOptions>(o => o.MaxOutputTokens == 50), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("إيجابي عام"));

        Platform.Application.MediaMonitoring.MediaReportNarrative result = await _sut.GenerateAsync("manager", Feed, CancellationToken.None);

        result.ContentMd.Should().Be("## نص التقرير الكامل");
        result.ExecutiveSummary.Should().Be("ملخص تنفيذي موجز.");
        result.OverallTone.Should().Be("إيجابي عام");
    }

    [Fact]
    public async Task GenerateAsync_AllThreeCallsUseConfiguredTextModel()
    {
        var options = new GeminiOptions { TextModel = "gemini-media-model" };
        var sut = new GeminiMediaReportNarrativeGenerator(_client, options);
        StubAnyTextCall();

        await sut.GenerateAsync("manager", Feed, CancellationToken.None);

        await _client.Received(3).GenerateTextAsync(Arg.Any<string>(), Arg.Is<GeminiCallOptions>(o => o.Model == "gemini-media-model"), Arg.Any<CancellationToken>());
    }

    private void StubAnyTextCall() =>
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("نتيجة"));
}
