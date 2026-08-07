namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>The honest (non-fabricated) dispatch result.</summary>
/// <param name="Ok">Whether at least one channel actually dispatched (always <c>false</c> today -- no provider is wired).</param>
/// <param name="Period">The reporting period label, echoed back.</param>
/// <param name="Provider">The requested provider, echoed back.</param>
/// <param name="Channels">The number of requested channels.</param>
/// <param name="Dispatched">The number of channels actually dispatched (always 0 today).</param>
/// <param name="Results">Per-channel results.</param>
public sealed record SendWeekendReportResultDto(
    bool Ok, string Period, string Provider, int Channels, int Dispatched, IReadOnlyList<WeekendReportChannelResultDto> Results);
