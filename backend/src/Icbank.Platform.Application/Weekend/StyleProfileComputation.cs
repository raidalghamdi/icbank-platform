namespace Icbank.Platform.Application.Weekend;

/// <summary>The recomputed style-profile fields produced by <see cref="StyleProfileRecalculator.Recompute"/>.</summary>
/// <param name="ToneSummary">The fixed tone-summary label.</param>
/// <param name="AvgParagraphLength">The average paragraph word count.</param>
/// <param name="OpenerPatterns">The first-10 opener sentences.</param>
/// <param name="CloserPatterns">The last-10 closer sentences.</param>
/// <param name="RecurringKeywords">The top-20 recurring non-stopword Arabic keywords.</param>
/// <param name="QuoteUsage">The quote-usage frequency descriptor.</param>
public sealed record StyleProfileComputation(
    string ToneSummary, float AvgParagraphLength, IReadOnlyList<string> OpenerPatterns, IReadOnlyList<string> CloserPatterns, IReadOnlyList<string> RecurringKeywords, string QuoteUsage);
