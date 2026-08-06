using FluentAssertions;
using Icbank.Platform.Application.InternationalDays;
using Icbank.Platform.Infrastructure.Gemini;
using Icbank.Platform.Infrastructure.InternationalDays;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.Gemini;

/// <summary>
/// Focused grounding-safeguard tests for <see cref="GeminiInternationalDaySearchProvider"/> — the
/// one adapter whose entire purpose is grounded search. Confirms: (1) the call requests grounding
/// (<see cref="GeminiCallOptions.UseGoogleSearchTool"/> + <see cref="GeminiCallOptions.RequireGrounding"/>),
/// (2) an ungrounded response never reaches this adapter as a usable result (the substituted
/// <see cref="IGeminiClient"/> is responsible for throwing <see cref="GeminiGroundingAbsentException"/>
/// per BUSINESS-RULES.md §4 — re-verified at the transport level in
/// <c>HttpGeminiTransportTests</c> and at the client level in <c>GeminiClientTests</c>), and (3)
/// citation URLs are surfaced into the mapped <see cref="DaySearchSourceDto.Url"/> field, merged
/// with the model's own self-reported <c>sources</c> JSON array.
/// </summary>
public sealed class GeminiInternationalDaySearchProviderTests
{
    private readonly IGeminiClient _client = Substitute.For<IGeminiClient>();
    private readonly GeminiInternationalDaySearchProvider _sut;

    public GeminiInternationalDaySearchProviderTests()
    {
        _sut = new GeminiInternationalDaySearchProvider(_client, new GeminiOptions());
    }

    [Fact]
    public async Task SearchAsync_RequestsGrounding_ViaGoogleSearchToolAndRequireGroundingFlags()
    {
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Grounded(MinimalDaySearchJson()));

        await _sut.SearchAsync("اليوم العالمي للعب النظيف", 2026, CancellationToken.None);

        await _client.Received(1).GenerateJsonAsync(
            Arg.Any<string>(),
            Arg.Is<GeminiCallOptions>(o => o.UseGoogleSearchTool && o.RequireGrounding && o.MaxOutputTokens == 8192),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_BuildsVerbatimPrompt_WithDayNameAndYearWindow()
    {
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Grounded(MinimalDaySearchJson()));

        await _sut.SearchAsync("يوم البيئة", 2026, CancellationToken.None);

        await _client.Received(1).GenerateJsonAsync(
            Arg.Is<string>(p =>
                p.Contains("ابحث عن \"يوم البيئة\" واستخرج بدقة المعلومات التالية", StringComparison.Ordinal) &&
                p.Contains("\"day_name_ar\": \"اسم اليوم بالعربية\"", StringComparison.Ordinal) &&
                p.Contains("اجمع تفعيلات من الأعوام 2024 و2025 و2026 فقط", StringComparison.Ordinal)),
            Arg.Any<GeminiCallOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchAsync_WhenClientThrowsGroundingAbsent_PropagatesUncaught()
    {
        // Verifies the adapter does NOT swallow or downgrade the grounding safeguard: it lets
        // GeminiGroundingAbsentException fly straight up (to SearchInternationalDayCommandHandler,
        // uncaught, to GlobalExceptionMiddleware) rather than falling back to an unverified answer.
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<GeminiGenerationResult>(new GeminiGroundingAbsentException()));

        Func<Task> act = async () => await _sut.SearchAsync("day", 2026, CancellationToken.None);

        await act.Should().ThrowAsync<GeminiGroundingAbsentException>();
    }

    [Fact]
    public async Task SearchAsync_MergesModelReportedSourcesWithTransportCitations_BothSurfacedInResult()
    {
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Grounded(
                MinimalDaySearchJson(),
                citationUrl: "https://vertexaisearch.cloud.google.com/grounding-api-redirect/tok1",
                citationTitle: "alriyadh.com"));

        DaySearchResultDto result = await _sut.SearchAsync("day", 2026, CancellationToken.None);

        result.Sources.Should().NotBeNull();
        result.Sources!.Should().Contain(s => s.Url == "https://un.org/fair-play" && s.Publisher == "UN");
        result.Sources!.Should().Contain(s =>
            s.Url == "https://vertexaisearch.cloud.google.com/grounding-api-redirect/tok1" && s.Title == "alriyadh.com");
    }

    [Fact]
    public async Task SearchAsync_TransportCitationUrl_IsPassedThroughAsIs_EvenThoughItIsAGoogleRedirect()
    {
        // Documents the real, verified behaviour: this adapter does not attempt to resolve or
        // rewrite the redirect -- it persists exactly what groundingChunks[].web.uri carried. The
        // redirect's lifetime relative to the 7-day cache window is unverified; see GEMINI-ADAPTER-NOTES.md.
        const string redirectUrl = "https://vertexaisearch.cloud.google.com/grounding-api-redirect/opaque-token-xyz";
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Grounded(MinimalDaySearchJson(), citationUrl: redirectUrl, citationTitle: "example.gov.sa"));

        DaySearchResultDto result = await _sut.SearchAsync("day", 2026, CancellationToken.None);

        result.Sources.Should().Contain(s => s.Url == redirectUrl);
    }

    [Fact]
    public async Task SearchAsync_NullJsonPayload_ThrowsGeminiUnavailable()
    {
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Grounded("null"));

        Func<Task> act = async () => await _sut.SearchAsync("day", 2026, CancellationToken.None);

        await act.Should().ThrowAsync<GeminiUnavailableException>();
    }

    [Fact]
    public async Task SearchAsync_MapsActivationsAndDesignSamplesAndSuggestions()
    {
        const string json = """
            {
              "day_name_ar": "اليوم",
              "activations": [ { "entity_name": "وزارة كذا", "entity_type": "حكومي", "activation_type": "حملة", "platform": "تويتر", "description": "وصف", "source_url": "https://x.com/1", "country": "السعودية", "year": 2025 } ],
              "design_samples": [ { "entity_name": "هيئة كذا", "entity_type": "حكومي", "platform": "إنستغرام", "description": "تصميم", "page_url": "https://ig.com/1", "image_url": "https://ig.com/1.png", "country": "السعودية", "year": 2025 } ],
              "suggestions": [ "فكرة أولى", "فكرة ثانية" ]
            }
            """;
        _client
            .GenerateJsonAsync(Arg.Any<string>(), Arg.Any<GeminiCallOptions>(), Arg.Any<CancellationToken>())
            .Returns(GeminiTestResults.Grounded(json));

        DaySearchResultDto result = await _sut.SearchAsync("day", 2026, CancellationToken.None);

        result.Activations.Should().ContainSingle().Which.EntityName.Should().Be("وزارة كذا");
        result.DesignSamples.Should().ContainSingle().Which.ImageUrl.Should().Be("https://ig.com/1.png");
        result.Suggestions.Should().Equal("فكرة أولى", "فكرة ثانية");
    }

    private static string MinimalDaySearchJson() => """
        {
          "day_name_ar": "اليوم العالمي للعب النظيف",
          "day_name_en": "World Fair Play Day",
          "annual_date": "٢٤ أغسطس",
          "sources": [ { "url": "https://un.org/fair-play", "title": "الأمم المتحدة", "publisher": "UN" } ]
        }
        """;
}
