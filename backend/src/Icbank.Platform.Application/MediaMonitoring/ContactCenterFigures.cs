namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>The Contact Center figures for the reporting week.</summary>
/// <param name="Calls">The number of calls received.</param>
/// <param name="Emails">The number of emails received.</param>
/// <param name="Inquiries">The number of inquiries handled.</param>
/// <param name="Notes">Key notes or recurring topics.</param>
public sealed record ContactCenterFigures(int? Calls, int? Emails, int? Inquiries, string? Notes);
