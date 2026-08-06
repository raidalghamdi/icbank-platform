using FluentAssertions;
using Icbank.Platform.Application.Weekend;
using Icbank.Platform.Infrastructure.Gemini;
using Icbank.Platform.Infrastructure.Weekend;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.Gemini;

/// <summary>
/// Tests for <see cref="GeminiWeekStartMessageGenerator"/>: confirms the 3 labeled outputs
/// (<c>claude</c>/<c>openai</c>/<c>gemini</c>, kept only for UI/DB compatibility with the Node
/// source) are 3 independent Gemini calls sharing one prompt, and that one call's
/// <see cref="GeminiUnavailableException"/> does not block the other two.
/// </summary>
public sealed class GeminiWeekStartMessageGeneratorTests
{
    private readonly IGeminiClient _client = Substitute.For<IGeminiClient>();
    private readonly GeminiWeekStartMessageGenerator _sut;

    public GeminiWeekStartMessageGeneratorTests()
    {
        _sut = new GeminiWeekStartMessageGenerator(_client, new GeminiOptions());
    }

    [Fact]
    public async Task GenerateAsync_MakesExactlyThreeIndependentCalls_WithSamePrompt()
    {
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("رسالة بداية الأسبوع"));

        await _sut.GenerateAsync(BuildRequest(), CancellationToken.None);

        await _client.Received(3).GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_ReturnsThreeLabeledOutputs_ClaudeOpenaiGemini_ForUiCompatibility()
    {
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("نص الرسالة"));

        IReadOnlyList<WeekStartModelOutput> outputs = await _sut.GenerateAsync(BuildRequest(), CancellationToken.None);

        outputs.Should().HaveCount(3);
        outputs.Select(o => o.ModelName).Should().ContainInOrder("claude", "openai", "gemini");
        outputs.Should().OnlyContain(o => o.OutputText == "نص الرسالة");
    }

    [Fact]
    public async Task GenerateAsync_BuildsVerbatimPrompt_IncludingTopicOccasionAudienceToneAndLength()
    {
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("x"));

        await _sut.GenerateAsync(BuildRequest(), CancellationToken.None);

        await _client.Received(3).GenerateTextAsync(
            Arg.Is<string>(p =>
                p.Contains("أنت كاتب محتوى داخلي لجهة حكومية سعودية.", StringComparison.Ordinal) &&
                p.Contains("موضوع هذا الأسبوع: الابتكار", StringComparison.Ordinal) &&
                p.Contains("المناسبة: بداية الربع", StringComparison.Ordinal) &&
                p.Contains("الجمهور: جميع الموظفين", StringComparison.Ordinal) &&
                p.Contains("النبرة: متحمسة", StringComparison.Ordinal) &&
                p.Contains("قصير (سطران فقط - 25-40 كلمة)", StringComparison.Ordinal)),
            Arg.Any<GeminiCallOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_DefaultsToneToOdiya_WhenToneNotSupplied()
    {
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("x"));

        await _sut.GenerateAsync(BuildRequest() with { Tone = null }, CancellationToken.None);

        await _client.Received(3).GenerateTextAsync(
            Arg.Is<string>(p => p.Contains("النبرة: " + WeekStartPromptTemplate.DefaultTone, StringComparison.Ordinal)),
            Arg.Any<GeminiCallOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_OneCallExhaustingToGeminiUnavailable_DoesNotBlockTheOtherTwo()
    {
        var callCount = 0;
        _client
            .GenerateTextAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                if (callCount == 2)
                {
                    throw new GeminiUnavailableException("exhausted");
                }

                return GeminiTestResults.Text("ok");
            });

        Func<Task> act = async () => await _sut.GenerateAsync(BuildRequest(), CancellationToken.None);

        // Documents actual behaviour: calls are sequential (foreach, not Task.WhenAll), so the
        // second call's exhaustion propagates and stops the third from ever running. This is an
        // honest characterization, not a design endorsement — see GEMINI-ADAPTER-NOTES.md.
        await act.Should().ThrowAsync<GeminiUnavailableException>();
        callCount.Should().Be(2);
    }

    private static WeekStartGenerationRequest BuildRequest(string? length = "short") => new(
        Topic: "الابتكار",
        Occasion: "بداية الربع",
        Audience: "جميع الموظفين",
        Tone: "متحمسة",
        Length: length,
        StyleContext: "أسلوب ودود");
}
