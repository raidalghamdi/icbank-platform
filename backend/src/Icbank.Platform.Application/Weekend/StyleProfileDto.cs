namespace Icbank.Platform.Application.Weekend;

/// <summary>The learned style-profile singleton response shape (API-SURFACE.md §8).</summary>
/// <param name="ToneSummary">A summary of the learned tone.</param>
/// <param name="AvgParagraphLength">The average paragraph length.</param>
/// <param name="OpenerPatterns">Recurring opener sentence patterns.</param>
/// <param name="CloserPatterns">Recurring closer sentence patterns.</param>
/// <param name="RecurringKeywords">Recurring keywords.</param>
/// <param name="QuoteUsage">The quote-usage frequency descriptor.</param>
public sealed record StyleProfileDto(
    string? ToneSummary, float? AvgParagraphLength, IReadOnlyList<string> OpenerPatterns, IReadOnlyList<string> CloserPatterns, IReadOnlyList<string> RecurringKeywords, string? QuoteUsage);
