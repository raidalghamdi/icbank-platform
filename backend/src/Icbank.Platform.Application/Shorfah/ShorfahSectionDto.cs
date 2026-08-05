namespace Icbank.Platform.Application.Shorfah;

/// <summary>The Shorfah section response shape (API-SURFACE.md §19, BUSINESS-RULES.md §1.3).</summary>
/// <param name="Id">The section id.</param>
/// <param name="IssueId">The owning issue's id.</param>
/// <param name="ParentSectionId">The parent section's id, for sub-sections.</param>
/// <param name="SectionType">The section type.</param>
/// <param name="TitleAr">The Arabic title.</param>
/// <param name="DescriptionAr">The optional Arabic description.</param>
/// <param name="DisplayOrder">The display sort order.</param>
/// <param name="OwnerUserId">The id of the section's owning user, if any.</param>
/// <param name="OwnerRole">The owning role name, if scoped by role rather than user.</param>
/// <param name="IncludeInPdf">Whether the section is included in the published PDF.</param>
/// <param name="AutoGenerate">Whether the section content is AI-auto-generated.</param>
/// <param name="WorkflowStatus">The contribution workflow status.</param>
/// <param name="ContentMd">The markdown content body.</param>
/// <param name="ContributedByUserId">The id of the user who contributed, if any.</param>
/// <param name="ContributedAt">The UTC timestamp of contribution.</param>
/// <param name="ReviewedByUserId">The id of the user who reviewed, if any.</param>
/// <param name="ReviewedAt">The UTC timestamp of review.</param>
/// <param name="ApprovedByUserId">The id of the user who gave final approval, if any.</param>
/// <param name="ApprovedAt">The UTC timestamp of approval.</param>
/// <param name="RejectionReason">The rejection reason, if rejected.</param>
/// <param name="SlaDays">The SLA day count for this section.</param>
/// <param name="SlaStartsAt">The UTC timestamp the SLA clock started.</param>
/// <param name="SlaDeadline">The computed SLA deadline.</param>
public sealed record ShorfahSectionDto(
    int Id,
    int IssueId,
    int? ParentSectionId,
    string SectionType,
    string TitleAr,
    string? DescriptionAr,
    int DisplayOrder,
    int? OwnerUserId,
    string? OwnerRole,
    bool IncludeInPdf,
    bool? AutoGenerate,
    string WorkflowStatus,
    string? ContentMd,
    int? ContributedByUserId,
    DateTimeOffset? ContributedAt,
    int? ReviewedByUserId,
    DateTimeOffset? ReviewedAt,
    int? ApprovedByUserId,
    DateTimeOffset? ApprovedAt,
    string? RejectionReason,
    int? SlaDays,
    DateTimeOffset? SlaStartsAt,
    DateTimeOffset? SlaDeadline);
