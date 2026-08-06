namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>The successful result of one Gemini <c>generateContent</c> call.</summary>
/// <param name="Text">The concatenated text of the response's first candidate.</param>
/// <param name="ModelUsed">Which model in the fallback chain actually produced this result.</param>
/// <param name="SearchQueries">The web-search queries Gemini issued, when grounding was requested (empty if it chose not to search).</param>
/// <param name="Citations">The <c>url_citation</c> grounding annotations attached to the response, when grounding was requested.</param>
/// <param name="InlineImages">Base64-encoded inline image parts returned by an image-generation model (e.g. <c>gemini-2.5-flash-image</c>), each paired with its MIME type. Empty for text/JSON calls.</param>
public sealed record GeminiGenerationResult(
    string Text,
    string ModelUsed,
    IReadOnlyList<string> SearchQueries,
    IReadOnlyList<GeminiCitation> Citations,
    IReadOnlyList<GeminiInlineImage> InlineImages);
