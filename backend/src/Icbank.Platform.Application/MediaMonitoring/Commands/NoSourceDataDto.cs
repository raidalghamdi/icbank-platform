namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>The <c>NO_SOURCE_DATA</c> diagnostic payload (BUSINESS-RULES.md §5.3).</summary>
/// <param name="Code">Always <c>NO_SOURCE_DATA</c>.</param>
/// <param name="TotalPostsAvailable">The total post count across all historical data, regardless of the requested range.</param>
/// <param name="TotalNewsAvailable">The total news count across all historical data, regardless of the requested range.</param>
/// <param name="EarliestAvailableDate">The earliest date with any source data, if any exists.</param>
/// <param name="LatestAvailableDate">The latest date with any source data, if any exists.</param>
public sealed record NoSourceDataDto(
    string Code, int TotalPostsAvailable, int TotalNewsAvailable, DateTimeOffset? EarliestAvailableDate, DateTimeOffset? LatestAvailableDate);
