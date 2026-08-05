namespace Icbank.Platform.Application.Shorfah;

/// <summary>The Shorfah reminder-record response shape (API-SURFACE.md §19).</summary>
/// <param name="Id">The reminder id.</param>
/// <param name="SectionId">The owning section's id.</param>
/// <param name="AssignmentId">The target assignment's id, if any.</param>
/// <param name="SentAt">The UTC timestamp the reminder was sent.</param>
/// <param name="ReminderType">The reminder type, e.g. <c>initial</c> or <c>overdue</c>.</param>
public sealed record ShorfahReminderDto(int Id, int SectionId, int? AssignmentId, DateTimeOffset? SentAt, string ReminderType);
