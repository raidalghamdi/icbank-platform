namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Result of a <see cref="SendFinalMediaReportEmailCommand"/>.</summary>
/// <param name="Sent">Whether the email was actually dispatched.</param>
/// <param name="Recipients">The recipient addresses the send was attempted for.</param>
/// <param name="ProviderMessage">A human-readable status message.</param>
public sealed record SendFinalMediaReportEmailResultDto(bool Sent, IReadOnlyList<string> Recipients, string ProviderMessage);
