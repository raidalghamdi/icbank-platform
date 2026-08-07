using System.Text.Json;
using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Weekend;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>weekend_drafts</c> → <see cref="WeekendDraft"/>.</summary>
/// <remarks>
/// <c>generated_by</c> and <c>approved_by</c> were unenforced implied FKs in the source schema;
/// the port makes both proper, enforced, optional foreign keys, so a source value with no
/// corresponding migrated <c>users</c> row is dropped (set to <see langword="null"/>) rather than
/// left pointing at a non-existent id. The free-form <c>content</c> jsonb payload is carried
/// across verbatim as JSON text (no destination schema imposed on the places/deals/podcasts/
/// aiTools/matches/movies bundle), matching <see cref="WeekendDraft.ContentJson"/>'s documented
/// untyped-text shape.
/// </remarks>
public sealed class WeekendDraftTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "weekend_drafts";

    /// <inheritdoc />
    public string DestinationTableName => "weekend_drafts";

    /// <inheritdoc />
    public async Task<TableMigrationResult> MigrateAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        var result = new TableMigrationResult { SourceTableName = SourceTableName };
        DateTimeOffset startedAt = context.DateTimeProvider.UtcNow;

        await using AppDbContext destination = context.CreateDestinationContext();

        await foreach (SourceRow row in context.Source.ReadTableAsync(SourceTableName, cancellationToken))
        {
            result.RowsRead++;
            var sourceId = row.GetInt32("id");

            var existingId = await context.IdMap.TryGetDestinationIdAsync(SourceTableName, sourceId, cancellationToken);
            if (existingId.HasValue)
            {
                result.RowsSkippedAlreadyMigrated++;
                continue;
            }

            DateTime createdAt = row.GetRawTimestamp("created_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;

            int? generatedByUserId = null;
            var sourceGeneratedBy = row.GetNullableInt32("generated_by");
            if (sourceGeneratedBy.HasValue)
            {
                generatedByUserId = await context.IdMap.TryGetDestinationIdAsync("users", sourceGeneratedBy.Value, cancellationToken);
            }

            int? approvedByUserId = null;
            var sourceApprovedBy = row.GetNullableInt32("approved_by");
            if (sourceApprovedBy.HasValue)
            {
                approvedByUserId = await context.IdMap.TryGetDestinationIdAsync("users", sourceApprovedBy.Value, cancellationToken);
            }

            var contentRaw = row["content"];
            var contentJson = contentRaw switch
            {
                null => "{}",
                string s => s,
                JsonElement je => je.GetRawText(),
                object other => JsonSerializer.Serialize(other),
            };

            var entity = new WeekendDraft
            {
                WeekendDate = row.GetString("weekend_date"),
                City = string.IsNullOrEmpty(row.GetNullableString("city")) ? "الرياض" : row.GetString("city"),
                Status = SnakeCaseEnumParser.Parse<WeekendDraftStatus>(
                    string.IsNullOrEmpty(row.GetNullableString("status")) ? "pending_review" : row.GetString("status")),
                ModelName = string.IsNullOrEmpty(row.GetNullableString("model_name")) ? "gemini-2.0-flash-exp" : row.GetString("model_name"),
                ContentJson = contentJson,
                GeneratedByUserId = generatedByUserId,
                ApprovedByUserId = approvedByUserId,
                RejectedReason = row.GetNullableString("rejected_reason"),
                ApprovedAt = row.GetRawTimestamp("approved_at") is { } approvedAt ? new DateTimeOffset(approvedAt, TimeSpan.Zero) : null,
                PublishedAt = row.GetRawTimestamp("published_at") is { } publishedAt ? new DateTimeOffset(publishedAt, TimeSpan.Zero) : null,
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.WeekendDrafts.Add(entity);
            await destination.SaveChangesAsync(cancellationToken);

            await context.IdMap.RecordAsync(SourceTableName, sourceId, entity.Id, context.DateTimeProvider.UtcNow, cancellationToken);
            result.RowsInserted++;
        }

        result.Duration = context.DateTimeProvider.UtcNow - startedAt;
        return result;
    }

    /// <inheritdoc />
    public async Task<long> CountDestinationRowsAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        await using AppDbContext destination = context.CreateDestinationContext();
        return await destination.WeekendDrafts.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
