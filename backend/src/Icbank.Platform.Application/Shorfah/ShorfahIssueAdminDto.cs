namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// Admin-view response shape for <c>GET /shorfah/issues/:id/admin</c> (API-SURFACE.md §19):
/// sections + assignments + reminders for an issue.
/// </summary>
/// <param name="Sections">The issue's sections, ordered by display order.</param>
/// <param name="Assignments">Every assignment across the issue's sections.</param>
/// <param name="Reminders">Every reminder sent across the issue's sections, most recent first.</param>
public sealed record ShorfahIssueAdminDto(
    IReadOnlyList<ShorfahSectionDto> Sections,
    IReadOnlyList<ShorfahAssignmentDto> Assignments,
    IReadOnlyList<ShorfahReminderDto> Reminders);
