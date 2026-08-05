using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.MediaMonitoring;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>reports_qa_queries</c> → <see cref="ReportsQaQuery"/>.</summary>
/// <remarks>
/// <c>final_report_id</c> is looked up through the id-mapping store for <c>final_media_reports</c>,
/// but <c>FinalMediaReportTableMigrator</c> does not exist yet (see
/// spec/DATA-MIGRATION-NOTES.md -- <c>final_media_reports</c> is explicitly listed as NOT
/// covered). Every row's <c>final_report_id</c> will therefore resolve to <see langword="null"/>
/// until that migrator is written and run first; this is not a bug in this migrator, it is a
/// direct, documented consequence of the still-open gap.
/// </remarks>
public sealed class ReportsQaQueryTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "reports_qa_queries";

    /// <inheritdoc />
    public string DestinationTableName => "reports_qa_queries";

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

            int? userId = null;
            var sourceUserId = row.GetNullableInt32("user_id");
            if (sourceUserId.HasValue)
            {
                userId = await context.IdMap.TryGetDestinationIdAsync("users", sourceUserId.Value, cancellationToken);
            }

            int? finalReportId = null;
            var sourceFinalReportId = row.GetNullableInt32("final_report_id");
            if (sourceFinalReportId.HasValue)
            {
                finalReportId = await context.IdMap.TryGetDestinationIdAsync("final_media_reports", sourceFinalReportId.Value, cancellationToken);
            }

            var entity = new ReportsQaQuery
            {
                UserId = userId,
                UserName = row.GetNullableString("user_name"),
                QueryType = SnakeCaseEnumParser.Parse<QaQueryType>(row.GetString("query_type")),
                WizardAnswers = row.ReadObject<WizardAnswers>("wizard_answers"),
                SearchQuery = row.GetNullableString("search_query"),
                FinalReportId = finalReportId,
                ResultSummary = row.GetNullableString("result_summary"),
                MetadataJson = row["metadata"] is null ? null : row.ReadRawJsonText("metadata", "{}"),
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.ReportsQaQueries.Add(entity);
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
        return await destination.ReportsQaQueries.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
