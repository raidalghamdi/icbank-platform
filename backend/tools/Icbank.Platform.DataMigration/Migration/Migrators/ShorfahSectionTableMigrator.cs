using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Mapping.Transformers;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>
/// Migrates <c>shorfah_sections</c> → <see cref="ShorfahSection"/>. Depends on
/// <c>ShorfahIssueTableMigrator</c> and (for owner/contributor/reviewer/approver) on
/// <see cref="UserTableMigrator"/>. Self-referencing (<c>parent_section_id</c>): two passes —
/// insert every row first with <c>ParentSectionId = null</c>, then a second pass sets it once
/// every row's mapping exists, so parent/child insertion order within the table never matters.
/// </summary>
/// <remarks>
/// See <see cref="ShorfahSectionTransformer"/> and <see cref="Mapping.ShorfahTimestampBackfill"/>
/// for the AMBIGUOUS-8 non-null-timestamp backfill decision (task requirement 3); every backfilled
/// value on a row is recorded as a report note here, never silently applied.
/// </remarks>
public sealed class ShorfahSectionTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "shorfah_sections";

    /// <inheritdoc />
    public string DestinationTableName => "shorfah_sections";

    /// <inheritdoc />
    public async Task<TableMigrationResult> MigrateAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        var result = new TableMigrationResult { SourceTableName = SourceTableName };
        DateTimeOffset startedAt = context.DateTimeProvider.UtcNow;
        DateTime migrationRunTimestamp = context.DateTimeProvider.UtcNow.UtcDateTime;

        await using AppDbContext destination = context.CreateDestinationContext();
        var pendingParentLinks = new List<(int SourceId, int ParentSourceId)>();
        var backfillCount = 0;

        await foreach (SourceRow row in context.Source.ReadTableAsync(SourceTableName, cancellationToken))
        {
            result.RowsRead++;
            MappedShorfahSection mapped = ShorfahSectionTransformer.Transform(row, migrationRunTimestamp);

            var existingId = await context.IdMap.TryGetDestinationIdAsync(SourceTableName, mapped.SourceId, cancellationToken);
            if (existingId.HasValue)
            {
                result.RowsSkippedAlreadyMigrated++;
                if (mapped.ParentSectionSourceId.HasValue)
                {
                    pendingParentLinks.Add((mapped.SourceId, mapped.ParentSectionSourceId.Value));
                }

                continue;
            }

            var issueId = await context.IdMap.TryGetDestinationIdAsync("shorfah_issues", mapped.IssueSourceId, cancellationToken);
            if (issueId is null)
            {
                result.RowsSkippedDueToDataIssue++;
                result.Notes.Add($"shorfah_sections source id {mapped.SourceId}: orphaned issue_id {mapped.IssueSourceId} — skipped.");
                continue;
            }

            var ownerUserId = await ResolveUserId(context, mapped.OwnerUserSourceId, cancellationToken);
            var contributedById = await ResolveUserId(context, mapped.ContributedBySourceId, cancellationToken);
            var reviewedById = await ResolveUserId(context, mapped.ReviewedBySourceId, cancellationToken);
            var approvedById = await ResolveUserId(context, mapped.ApprovedBySourceId, cancellationToken);

            var entity = new ShorfahSection
            {
                IssueId = issueId.Value,
                SectionType = SnakeCaseEnumParser.Parse<ShorfahSectionType>(mapped.SectionType),
                TitleAr = mapped.TitleAr,
                DescriptionAr = mapped.DescriptionAr,
                DisplayOrder = mapped.DisplayOrder,
                OwnerUserId = ownerUserId,
                OwnerRole = mapped.OwnerRole,
                IncludeInPdf = mapped.IncludeInPdf,
                AutoGenerate = mapped.AutoGenerate,
                GenerationPrompt = mapped.GenerationPrompt,
                WorkflowStatus = SnakeCaseEnumParser.Parse<ShorfahWorkflowStatus>(mapped.WorkflowStatus),
                ContentMd = mapped.ContentMd,
                ContentHtml = mapped.ContentHtml,
                ContributedByUserId = contributedById,
                ContributedAt = TimestampConverter.ToDestinationOffset(mapped.ContributedAtUtc),
                ReviewedByUserId = reviewedById,
                ReviewedAt = TimestampConverter.ToDestinationOffset(mapped.ReviewedAtUtc),
                ReviewNotes = mapped.ReviewNotes,
                ApprovedByUserId = approvedById,
                ApprovedAt = TimestampConverter.ToDestinationOffset(mapped.ApprovedAtUtc),
                RejectionReason = mapped.RejectionReason,
                SlaDays = mapped.SlaDays,
                SlaStartsAt = TimestampConverter.ToDestinationOffset(mapped.SlaStartsAtUtc),
                SlaDeadline = TimestampConverter.ToDestinationOffset(mapped.SlaDeadlineUtc),
                CreatedAt = mapped.CreatedAtUtc,
                CreatedBy = "data-migration-tool",
            };

            destination.ShorfahSections.Add(entity);
            await destination.SaveChangesAsync(cancellationToken);

            await context.IdMap.RecordAsync(SourceTableName, mapped.SourceId, entity.Id, context.DateTimeProvider.UtcNow, cancellationToken);
            result.RowsInserted++;

            if (mapped.ParentSectionSourceId.HasValue)
            {
                pendingParentLinks.Add((mapped.SourceId, mapped.ParentSectionSourceId.Value));
            }

            if (mapped.CreatedAtBackfilled || mapped.ContributedAtBackfilled || mapped.ReviewedAtBackfilled || mapped.ApprovedAtBackfilled || mapped.SlaStartsAtBackfilled)
            {
                backfillCount++;
            }
        }

        foreach ((var sourceId, var parentSourceId) in pendingParentLinks)
        {
            var sectionId = await context.IdMap.TryGetDestinationIdAsync(SourceTableName, sourceId, cancellationToken);
            var parentId = await context.IdMap.TryGetDestinationIdAsync(SourceTableName, parentSourceId, cancellationToken);
            if (sectionId is null || parentId is null)
            {
                result.Notes.Add($"shorfah_sections source id {sourceId}: could not resolve parent_section_id {parentSourceId} in second pass — left unset.");
                continue;
            }

            ShorfahSection? entity = await destination.ShorfahSections.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == sectionId, cancellationToken);
            if (entity is not null && entity.ParentSectionId != parentId)
            {
                entity.ParentSectionId = parentId;
                await destination.SaveChangesAsync(cancellationToken);
            }
        }

        if (backfillCount > 0)
        {
            result.Notes.Add($"{backfillCount} shorfah_sections row(s) had one or more nullable-in-source workflow timestamps backfilled (AMBIGUOUS-8) — see docs/DATA-MIGRATION.md.");
        }

        result.Duration = context.DateTimeProvider.UtcNow - startedAt;
        return result;
    }

    /// <inheritdoc />
    public async Task<long> CountDestinationRowsAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        await using AppDbContext destination = context.CreateDestinationContext();
        return await destination.ShorfahSections.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }

    private static async Task<int?> ResolveUserId(MigrationRunContext context, int? userSourceId, CancellationToken cancellationToken) =>
        userSourceId.HasValue
            ? await context.IdMap.TryGetDestinationIdAsync("users", userSourceId.Value, cancellationToken)
            : null;
}
