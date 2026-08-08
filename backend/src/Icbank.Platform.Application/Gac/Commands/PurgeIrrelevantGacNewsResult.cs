namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>The outcome of a relevance purge.</summary>
/// <param name="Examined">The number of stored news items inspected.</param>
/// <param name="Removed">The number of items deleted as unrelated to competition policy.</param>
public sealed record PurgeIrrelevantGacNewsResult(int Examined, int Removed);
