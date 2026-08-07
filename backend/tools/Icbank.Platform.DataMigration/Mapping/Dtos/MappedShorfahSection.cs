namespace Icbank.Platform.DataMigration.Mapping.Dtos;

/// <summary>Pure DTO produced by <see cref="Transformers.ShorfahSectionTransformer"/>.</summary>
/// <param name="SourceId">The source Postgres <c>shorfah_sections.id</c>.</param>
/// <param name="IssueSourceId">The source <c>shorfah_issues.id</c> this section belongs to.</param>
/// <param name="ParentSectionSourceId">The source parent section id, if this is a sub-section.</param>
/// <param name="SectionType">One of the 13 canonical section types.</param>
/// <param name="TitleAr">The Arabic title.</param>
/// <param name="DescriptionAr">The optional Arabic description.</param>
/// <param name="DisplayOrder">The sort order within the issue.</param>
/// <param name="OwnerUserSourceId">The source owning-user id, if any.</param>
/// <param name="OwnerRole">The owning role name, if any.</param>
/// <param name="IncludeInPdf">Whether this section renders into the final PDF.</param>
/// <param name="AutoGenerate">Whether AI auto-generation is enabled.</param>
/// <param name="GenerationPrompt">The custom AI prompt override, if any.</param>
/// <param name="WorkflowStatus">The workflow state machine status.</param>
/// <param name="ContentMd">The markdown content.</param>
/// <param name="ContentHtml">The rendered HTML content (dead write path upstream — carried over for fidelity, never rendered raw downstream).</param>
/// <param name="ContributedBySourceId">The source contributing-user id, if any.</param>
/// <param name="ContributedAtUtc">The resolved (possibly backfilled) contribution timestamp.</param>
/// <param name="ContributedAtBackfilled">Whether <paramref name="ContributedAtUtc"/> is synthetic.</param>
/// <param name="ReviewedBySourceId">The source reviewing-user id, if any.</param>
/// <param name="ReviewedAtUtc">The resolved (possibly backfilled) review timestamp.</param>
/// <param name="ReviewedAtBackfilled">Whether <paramref name="ReviewedAtUtc"/> is synthetic.</param>
/// <param name="ReviewNotes">Free-text review notes.</param>
/// <param name="ApprovedBySourceId">The source approving-user id, if any.</param>
/// <param name="ApprovedAtUtc">The resolved (possibly backfilled) approval timestamp.</param>
/// <param name="ApprovedAtBackfilled">Whether <paramref name="ApprovedAtUtc"/> is synthetic.</param>
/// <param name="RejectionReason">Free-text rejection reason.</param>
/// <param name="SlaDays">The SLA window, in days.</param>
/// <param name="SlaStartsAtUtc">The resolved (possibly backfilled) SLA start timestamp.</param>
/// <param name="SlaStartsAtBackfilled">Whether <paramref name="SlaStartsAtUtc"/> is synthetic.</param>
/// <param name="SlaDeadlineUtc">The SLA deadline, if the source recorded/derived one.</param>
/// <param name="CreatedAtUtc">The resolved (possibly backfilled) row-creation instant.</param>
/// <param name="CreatedAtBackfilled">Whether <paramref name="CreatedAtUtc"/> is synthetic (source <c>created_at</c> is nullable, unusually — DATA-MODEL.md §3.8).</param>
public sealed record MappedShorfahSection(
    int SourceId,
    int IssueSourceId,
    int? ParentSectionSourceId,
    string SectionType,
    string TitleAr,
    string? DescriptionAr,
    int DisplayOrder,
    int? OwnerUserSourceId,
    string? OwnerRole,
    bool IncludeInPdf,
    bool? AutoGenerate,
    string? GenerationPrompt,
    string WorkflowStatus,
    string? ContentMd,
    string? ContentHtml,
    int? ContributedBySourceId,
    DateTime ContributedAtUtc,
    bool ContributedAtBackfilled,
    int? ReviewedBySourceId,
    DateTime ReviewedAtUtc,
    bool ReviewedAtBackfilled,
    string? ReviewNotes,
    int? ApprovedBySourceId,
    DateTime ApprovedAtUtc,
    bool ApprovedAtBackfilled,
    string? RejectionReason,
    int? SlaDays,
    DateTime SlaStartsAtUtc,
    bool SlaStartsAtBackfilled,
    DateTime? SlaDeadlineUtc,
    DateTime CreatedAtUtc,
    bool CreatedAtBackfilled);
