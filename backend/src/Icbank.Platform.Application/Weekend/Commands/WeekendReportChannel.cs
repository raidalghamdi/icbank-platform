namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>A single requested delivery channel.</summary>
/// <param name="Type">The channel type: <c>email</c>, <c>sms</c>, or <c>whatsapp</c>.</param>
/// <param name="To">The delivery target (email address or phone number).</param>
/// <param name="Kind">An optional sub-kind, e.g. <c>work</c> for email.</param>
public sealed record WeekendReportChannel(string Type, string To, string? Kind);
