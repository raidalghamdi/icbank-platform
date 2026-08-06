namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>The successful result of one Gemini <c>generateContent</c> call.</summary>
/// <param name="Text">The concatenated text of the response's first candidate.</param>
/// <param name="ModelUsed">Which model in the fallback chain actually produced this result.</param>
/// <param name="SearchQueries">The web-search queries Gemini issued (<c>groundingMetadata.webSearchQueries</c>), when grounding was requested (empty if it chose not to search).</param>
/// <param name="Citations">
/// The grounding citations built from <c>groundingMetadata.groundingChunks</c>/<c>groundingSupports</c>,
/// when grounding was requested. See <see cref="GeminiCitation"/> for the important caveat that
/// <see cref="GeminiCitation.Url"/> is a Google redirect, not the resolved publisher URL.
/// </param>
/// <param name="InlineImages">Base64-encoded inline image parts returned by an image-generation model (e.g. <c>gemini-2.5-flash-image</c>), each paired with its MIME type. Empty for text/JSON calls.</param>
/// <param name="SearchEntryPointHtml">
/// <c>groundingMetadata.searchEntryPoint.renderedContent</c> verbatim -- the Google-mandated
/// "Search Suggestions" HTML snippet, when grounding was requested and Gemini searched. Null when
/// absent (no grounding, or this specific field was omitted). Passed through unmodified so the
/// frontend can render it if Google's terms require it; this port does not interpret or strip it.
/// Whether rendering is actually mandatory for this product's usage is an open question -- see
/// GEMINI-ADAPTER-NOTES.md.
/// </param>
public sealed record GeminiGenerationResult(
    string Text,
    string ModelUsed,
    IReadOnlyList<string> SearchQueries,
    IReadOnlyList<GeminiCitation> Citations,
    IReadOnlyList<GeminiInlineImage> InlineImages,
    string? SearchEntryPointHtml = null);
