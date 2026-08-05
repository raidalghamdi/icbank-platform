namespace Icbank.Platform.Application.Dashboard.Commands;

/// <summary>The generated executive-summary response shape.</summary>
/// <param name="Summary">The generated Arabic summary text.</param>
/// <param name="GeneratedAt">The UTC timestamp of generation.</param>
public sealed record ExecutiveSummaryDto(string Summary, DateTimeOffset GeneratedAt);
