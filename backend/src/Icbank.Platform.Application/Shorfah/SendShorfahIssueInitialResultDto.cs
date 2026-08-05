namespace Icbank.Platform.Application.Shorfah;

/// <summary>The response shape for <c>POST /shorfah/issues/:id/send-initial</c> (API-SURFACE.md §19).</summary>
/// <param name="Sent">The total number of initial-contribution notifications sent.</param>
/// <param name="Results">One entry per notification sent.</param>
public sealed record SendShorfahIssueInitialResultDto(int Sent, IReadOnlyList<SendShorfahIssueInitialEntryDto> Results);
