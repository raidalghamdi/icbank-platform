namespace Icbank.Platform.Application.Weekend;

/// <summary>The weekend-draft response shape (API-SURFACE.md §10).</summary>
/// <param name="Id">The draft id.</param>
/// <param name="WeekendDate">The ISO date string of the target Thursday.</param>
/// <param name="City">The target city.</param>
/// <param name="Status">The review workflow status.</param>
/// <param name="ModelName">The generating model name.</param>
/// <param name="ContentJson">The content payload as JSON text.</param>
/// <param name="GeneratedByUserId">The generating user's id, if known.</param>
/// <param name="ApprovedByUserId">The approving user's id, if any.</param>
/// <param name="RejectedReason">The rejection reason, if rejected.</param>
/// <param name="ApprovedAt">The UTC timestamp of approval.</param>
/// <param name="PublishedAt">The UTC timestamp of publication.</param>
public sealed record WeekendDraftDto(
    int Id,
    string WeekendDate,
    string City,
    string Status,
    string ModelName,
    string ContentJson,
    int? GeneratedByUserId,
    int? ApprovedByUserId,
    string? RejectedReason,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? PublishedAt);
