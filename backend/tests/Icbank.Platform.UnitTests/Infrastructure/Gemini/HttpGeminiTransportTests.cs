using FluentAssertions;
using Icbank.Platform.Infrastructure.Gemini;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.Gemini;

/// <summary>
/// Tests <see cref="HttpGeminiTransport"/>'s JSON parsing against literal response bodies shaped
/// exactly like the real Gemini <c>v1beta generateContent</c> REST API — confirmed empirically
/// against a live key: <c>groundingMetadata.webSearchQueries</c>,
/// <c>groundingMetadata.groundingChunks[].web.{uri,title}</c>,
/// <c>groundingMetadata.groundingSupports[].{segment,groundingChunkIndices}</c>, and
/// <c>groundingMetadata.searchEntryPoint.renderedContent</c>. This is deliberately NOT the
/// <c>url_citation</c>/<c>start_index</c>/<c>end_index</c> annotation shape used by some other
/// Gemini API surfaces — getting this shape wrong would make every grounded call silently look
/// ungrounded. No network call is made: <see cref="StubHttpMessageHandler"/> is an in-memory
/// <see cref="HttpMessageHandler"/> double.
/// </summary>
public sealed class HttpGeminiTransportTests
{
    private const string ApiKey = "test-key";

    private static readonly GeminiGenerationRequest PlainRequest = new(
        "gemini-2.5-flash", SystemInstruction: null, "prompt", 2048, 0.7, UseGoogleSearchTool: false, ResponseMimeType: null);

    [Fact]
    public async Task GenerateContentAsync_NoGroundingMetadata_ReturnsEmptyQueriesCitationsAndNullSearchEntryPoint()
    {
        const string body = """
            {
              "candidates": [
                { "content": { "parts": [ { "text": "نص عادي بدون تأريض" } ] } }
              ]
            }
            """;
        HttpGeminiTransport transport = BuildTransport(body);

        GeminiGenerationResult result = await transport.GenerateContentAsync(ApiKey, PlainRequest, CancellationToken.None);

        result.Text.Should().Be("نص عادي بدون تأريض");
        result.SearchQueries.Should().BeEmpty();
        result.Citations.Should().BeEmpty();
        result.SearchEntryPointHtml.Should().BeNull();
    }

    [Fact]
    public async Task GenerateContentAsync_RealGroundedShape_ExtractsWebSearchQueries()
    {
        HttpGeminiTransport transport = BuildTransport(GroundedResponseBody());

        GeminiGenerationResult result = await transport.GenerateContentAsync(ApiKey, PlainRequest, CancellationToken.None);

        result.SearchQueries.Should().BeEquivalentTo("اليوم العالمي للعب النظيف", "World Fair Play Day activations Saudi");
    }

    [Fact]
    public async Task GenerateContentAsync_RealGroundedShape_BuildsCitationsFromGroundingChunksAndSupports_NotUrlCitationAnnotations()
    {
        HttpGeminiTransport transport = BuildTransport(GroundedResponseBody());

        GeminiGenerationResult result = await transport.GenerateContentAsync(ApiKey, PlainRequest, CancellationToken.None);

        result.Citations.Should().HaveCount(2);
        result.Citations.Should().Contain(c =>
            c.Url == "https://vertexaisearch.cloud.google.com/grounding-api-redirect/AbC123" &&
            c.Title == "alriyadh.com" &&
            c.StartIndex == 0 &&
            c.EndIndex == 12);
        result.Citations.Should().Contain(c =>
            c.Url == "https://vertexaisearch.cloud.google.com/grounding-api-redirect/XyZ789" &&
            c.Title == "un.org" &&
            c.StartIndex == 13 &&
            c.EndIndex == 30);
    }

