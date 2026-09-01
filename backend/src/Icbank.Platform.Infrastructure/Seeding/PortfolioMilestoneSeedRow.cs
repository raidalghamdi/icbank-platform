namespace Icbank.Platform.Infrastructure.Seeding;

/// <summary>A delivery checkpoint inside a <see cref="PortfolioProjectSeedRow"/>.</summary>
/// <param name="Title">The checkpoint title.</param>
/// <param name="DueOffsetDays">Days from the seed instant to the checkpoint's due date; negative for past dates.</param>
/// <param name="IsCompleted">Whether the checkpoint is already delivered.</param>
internal sealed record PortfolioMilestoneSeedRow(string Title, int DueOffsetDays, bool IsCompleted);
