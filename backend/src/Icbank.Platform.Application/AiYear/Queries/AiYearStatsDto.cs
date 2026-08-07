namespace Icbank.Platform.Application.AiYear.Queries;

/// <summary>The aggregate stats payload.</summary>
/// <param name="TotalActivations">The total number of activations.</param>
/// <param name="TotalMedia">The total number of attached media rows.</param>
/// <param name="TotalChannels">The number of distinct channels in use.</param>
/// <param name="LastUpdated">The most recent <c>UpdatedAt</c> across all activations, if any exist.</param>
/// <param name="ByMonth">Activation counts keyed by month (1-12, all 12 present).</param>
/// <param name="ByType">Activation counts keyed by type.</param>
/// <param name="ByChannel">Activation counts keyed by channel.</param>
public sealed record AiYearStatsDto(
    int TotalActivations,
    int TotalMedia,
    int TotalChannels,
    DateTime? LastUpdated,
    IReadOnlyDictionary<int, int> ByMonth,
    IReadOnlyDictionary<string, int> ByType,
    IReadOnlyDictionary<string, int> ByChannel);
