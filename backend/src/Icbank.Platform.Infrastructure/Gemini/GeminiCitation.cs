namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>One grounding citation extracted from Gemini's <c>groundingMetadata</c> (BUSINESS-RULES.md §4 grounding safeguard).</summary>
/// <param name="Url">The cited source URL.</param>
/// <param name="Title">The cited source title, if provided.</param>
/// <param name="StartIndex">The start character offset in the response text the citation covers.</param>
/// <param name="EndIndex">The end character offset in the response text the citation covers.</param>
public sealed record GeminiCitation(string Url, string? Title, int StartIndex, int EndIndex);
