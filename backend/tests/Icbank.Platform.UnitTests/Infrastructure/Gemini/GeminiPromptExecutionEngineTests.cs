using FluentAssertions;
using Icbank.Platform.Infrastructure.Gemini;
using Icbank.Platform.Infrastructure.MediaMonitoring;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.Gemini;

/// <summary>
/// Tests for <see cref="GeminiPromptExecutionEngine"/> confirming it is a pure passthrough: the
/// caller-built prompt text reaches <see cref="IGeminiClient.GenerateTextAsync"/> unmodified, with
/// no further prompt construction on the adapter's part.
/// </summary>
public sealed class GeminiPromptExecutionEngineTests
{
    private readonly IGeminiClient _client = Substitute.For<IGeminiClient>();
    private readonly GeminiPromptExecutionEngine _sut;

    public GeminiPromptExecutionEngineTests()
    {
        _sut = new GeminiPromptExecutionEngine(_client, new GeminiOptions());
    }

    [Fact]
    public async Task ExecuteAsync_PassesCallerPromptTextUnmodified()
    {
        const string callerPrompt = "لخّص هذا النص التجريبي كما هو دون أي إضافة.";
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("نتيجة"));

        await _sut.ExecuteAsync(callerPrompt, CancellationToken.None);

        await _client.Received(1).GenerateTextAsync(callerPrompt, Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_UsesConfiguredTextModel()
    {
        var options = new GeminiOptions { TextModel = "gemini-quick-tool-model" };
        var sut = new GeminiPromptExecutionEngine(_client, options);
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("x"));

        await sut.ExecuteAsync("prompt", CancellationToken.None);

        await _client.Received(1).GenerateTextAsync(
            Arg.Any<string>(),
            Arg.Is<GeminiCallOptions>(o => o.Model == "gemini-quick-tool-model"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsResultTextVerbatim()
    {
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("النص الناتج بالكامل"));

        var result = await _sut.ExecuteAsync("أي مدخل", CancellationToken.None);

        result.Should().Be("النص الناتج بالكامل");
    }
}
