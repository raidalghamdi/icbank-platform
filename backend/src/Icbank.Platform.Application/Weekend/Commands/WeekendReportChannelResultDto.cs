namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>A single channel's honest dispatch outcome.</summary>
/// <param name="Type">The channel type.</param>
/// <param name="To">The delivery target.</param>
/// <param name="Ok">Whether the channel actually dispatched.</param>
/// <param name="Status">The honest status: always <c>not_implemented</c> in Wave 1.</param>
/// <param name="Error">An error message, if the channel was rejected for input reasons.</param>
public sealed record WeekendReportChannelResultDto(string Type, string To, bool Ok, string Status, string? Error);
