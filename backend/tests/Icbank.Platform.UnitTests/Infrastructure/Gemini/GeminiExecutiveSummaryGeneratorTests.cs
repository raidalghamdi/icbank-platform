using FluentAssertions;
using Icbank.Platform.Infrastructure.Dashboard;
using Icbank.Platform.Infrastructure.Gemini;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.Gemini;

/// <summary>
/// Prompt-construction and response-mapping tests for <see cref="GeminiExecutiveSummaryGenerator"/>,
/// verified against a substituted <see cref="IGeminiClient"/> (no real transport, no network).
/// </summary>
public sealed class GeminiExecutiveSummaryGeneratorTests
{
    private readonly IGeminiClient _client = Substitute.For<IGeminiClient>();
    private readonly GeminiExecutiveSummaryGenerator _sut;

    public GeminiExecutiveSummaryGeneratorTests()
    {
        _sut = new GeminiExecutiveSummaryGenerator(_client, new GeminiOptions());
    }

    [Fact]
    public async Task GenerateAsync_BuildsVerbatimPromptAroundDigest_FromDashboardTs168()
    {
        const string digest = "٥ رسائل جديدة، ٢ حملات نشطة";
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("• ملخص أول\n• ملخص ثانٍ"));

        await _sut.GenerateAsync(digest, CancellationToken.None);

        await _client.Received(1).GenerateTextAsync(
            Arg.Is<string>(p =>
                p.StartsWith("أنت مساعد تنفيذي متخصص في التواصل الداخلي المؤسسي. بناءً على البيانات التالية:", StringComparison.Ordinal) &&
                p.Contains(digest, StringComparison.Ordinal) &&
                p.Contains("اكتب ملخصاً تنفيذياً قصيراً (3-4 نقاط عربية)", StringComparison.Ordinal) &&
                p.Contains("كل نقطة في سطر منفصل تبدأ بـ •", StringComparison.Ordinal)),
            Arg.Any<GeminiCallOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_UsesConfiguredTextModel_NotProModel()
    {
        var options = new GeminiOptions { TextModel = "gemini-2.5-flash-custom" };
        var sut = new GeminiExecutiveSummaryGenerator(_client, options);
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("x"));

        await sut.GenerateAsync("digest", CancellationToken.None);

        await _client.Received(1).GenerateTextAsync(
            Arg.Any<string>(),
            Arg.Is<GeminiCallOptions>(o => o.Model == "gemini-2.5-flash-custom"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_ReturnsClientTextVerbatim()
    {
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("• نقطة واحدة\n• نقطة ثانية"));

        var result = await _sut.GenerateAsync("digest", CancellationToken.None);

        result.Should().Be("• نقطة واحدة\n• نقطة ثانية");
    }
}
