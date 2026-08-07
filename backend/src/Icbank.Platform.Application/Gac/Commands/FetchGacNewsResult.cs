namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>The outcome of a provider-driven news fetch.</summary>
/// <param name="Fetched">The total items returned across all providers and terms, before deduplication.</param>
/// <param name="Inserted">The number of new rows written.</param>
/// <param name="Updated">The number of existing rows refreshed.</param>
/// <param name="Skipped">The number of items dropped as duplicates.</param>
/// <param name="PerProvider">Per-provider item counts, so a silently failing provider is visible in the response.</param>
public sealed record FetchGacNewsResult(
    int Fetched,
    int Inserted,
    int Updated,
    int Skipped,
    IReadOnlyDictionary<string, int> PerProvider);
