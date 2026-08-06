using Icbank.Platform.Infrastructure.Gemini;

namespace Icbank.Platform.UnitTests.Infrastructure.Gemini;

/// <summary>Small factory helpers so Gemini tests can build <see cref="GeminiGenerationResult"/> instances tersely.</summary>
public static class GeminiTestResults
{
    private static readonly string[] SampleSearchQueries = { "بحث تجريبي" };

    /// <summary>Builds a plain-text result with no grounding metadata and no inline images.</summary>
    /// <param name="text">The response text.</param>
    /// <param name="model">The model that produced it.</param>
    /// <returns>The constructed result.</returns>
    public static GeminiGenerationResult Text(string text, string model = "gemini-2.5-flash") =>
        new(text, model, Array.Empty<string>(), Array.Empty<GeminiCitation>(), Array.Empty<GeminiInlineImage>());

    /// <summary>Builds a grounded result carrying at least one search query and citation.</summary>
    /// <param name="text">The response text.</param>
    /// <param name="model">The model that produced it.</param>
    /// <param name="citationUrl">
    /// The citation URL to attach. Named to reflect reality: real Gemini responses carry a Google
    /// redirect here (<c>https://vertexaisearch.cloud.google.com/grounding-api-redirect/...</c>),
    /// never the resolved publisher URL directly.
    /// </param>
    /// <param name="citationTitle">The citation title -- in real responses this is the bare publisher domain (e.g. <c>alriyadh.com</c>), not a headline.</param>
    /// <param name="searchEntryPointHtml">The optional Google "Search Suggestions" HTML, or <c>null</c> to omit it (as most tests should, since it is orthogonal to the behaviour under test).</param>
    /// <returns>The constructed result.</returns>
    public static GeminiGenerationResult Grounded(
        string text,
        string model = "gemini-2.5-flash",
        string citationUrl = "https://vertexaisearch.cloud.google.com/grounding-api-redirect/test-token",
        string citationTitle = "example.gov.sa",
        string? searchEntryPointHtml = null) =>
        new(
            text,
            model,
            SampleSearchQueries,
            new[] { new GeminiCitation(citationUrl, citationTitle, 0, text.Length) },
            Array.Empty<GeminiInlineImage>(),
            searchEntryPointHtml);

    /// <summary>Builds an image result carrying one inline image and no text.</summary>
    /// <param name="base64Data">The base64 image payload.</param>
    /// <param name="mimeType">The image MIME type.</param>
    /// <param name="model">The model that produced it.</param>
    /// <returns>The constructed result.</returns>
    public static GeminiGenerationResult Image(string base64Data, string mimeType = "image/png", string model = "gemini-2.5-flash-image") =>
        new(string.Empty, model, Array.Empty<string>(), Array.Empty<GeminiCitation>(), new[] { new GeminiInlineImage(base64Data, mimeType) });
}
