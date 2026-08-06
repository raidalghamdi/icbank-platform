using FluentAssertions;
using Icbank.Platform.Application.Designs.Composer;
using Icbank.Platform.Infrastructure.Designs;
using Icbank.Platform.Infrastructure.Gemini;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.Gemini;

/// <summary>
/// Tests for <see cref="GeminiBackgroundImageGenerator"/> — the only adapter whose output is
/// binary rather than text/JSON. Confirms it calls <see cref="IGeminiClient.GenerateImageAsync"/>
/// with the configured image model, decodes the first inline image, and passes the prompt through
/// unmodified (prompt assembly lives upstream in <c>BackgroundPromptBuilder</c>).
/// </summary>
public sealed class GeminiBackgroundImageGeneratorTests
{
    private readonly IGeminiClient _client = Substitute.For<IGeminiClient>();

    [Fact]
    public async Task GenerateAsync_UsesConfiguredImageModel_NotTextModel()
    {
        var options = new GeminiOptions { TextModel = "gemini-text", ImageModel = "gemini-nano-banana" };
        var sut = new GeminiBackgroundImageGenerator(_client, options);
        _client
            .GenerateImageAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Image(Convert.ToBase64String(new byte[] { 1, 2, 3 })));

        await sut.GenerateAsync("prompt", CancellationToken.None);

        await _client.Received(1).GenerateImageAsync(
            Arg.Any<string>(),
            Arg.Is<GeminiCallOptions>(o => o.Model == "gemini-nano-banana"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_PassesPromptThroughUnmodified()
    {
        var sut = new GeminiBackgroundImageGenerator(_client, new GeminiOptions());
        const string prompt = "برومبت خلفية جاهز مع تلميح مكاني";
        _client
            .GenerateImageAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Image(Convert.ToBase64String(new byte[] { 9 })));

        await sut.GenerateAsync(prompt, CancellationToken.None);

        await _client.Received(1).GenerateImageAsync(prompt, Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_DecodesFirstInlineImageBase64AndMimeType()
    {
        var sut = new GeminiBackgroundImageGenerator(_client, new GeminiOptions());
        var rawBytes = new byte[] { 137, 80, 78, 71 };
        _client
            .GenerateImageAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Image(Convert.ToBase64String(rawBytes), mimeType: "image/webp"));

        GeneratedBackgroundImage result = await sut.GenerateAsync("prompt", CancellationToken.None);

        result.Content.Should().Equal(rawBytes);
        result.ContentType.Should().Be("image/webp");
    }
}
