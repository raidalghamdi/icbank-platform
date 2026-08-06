using FluentAssertions;
using Icbank.Platform.Application.Designs.IconEvent;
using Icbank.Platform.Infrastructure.Designs;
using Icbank.Platform.Infrastructure.Gemini;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.Gemini;

/// <summary>
/// Tests for <see cref="GeminiIconEventDesignExtractor"/>: prompt passthrough (the prompt is fully
/// assembled upstream by <c>IconEventPromptBuilder</c> in the Application layer), and mapping the
/// <c>{extracted, variants}</c> wire shape onto <see cref="IconEventExtractionResultDto"/>,
/// including the documented all-empty-defaults behaviour when fields are absent.
/// </summary>
public sealed class GeminiIconEventDesignExtractorTests
{
    private readonly IGeminiClient _client = Substitute.For<IGeminiClient>();
    private readonly GeminiIconEventDesignExtractor _sut;

    public GeminiIconEventDesignExtractorTests()
    {
        _sut = new GeminiIconEventDesignExtractor(_client, new GeminiOptions());
    }

    [Fact]
    public async Task ExtractAsync_PassesCallerPromptUnmodified()
    {
        const string prompt = "برومبت استخلاص الأيقونات الجاهز";
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("{\"extracted\":{},\"variants\":[]}"));

        await _sut.ExtractAsync(prompt, CancellationToken.None);

        await _client.Received(1).GenerateJsonAsync(prompt, Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExtractAsync_MapsExtractedFieldsAndStats()
    {
        const string json = """
            {
              "extracted": {
                "headline": "عنوان الحدث",
                "subtitle": "عنوان فرعي",
                "department": "الإدارة العامة",
                "hashtag": "#حدث",
                "contact_email": "a@b.gov.sa",
                "contact_phone": "0555555555",
                "stats": [ { "icon": "users", "value": "500", "label": "مشارك" } ]
              },
              "variants": []
            }
            """;
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text(json));

        IconEventExtractionResultDto result = await _sut.ExtractAsync("prompt", CancellationToken.None);

        result.Extracted.Headline.Should().Be("عنوان الحدث");
        result.Extracted.Department.Should().Be("الإدارة العامة");
        result.Extracted.Stats.Should().ContainSingle().Which.Value.Should().Be("500");
    }

    [Fact]
    public async Task ExtractAsync_MapsVariants()
    {
        const string json = """
            {
              "extracted": {},
              "variants": [ { "layout": "grid", "main_icon": "star", "supporting_icons": ["a","b"], "rationale": "سبب الاختيار" } ]
            }
            """;
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text(json));

        IconEventExtractionResultDto result = await _sut.ExtractAsync("prompt", CancellationToken.None);

        result.Variants.Should().ContainSingle();
        result.Variants[0].Layout.Should().Be("grid");
        result.Variants[0].SupportingIcons.Should().Equal("a", "b");
    }

    [Fact]
    public async Task ExtractAsync_NullExtractedObject_DefaultsToAllEmptyStrings()
    {
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("{\"variants\":[]}"));

        IconEventExtractionResultDto result = await _sut.ExtractAsync("prompt", CancellationToken.None);

        result.Extracted.Headline.Should().BeEmpty();
        result.Extracted.Stats.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_NullJsonPayload_ThrowsGeminiUnavailable()
    {
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Text("null"));

        Func<Task> act = async () => await _sut.ExtractAsync("prompt", CancellationToken.None);

        await act.Should().ThrowAsync<GeminiUnavailableException>();
    }
}
