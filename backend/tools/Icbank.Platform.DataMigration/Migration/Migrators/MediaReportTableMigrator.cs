using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.MediaMonitoring;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>media_reports</c> → <see cref="MediaReport"/>.</summary>
public sealed class MediaReportTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "media_reports";

    /// <inheritdoc />
    public string DestinationTableName => "media_reports";

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
            DateTime dateFrom = row.GetRawTimestamp("date_from") ?? createdAt;
            DateTime dateTo = row.GetRawTimestamp("date_to") ?? createdAt;

            int? generatedByUserId = null;
            var sourceGeneratedBy = row.GetNullableInt32("generated_by_user_id");
            if (sourceGeneratedBy.HasValue)
            {
                generatedByUserId = await context.IdMap.TryGetDestinationIdAsync("users", sourceGeneratedBy.Value, cancellationToken);
            }

            var entity = new MediaReport
            {
                Title = row.GetString("title"),
                ReportType = SnakeCaseEnumParser.Parse<MediaReportType>(
                    string.IsNullOrEmpty(row.GetNullableString("report_type")) ? "weekly" : row.GetString("report_type")),
                Audience = SnakeCaseEnumParser.Parse<MediaReportAudience>(
                    string.IsNullOrEmpty(row.GetNullableString("audience")) ? "manager" : row.GetString("audience")),
                DateFrom = new DateTimeOffset(dateFrom, TimeSpan.Zero),
                DateTo = new DateTimeOffset(dateTo, TimeSpan.Zero),
                Sources = row.GetStringArray("sources").ToList(),
                ExecutiveSummary = row.GetNullableString("executive_summary"),
                ContentMd = row.GetString("content_md"),
                Stats = row.ReadObject<MediaReportStats>("stats"),
                OverallTone = row.GetNullableString("overall_tone"),
                SourceItemsJson = row.ReadRawJsonText("source_items", "[]"),
                GeneratedByUserId = generatedByUserId,
                GeneratedByName = row.GetNullableString("generated_by_name"),
                AiModel = row.GetNullableString("ai_model") ?? "gemini-2.5-flash",
                Status = SnakeCaseEnumParser.Parse<MediaReportStatus>(
                    string.IsNullOrEmpty(row.GetNullableString("status")) ? "published" : row.GetString("status")),
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
                UpdatedAt = row.GetRawTimestamp("updated_at"),
            };

            destination.MediaReports.Add(entity);
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
        return await destination.MediaReports.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
