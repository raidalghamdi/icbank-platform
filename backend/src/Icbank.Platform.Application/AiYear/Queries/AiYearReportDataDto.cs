namespace Icbank.Platform.Application.AiYear.Queries;

/// <summary>The report data payload.</summary>
/// <param name="TotalActivations">The total number of activations.</param>
/// <param name="TotalMedia">The total number of attached media rows.</param>
/// <param name="TotalChannels">The number of distinct channels in use.</param>
/// <param name="ByType">Activation counts keyed by type.</param>
/// <param name="TopByReach">The top 3 activations by reach (descending, nulls excluded, no other weighting).</param>
/// <param name="Rows">Every activation, in month-then-newest-first order, for the full table section.</param>
public sealed record AiYearReportDataDto(
    int TotalActivations,
    int TotalMedia,
    int TotalChannels,
    IReadOnlyDictionary<string, int> ByType,
    IReadOnlyList<AiYearReportRowDto> TopByReach,
    IReadOnlyList<AiYearReportRowDto> Rows);
