namespace Icbank.Platform.Application.Shorfah;

/// <summary>The Shorfah notification response shape (API-SURFACE.md §19).</summary>
/// <param name="Id">The notification id.</param>
/// <param name="IssueId">The related issue's id, if any.</param>
/// <param name="SectionId">The related section's id, if any.</param>
/// <param name="Type">The notification type, e.g. <c>initial</c>, <c>reminder_overdue</c>, <c>published</c>.</param>
/// <param name="Title">The notification title.</param>
/// <param name="Body">The notification body.</param>
/// <param name="Url">A relative in-app URL to navigate to.</param>
/// <param name="IsRead">Whether the notification has been read.</param>
/// <param name="CreatedAt">The UTC timestamp the notification was created.</param>
public sealed record ShorfahNotificationDto(
    int Id,
    int? IssueId,
    int? SectionId,
    string Type,
    string Title,
    string? Body,
    string? Url,
    bool? IsRead,
    DateTime CreatedAt);
