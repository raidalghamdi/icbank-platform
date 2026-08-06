using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>
/// Real <see cref="IGeminiTransport"/> backed by the named "gemini" <see cref="HttpClient"/>
/// (see <c>DependencyInjection.AddResilientHttpClients</c>). Talks to the Gemini Generative
/// Language REST API's <c>models/{model}:generateContent</c> endpoint directly (no Google SDK
/// dependency), which is what makes this fully unit-testable without any network access: this
/// class is the one seam every adapter test replaces with a fake, never a real
/// <see cref="HttpMessageHandler"/> mock.
/// </summary>
public sealed class HttpGeminiTransport : IGeminiTransport
{
    private const string ApiKeyHeaderName = "x-goog-api-key";

    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;

    /// <summary>Initializes a new instance of the <see cref="HttpGeminiTransport"/> class.</summary>
    /// <param name="httpClient">The named "gemini" <see cref="HttpClient"/>.</param>
    /// <param name="options">The Gemini configuration (base URL, model defaults).</param>
    public HttpGeminiTransport(HttpClient httpClient, GeminiOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<GeminiGenerationResult> GenerateContentAsync(string apiKey, GeminiGenerationRequest request, CancellationToken cancellationToken)
    {
        var url = string.Create(CultureInfo.InvariantCulture, $"{_options.BaseUrl}/v1beta/models/{request.Model}:generateContent");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add(ApiKeyHeaderName, apiKey);
        httpRequest.Content = JsonContent.Create(BuildBody(request));

        using HttpResponseMessage response = await _httpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                string.Create(CultureInfo.InvariantCulture, $"Gemini API returned {(int)response.StatusCode} {response.StatusCode}: {body}"));
        }

        return ParseResponse(body, request.Model);
    }

    private static Dictionary<string, object> BuildBody(GeminiGenerationRequest request)
    {
        var generationConfig = new Dictionary<string, object>
        {
            ["maxOutputTokens"] = request.MaxOutputTokens,
            ["temperature"] = request.Temperature,
        };
        if (!string.IsNullOrEmpty(request.ResponseMimeType))
        {
            generationConfig["responseMimeType"] = request.ResponseMimeType;
        }

        if (request.Model.Contains("image", StringComparison.OrdinalIgnoreCase))
        {
            generationConfig["responseModalities"] = new[] { "IMAGE", "TEXT" };
        }

        var body = new Dictionary<string, object>
        {
            ["contents"] = new[] { new { role = "user", parts = new[] { new { text = request.UserPrompt } } } },
            ["generationConfig"] = generationConfig,
        };
        if (!string.IsNullOrWhiteSpace(request.SystemInstruction))
        {
            body["systemInstruction"] = new { parts = new[] { new { text = request.SystemInstruction } } };
        }

        if (request.UseGoogleSearchTool)
        {
            body["tools"] = new object[] { new { google_search = new { } } };
        }

        return body;
    }

    private static GeminiGenerationResult ParseResponse(string body, string modelUsed)
    {
        JsonNode? root = JsonNode.Parse(body) ?? throw new JsonException("Gemini response body was not valid JSON.");
        JsonNode? firstCandidate = root["candidates"]?.AsArray().FirstOrDefault();
        var text = ExtractText(firstCandidate);
        IReadOnlyList<GeminiInlineImage> inlineImages = ExtractInlineImages(firstCandidate);
        (IReadOnlyList<string> queries, IReadOnlyList<GeminiCitation> citations, var searchEntryPointHtml) = ExtractGrounding(firstCandidate);
        return new GeminiGenerationResult(text, modelUsed, queries, citations, inlineImages, searchEntryPointHtml);
    }

    private static string ExtractText(JsonNode? candidate)
    {
        JsonArray? parts = candidate?["content"]?["parts"]?.AsArray();
        if (parts is null)
        {
            return string.Empty;
        }

        return string.Concat(parts.Select(p => p?["text"]?.GetValue<string>() ?? string.Empty));
    }

    private static IReadOnlyList<GeminiInlineImage> ExtractInlineImages(JsonNode? candidate)
    {
        JsonArray? parts = candidate?["content"]?["parts"]?.AsArray();
        if (parts is null)
        {
            return Array.Empty<GeminiInlineImage>();
        }

        var images = new List<GeminiInlineImage>();
        foreach (JsonNode? part in parts)
        {
            JsonNode? inlineData = part?["inlineData"] ?? part?["inline_data"];
            var data = inlineData?["data"]?.GetValue<string>();
            var mimeType = inlineData?["mimeType"]?.GetValue<string>() ?? inlineData?["mime_type"]?.GetValue<string>();
            if (!string.IsNullOrEmpty(data))
            {
                images.Add(new GeminiInlineImage(data, mimeType ?? "image/png"));
            }
        }

        return images;
    }

    private static (IReadOnlyList<string> Queries, IReadOnlyList<GeminiCitation> Citations, string? SearchEntryPointHtml) ExtractGrounding(JsonNode? candidate)
    {
        JsonNode? metadata = candidate?["groundingMetadata"];
        if (metadata is null)
        {
            return (Array.Empty<string>(), Array.Empty<GeminiCitation>(), null);
        }

        List<string> queries = metadata["webSearchQueries"]?.AsArray().Select(q => q?.GetValue<string>() ?? string.Empty).ToList()
            ?? new List<string>();
        IReadOnlyList<GeminiCitation> citations = ExtractCitations(metadata);
        string? searchEntryPointHtml = ExtractSearchEntryPointHtml(metadata);

        return (queries, citations, searchEntryPointHtml);
    }

    private static IReadOnlyList<GeminiCitation> ExtractCitations(JsonNode metadata)
    {
        JsonArray? chunks = metadata["groundingChunks"]?.AsArray();
        JsonArray? supports = metadata["groundingSupports"]?.AsArray();
        if (chunks is null || supports is null)
        {
            return Array.Empty<GeminiCitation>();
        }

        var citations = new List<GeminiCitation>();
        foreach (JsonNode? support in supports)
        {
            citations.AddRange(BuildCitationsForSupport(support, chunks));
        }

        return citations;
    }

    private static string? ExtractSearchEntryPointHtml(JsonNode metadata)
    {
        // Why: groundingMetadata.searchEntryPoint.renderedContent is Google's "Search Suggestions"
        // HTML. Whether displaying it is mandatory under Google's ToS for grounded results is an
        // open question this port cannot resolve -- it is captured verbatim and passed through so
        // the frontend can render it if required, never discarded silently.
        return metadata["searchEntryPoint"]?["renderedContent"]?.GetValue<string>();
    }

    private static IEnumerable<GeminiCitation> BuildCitationsForSupport(JsonNode? support, JsonArray chunks)
    {
        JsonNode? segment = support?["segment"];
        var startIndex = segment?["startIndex"]?.GetValue<int>() ?? 0;
        var endIndex = segment?["endIndex"]?.GetValue<int>() ?? 0;
        JsonArray? chunkIndices = support?["groundingChunkIndices"]?.AsArray();
        if (chunkIndices is null)
        {
            yield break;
        }

        foreach (JsonNode? indexNode in chunkIndices)
        {
            var index = indexNode?.GetValue<int>() ?? -1;
            if (index < 0 || index >= chunks.Count)
            {
                continue;
            }

            JsonNode? web = chunks[index]?["web"];
            var uri = web?["uri"]?.GetValue<string>();
            if (string.IsNullOrEmpty(uri))
            {
                continue;
            }

            yield return new GeminiCitation(uri, web?["title"]?.GetValue<string>(), startIndex, endIndex);
        }
    }
}
