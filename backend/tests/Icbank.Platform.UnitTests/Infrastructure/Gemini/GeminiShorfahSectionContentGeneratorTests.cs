using FluentAssertions;
using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Infrastructure.Gemini;
using Icbank.Platform.Infrastructure.Shorfah;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.Gemini;

/// <summary>
/// Tests for <see cref="GeminiShorfahSectionContentGenerator"/>: the prompt is fully assembled
/// upstream (Application layer), so this adapter's own contract is the JSON call shape (2000
/// max-output-token cap, matching <c>shorfah.ts:471-513</c>) and the <c>content_md</c> field
/// mapping.
/// </summary>
public sealed class GeminiShorfahSectionContentGeneratorTests
{
    private readonly IGeminiClient _client = Substitute.For<IGeminiClient>();
    private readonly GeminiShorfahSectionContentGenerator _sut;

    public GeminiShorfahSectionContentGeneratorTests()
    {
        _sut = new GeminiShorfahSectionContentGenerator(_client, new GeminiOptions());
    }

    [Fact]
    public async Task GenerateAsync_PassesCallerPromptUnmodified_AtMaxTokens2000()
    {
        const string callerPrompt = "برومبت شرفة الجاهز مسبقاً";
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("{\"content_md\":\"# عنوان\"}"));

        await _sut.GenerateAsync(callerPrompt, CancellationToken.None);

        await _client.Received(1).GenerateJsonAsync(
            callerPrompt,
            Arg.Is<GeminiCallOptions>(o => o.MaxOutputTokens == 2000),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_MapsContentMdField()
    {
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("{\"content_md\":\"# قسم شرفة\\nنص المحتوى.\"}"));

        ShorfahGeneratedSectionContent result = await _sut.GenerateAsync("prompt", CancellationToken.None);

        result.ContentMd.Should().Be("# قسم شرفة\nنص المحتوى.");
    }

    [Fact]
    public async Task GenerateAsync_MissingContentMdField_FallsBackToRawResponseText()
    {
        const string raw = "{\"unexpected_field\":\"value\"}";
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text(raw));

        ShorfahGeneratedSectionContent result = await _sut.GenerateAsync("prompt", CancellationToken.None);

        result.ContentMd.Should().Be(raw);
    }
}
