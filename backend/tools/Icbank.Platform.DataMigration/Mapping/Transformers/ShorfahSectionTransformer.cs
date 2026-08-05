using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Source;

namespace Icbank.Platform.DataMigration.Mapping.Transformers;

/// <summary>
/// Pure transformer from a raw <c>shorfah_sections</c> row to <see cref="MappedShorfahSection"/>.
/// Resolves every nullable-in-source workflow timestamp (now non-null in the destination —
/// AMBIGUOUS-8) via <see cref="ShorfahTimestampBackfill"/>, and flags every backfilled value so
/// the report can list them (task requirement 3).
/// </summary>
public static class ShorfahSectionTransformer
{
    /// <summary>Transforms one raw <c>shorfah_sections</c> row.</summary>
    /// <param name="row">The raw source row.</param>
    /// <param name="migrationRunTimestamp">The migration run's start time, used only as the last-resort backfill default.</param>
    /// <returns>The mapped, destination-ready DTO.</returns>
    public static MappedShorfahSection Transform(SourceRow row, DateTime migrationRunTimestamp)
    {
        // shorfah_sections.created_at is itself nullable in source (DATA-MODEL.md §3.8 flags
        // this as "nullable, inconsistent with rest of schema") -- resolve it first since every
        // other backfill in this row falls back to it.
        DateTime? rawCreatedAt = row.GetRawTimestamp("created_at");
        DateTime resolvedCreatedAt = rawCreatedAt ?? migrationRunTimestamp;
        bool createdAtBackfilled = !rawCreatedAt.HasValue;

        DateTime? contributedAt = row.GetRawTimestamp("contributed_at");
        DateTime? reviewedAt = row.GetRawTimestamp("reviewed_at");
        DateTime? approvedAt = row.GetRawTimestamp("approved_at");
        DateTime? slaStartsAt = row.GetRawTimestamp("sla_starts_at");

        ShorfahTimestampBackfill.BackfillResult contributed = ShorfahTimestampBackfill.Resolve(
            contributedAt, new[] { reviewedAt, approvedAt }, resolvedCreatedAt, migrationRunTimestamp);
        ShorfahTimestampBackfill.BackfillResult reviewed = ShorfahTimestampBackfill.Resolve(
            reviewedAt, new[] { approvedAt, contributedAt }, resolvedCreatedAt, migrationRunTimestamp);
        ShorfahTimestampBackfill.BackfillResult approved = ShorfahTimestampBackfill.Resolve(
            approvedAt, new[] { reviewedAt, contributedAt }, resolvedCreatedAt, migrationRunTimestamp);
        ShorfahTimestampBackfill.BackfillResult slaStarts = ShorfahTimestampBackfill.Resolve(
            slaStartsAt, new[] { contributedAt }, resolvedCreatedAt, migrationRunTimestamp);

        return new MappedShorfahSection(
            SourceId: row.GetInt32("id"),
            IssueSourceId: row.GetInt32("issue_id"),
            ParentSectionSourceId: row.GetNullableInt32("parent_section_id"),
            SectionType: row.GetString("section_type"),
            TitleAr: row.GetString("title_ar"),
            DescriptionAr: row.GetNullableString("description_ar"),
            DisplayOrder: row.GetNullableInt32("display_order") ?? 0,
            OwnerUserSourceId: row.GetNullableInt32("owner_user_id"),
            OwnerRole: row.GetNullableString("owner_role"),
            IncludeInPdf: row.GetBoolean("include_in_pdf"),
            AutoGenerate: row.GetNullableBoolean("auto_generate"),
            GenerationPrompt: row.GetNullableString("generation_prompt"),
            WorkflowStatus: row.GetString("workflow_status"),
            ContentMd: row.GetNullableString("content_md"),
            ContentHtml: row.GetNullableString("content_html"),
            ContributedBySourceId: row.GetNullableInt32("contributed_by"),
            ContributedAtUtc: contributed.Value,
            ContributedAtBackfilled: contributed.WasBackfilled,
            ReviewedBySourceId: row.GetNullableInt32("reviewed_by"),
            ReviewedAtUtc: reviewed.Value,
            ReviewedAtBackfilled: reviewed.WasBackfilled,
            ReviewNotes: row.GetNullableString("review_notes"),
            ApprovedBySourceId: row.GetNullableInt32("approved_by"),
            ApprovedAtUtc: approved.Value,
            ApprovedAtBackfilled: approved.WasBackfilled,
            RejectionReason: row.GetNullableString("rejection_reason"),
            SlaDays: row.GetNullableInt32("sla_days"),
            SlaStartsAtUtc: slaStarts.Value,
            SlaStartsAtBackfilled: slaStarts.WasBackfilled,
            SlaDeadlineUtc: row.GetRawTimestamp("sla_deadline"),
            CreatedAtUtc: resolvedCreatedAt,
            CreatedAtBackfilled: createdAtBackfilled);
    }
}
