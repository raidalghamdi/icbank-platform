namespace Icbank.Platform.Application.Gac.News;

/// <summary>
/// The search parameters a fetch run uses, supplied by the composition root.
/// </summary>
/// <remarks>
/// Deliberately separate from the infrastructure options class: the Application layer needs the
/// terms and window to build queries, but must not see provider base URLs or key names.
/// </remarks>
/// <param name="Terms">The search terms to track.</param>
/// <param name="Language">The ISO 639-1 language filter.</param>
/// <param name="Region">The ISO 3166-1 alpha-2 region bias.</param>
/// <param name="WithinDays">The default lookback window in days.</param>
/// <param name="MaxItemsPerTerm">The per-term cap on retrieved items.</param>
public sealed record NewsFetchSettings(
    IReadOnlyList<string> Terms,
    string Language,
    string Region,
    int WithinDays,
    int MaxItemsPerTerm);