    [Fact]
    public async Task GenerateContentAsync_CitationUrl_IsTheGoogleRedirectNotTheResolvedPublisherUrl()
    {
        // Documents the real, verified behaviour: web.uri is always a
        // vertexaisearch.cloud.google.com redirect. The bare publisher domain is only ever
        // available via web.title. Both must be persisted downstream since the redirect's
        // lifetime relative to this platform's 7-day cache is unverified (see GEMINI-ADAPTER-NOTES.md).
        HttpGeminiTransport transport = BuildTransport(GroundedResponseBody());

        GeminiGenerationResult result = await transport.GenerateContentAsync(ApiKey, PlainRequest, CancellationToken.None);

        result.Citations.Should().OnlyContain(c => c.Url.StartsWith("https://vertexaisearch.cloud.google.com/grounding-api-redirect/", StringComparison.Ordinal));
        result.Citations.Should().OnlyContain(c => c.Title != null && !c.Title.StartsWith("http", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GenerateContentAsync_RealGroundedShape_ExtractsSearchEntryPointRenderedContentVerbatim()
    {
        HttpGeminiTransport transport = BuildTransport(GroundedResponseBody());

        GeminiGenerationResult result = await transport.GenerateContentAsync(ApiKey, PlainRequest, CancellationToken.None);

        result.SearchEntryPointHtml.Should().Be("<div class=\"google-search-suggestions\">...</div>");
    }

    [Fact]
    public async Task GenerateContentAsync_GroundingChunkIndexOutOfRange_IsSkippedRatherThanThrowing()
    {
        const string body = """
            {
              "candidates": [
                {
                  "content": { "parts": [ { "text": "نص" } ] },
                  "groundingMetadata": {
                    "webSearchQueries": ["q"],
                    "groundingChunks": [ { "web": { "uri": "https://vertexaisearch.cloud.google.com/grounding-api-redirect/only", "title": "only.com" } } ],
                    "groundingSupports": [
                      { "segment": { "startIndex": 0, "endIndex": 2 }, "groundingChunkIndices": [0, 5] }
                    ]
                  }
                }
              ]
            }
            """;
        HttpGeminiTransport transport = BuildTransport(body);

        GeminiGenerationResult result = await transport.GenerateContentAsync(ApiKey, PlainRequest, CancellationToken.None);

        result.Citations.Should().HaveCount(1);
        result.Citations.Single().Title.Should().Be("only.com");
    }

    [Fact]
    public async Task GenerateContentAsync_GroundingChunksWithoutSupports_ProducesQueriesButNoCitations()
    {
        // Real responses can carry webSearchQueries with groundingChunks but (rarely) no
        // groundingSupports; the grounding-absent safeguard only requires ONE of
        // SearchQueries/Citations to be non-empty, so this is still treated as grounded.
        const string body = """
            {
              "candidates": [
                {
                  "content": { "parts": [ { "text": "نص" } ] },
                  "groundingMetadata": { "webSearchQueries": ["q"] }
                }
              ]
            }
            """;
        HttpGeminiTransport transport = BuildTransport(body);

        GeminiGenerationResult result = await transport.GenerateContentAsync(ApiKey, PlainRequest, CancellationToken.None);

        result.SearchQueries.Should().ContainSingle().Which.Should().Be("q");
        result.Citations.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateContentAsync_SendsGoogleSearchTool_WhenRequested()
    {
        var handler = new StubHttpMessageHandler(SimpleTextBody("ok"));
        HttpGeminiTransport transport = BuildTransport(handler);
        GeminiGenerationRequest groundedRequest = PlainRequest with { UseGoogleSearchTool = true };

        await transport.GenerateContentAsync(ApiKey, groundedRequest, CancellationToken.None);

        handler.LastRequestBody.Should().Contain("google_search");
    }

    [Fact]
    public async Task GenerateContentAsync_SendsApiKeyHeader_NeverInBody()
    {
        var handler = new StubHttpMessageHandler(SimpleTextBody("ok"));
        HttpGeminiTransport transport = BuildTransport(handler);

        await transport.GenerateContentAsync("secret-key-value", PlainRequest, CancellationToken.None);

        handler.LastRequest!.Headers.GetValues("x-goog-api-key").Should().ContainSingle().Which.Should().Be("secret-key-value");
        handler.LastRequestBody.Should().NotContain("secret-key-value");
    }

    [Fact]
    public async Task GenerateContentAsync_NonSuccessStatusCode_ThrowsWithBodyAndStatusInMessage()
    {
        var handler = new StubHttpMessageHandler("{\"error\":{\"message\":\"The model is overloaded\"}}", System.Net.HttpStatusCode.ServiceUnavailable);
        HttpGeminiTransport transport = BuildTransport(handler);

        Func<Task> act = async () => await transport.GenerateContentAsync(ApiKey, PlainRequest, CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().Contain("503").And.Contain("overloaded");
    }

    private static string SimpleTextBody(string text) =>
        $$"""{ "candidates": [ { "content": { "parts": [ { "text": "{{text}}" } ] } } ] }""";

    private static string GroundedResponseBody() => """
        {
          "candidates": [
            {
              "content": { "parts": [ { "text": "وفق ما ذكرته الرياض واليونسكو، تم الإعلان." } ] },
              "groundingMetadata": {
                "webSearchQueries": ["اليوم العالمي للعب النظيف", "World Fair Play Day activations Saudi"],
                "groundingChunks": [
                  { "web": { "uri": "https://vertexaisearch.cloud.google.com/grounding-api-redirect/AbC123", "title": "alriyadh.com" } },
                  { "web": { "uri": "https://vertexaisearch.cloud.google.com/grounding-api-redirect/XyZ789", "title": "un.org" } }
                ],
                "groundingSupports": [
                  { "segment": { "startIndex": 0, "endIndex": 12 }, "groundingChunkIndices": [0] },
                  { "segment": { "startIndex": 13, "endIndex": 30 }, "groundingChunkIndices": [1] }
                ],
                "searchEntryPoint": { "renderedContent": "<div class=\"google-search-suggestions\">...</div>" }
              }
            }
          ]
        }
        """;

    private static HttpGeminiTransport BuildTransport(string responseBody) => BuildTransport(new StubHttpMessageHandler(responseBody));

    private static HttpGeminiTransport BuildTransport(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://generativelanguage.googleapis.com") };
        return new HttpGeminiTransport(httpClient, new GeminiOptions());
    }
}
