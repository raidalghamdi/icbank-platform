using FluentAssertions;
using Icbank.Platform.Infrastructure.Gemini;
using Icbank.Platform.Infrastructure.Weekend;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.Gemini;

/// <summary>
/// Tests for <see cref="GeminiWeekendContentGenerator"/>: verbatim BUSINESS-RULES.md §2.3 prompt
/// construction (Riyadh hardcoded, matching the Node source's product scope), the required-JSON
/// wire schema markers, and use of the "pro" model tier as <c>aiJSONWithFallback</c> did in the
/// Node original for weekend drafts.
/// </summary>
public sealed class GeminiWeekendContentGeneratorTests
{
    private readonly IGeminiClient _client = Substitute.For<IGeminiClient>();
    private readonly GeminiWeekendContentGenerator _sut;

    public GeminiWeekendContentGeneratorTests()
    {
        _sut = new GeminiWeekendContentGenerator(_client, new GeminiOptions());
    }

    [Fact]
    public async Task GenerateAsync_BuildsVerbatimPrompt_WithThursdayDateAndRiyadhHardcoded()
    {
        const string date = "12 ديسمبر 2026";
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("{\"summary\":\"s\"}"));

        await _sut.GenerateAsync(date, CancellationToken.None);

        await _client.Received(1).GenerateJsonAsync(
            Arg.Is<string>(p =>
                p.Contains("أنت محرر محتوى ترفيهي خبير للموظفين الحكوميين في المملكة العربية السعودية", StringComparison.Ordinal) &&
                p.Contains("يخص مدينة الرياض ليوم الخميس " + date, StringComparison.Ordinal) &&
                p.Contains("\"summary\": \"فقرة 3 أسطر ترحيبية", StringComparison.Ordinal) &&
                p.Contains("\"maps_query\"", StringComparison.Ordinal)),
            Arg.Any<GeminiCallOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_UsesProModel_NotTextModel()
    {
        var options = new GeminiOptions { TextModel = "gemini-text", ProModel = "gemini-pro-weekend" };
        var sut = new GeminiWeekendContentGenerator(_client, options);
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("{}"));

        await sut.GenerateAsync("date", CancellationToken.None);

        await _client.Received(1).GenerateJsonAsync(
            Arg.Any<string>(),
            Arg.Is<GeminiCallOptions>(o => o.Model == "gemini-pro-weekend"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_ReturnsRawJsonTextVerbatim_ForCallerToDeserialize()
    {
        const string json = "{\"summary\":\"ملخص\",\"places\":[]}";
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text(json));

        var result = await _sut.GenerateAsync("date", CancellationToken.None);

        result.Should().Be(json);
    }
}
