using System.Globalization;
using Icbank.Platform.Domain.Shorfah;

namespace Icbank.Platform.Application.Shorfah;

/// <summary>Shared entity-to-DTO mapping for the Shorfah issue lifecycle, so every handler maps identically.</summary>
public static class ShorfahMappers
{
    /// <summary>Maps a <see cref="ShorfahIssue"/> to its response DTO.</summary>
    /// <param name="issue">The issue to map.</param>
    /// <returns>The mapped DTO.</returns>
    public static ShorfahIssueDto ToDto(ShorfahIssue issue) => new(
        issue.Id,
        issue.IssueNo,
        issue.TitleAr,
        issue.SubtitleAr,
        issue.Month,
        issue.Year,
        issue.CoverImageUrl,
        issue.EditorLetter,
        issue.ContributionsOpenAt,
        issue.ContributionsCloseAt,
        issue.Status.ToString(),
        issue.PublishedPdfUrl,
        issue.PublishedAt,
        issue.CreatedByUserId);

    /// <summary>Maps a <see cref="ShorfahSection"/> to its response DTO.</summary>
    /// <param name="section">The section to map.</param>
    /// <returns>The mapped DTO.</returns>
    public static ShorfahSectionDto ToDto(ShorfahSection section) => new(
        section.Id,
        section.IssueId,
        section.ParentSectionId,
        section.SectionType.ToString(),
        section.TitleAr,
        section.DescriptionAr,
        section.DisplayOrder,
        section.OwnerUserId,
        section.OwnerRole,
        section.IncludeInPdf,
        section.AutoGenerate,
        section.WorkflowStatus.ToString(),
        section.ContentMd,
        section.ContributedByUserId,
        section.ContributedAt,
        section.ReviewedByUserId,
        section.ReviewedAt,
        section.ApprovedByUserId,
        section.ApprovedAt,
        section.RejectionReason,
        section.SlaDays,
        section.SlaStartsAt,
        section.SlaDeadline);

    /// <summary>Maps a <see cref="ShorfahAssignment"/> to its response DTO.</summary>
    /// <param name="assignment">The assignment to map.</param>
    /// <returns>The mapped DTO.</returns>
    public static ShorfahAssignmentDto ToDto(ShorfahAssignment assignment) => new(
        assignment.Id, assignment.SectionId, assignment.UserId, assignment.Role);

    /// <summary>Maps a <see cref="ShorfahReminder"/> to its response DTO.</summary>
    /// <param name="reminder">The reminder to map.</param>
    /// <returns>The mapped DTO.</returns>
    public static ShorfahReminderDto ToDto(ShorfahReminder reminder) => new(
        reminder.Id, reminder.SectionId, reminder.AssignmentId, reminder.SentAt, reminder.ReminderType.ToString());

    /// <summary>Formats an entity id as the invariant-culture string audit logs expect.</summary>
    /// <param name="id">The entity id.</param>
    /// <returns>The invariant-culture string form.</returns>
    public static string IdString(int id) => id.ToString(CultureInfo.InvariantCulture);
}
