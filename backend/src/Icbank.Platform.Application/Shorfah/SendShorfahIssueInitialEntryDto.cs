namespace Icbank.Platform.Application.Shorfah;

/// <summary>One per-assignment result row within <see cref="SendShorfahIssueInitialResultDto"/> (API-SURFACE.md §19).</summary>
/// <param name="SectionId">The section the notification was sent for.</param>
/// <param name="UserId">The recipient's user id.</param>
/// <param name="Status">The send status, always <c>"sent"</c> once persisted, matching the Node source.</param>
public sealed record SendShorfahIssueInitialEntryDto(int SectionId, int UserId, string Status);
