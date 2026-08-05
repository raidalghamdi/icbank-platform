using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Source;

namespace Icbank.Platform.DataMigration.Mapping.Transformers;

/// <summary>
/// Pure transformer from a raw <c>shorfah_issues</c> row to <see cref="MappedShorfahIssue"/>.
/// Source <c>created_at</c>/<c>updated_at</c> are nullable despite having <c>defaultNow()</c>
/// (AMBIGUOUS-8 in DATA-MODEL.md — the same ambiguity <see cref="ShorfahSectionTransformer"/>
/// resolves for <c>shorfah_sections</c>), but this table has no sibling workflow timestamp to
/// prefer over the migration-run fallback, so <see cref="ShorfahTimestampBackfill"/> is invoked
/// with an empty sibling list for both columns.
/// </summary>
public static class ShorfahIssueTransformer
{
    /// <summary>Transforms one raw <c>shorfah_issues</c> row.</summary>
    /// <param name="row">The raw source row.</param>
    /// <param name="migrationRunTimestamp">The migration run's start time, used only as the last-resort backfill default.</param>
    /// <returns>The mapped, destination-ready DTO.</returns>
    public static MappedShorfahIssue Transform(SourceRow row, DateTime migrationRunTimestamp)
    {
        DateTime? rawCreatedAt = row.GetRawTimestamp("created_at");
        ShorfahTimestampBackfill.BackfillResult created = ShorfahTimestampBackfill.Resolve(
            rawCreatedAt, Array.Empty<DateTime?>(), migrationRunTimestamp, migrationRunTimestamp);

        DateTime? rawUpdatedAt = row.GetRawTimestamp("updated_at");
        ShorfahTimestampBackfill.BackfillResult updated = ShorfahTimestampBackfill.Resolve(
            rawUpdatedAt, new[] { rawCreatedAt }, created.Value, migrationRunTimestamp);

        return new MappedShorfahIssue(
            SourceId: row.GetInt32("id"),
            IssueNo: row.GetInt32("issue_no"),
            TitleAr: row.GetString("title_ar"),
            SubtitleAr: row.GetNullableString("subtitle_ar"),
            Month: row.GetInt32("month"),
            Year: row.GetInt32("year"),
            CoverImageUrl: row.GetNullableString("cover_image_url"),
            EditorLetter: row.GetNullableString("editor_letter"),
            ContributionsOpenAtUtc: row.GetRawTimestamp("contributions_open_at"),
            ContributionsCloseAtUtc: row.GetRawTimestamp("contributions_close_at"),
            Status: row.GetString("status"),
            PublishedPdfUrl: row.GetNullableString("published_pdf_url"),
            PublishedAtUtc: row.GetRawTimestamp("published_at"),
            CreatedBySourceId: row.GetNullableInt32("created_by"),
            CreatedAtUtc: created.Value,
            CreatedAtBackfilled: created.WasBackfilled,
            UpdatedAtUtc: updated.Value,
            UpdatedAtBackfilled: updated.WasBackfilled);
    }
}
