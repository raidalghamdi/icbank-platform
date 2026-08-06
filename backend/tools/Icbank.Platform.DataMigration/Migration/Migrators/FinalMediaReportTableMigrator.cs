using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.MediaMonitoring;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates approved immutable <c>final_media_reports</c> history into <see cref="FinalMediaReport"/>.</summary>
public sealed class FinalMediaReportTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "final_media_reports";

    /// <inheritdoc />
    public string DestinationTableName => "final_media_reports";

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

            var sourceGeneratedByUserId = row.GetNullableInt32("generated_by_user_id");
            var generatedByUserId = sourceGeneratedByUserId.HasValue
                ? await context.IdMap.TryGetDestinationIdAsync("users", sourceGeneratedByUserId.Value, cancellationToken)
                : null;
            if (sourceGeneratedByUserId.HasValue && generatedByUserId is null)
            {
                result.Notes.Add("A final_media_reports generated_by_user_id was not found in the users id-map and was set to null.");
            }

            DateTime createdAt = row.GetRawTimestamp("created_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;
            DateTime issueDate = row.GetRawTimestamp("issue_date") ?? createdAt;
            DateTime lockedAt = row.GetRawTimestamp("locked_at") ?? createdAt;
            DateTime dateFrom = row.GetRawTimestamp("date_from") ?? createdAt;
            DateTime dateTo = row.GetRawTimestamp("date_to") ?? createdAt;

            var entity = new FinalMediaReport
            {
                ReportNumber = row.GetString("report_number"),
                Title = row.GetString("title"),
                ReportType = SnakeCaseEnumParser.Parse<MediaReportType>(
                    string.IsNullOrEmpty(row.GetNullableString("report_type")) ? "weekly" : row.GetString("report_type")),
                PeriodLabel = row.GetString("period_label"),
                DateFrom = new DateTimeOffset(dateFrom, TimeSpan.Zero),
                DateTo = new DateTimeOffset(dateTo, TimeSpan.Zero),
                PreparedBy = row.GetNullableString("prepared_by"),
                Beneficiary = row.GetNullableString("beneficiary"),
                ReferenceNumber = row.GetNullableString("reference_number"),
                Classification = row.GetNullableString("classification"),
                IssueDate = new DateTimeOffset(issueDate, TimeSpan.Zero),
                Kpis = row.ReadObject<ReportKpis>("kpis") ?? new ReportKpis(),
                ExecutiveSummary = row.GetNullableString("executive_summary"),
                TopNews = row.ReadObjectList<TopNewsItem>("top_news"),
                Timeline = row.ReadObjectList<TimelineEvent>("timeline"),
                DigitalPresence = row.ReadObject<DigitalPresence>("digital_presence") ?? new DigitalPresence(),
                EditorialTone = row.ReadObject<EditorialTone>("editorial_tone") ?? new EditorialTone(),
                DeepAnalysis = row.ReadObject<DeepAnalysis>("deep_analysis") ?? new DeepAnalysis(),
                RegionalComparison = row.ReadObjectList<RegionalComparison>("regional_comparison"),
                Recommendations = row.ReadObjectList<Recommendation>("recommendations"),
                Alerts = row.ReadObjectList<AlertItem>("alerts"),
                QuotesAppendix = row.ReadObjectList<QuoteAppendixItem>("quotes_appendix"),
                Methodology = row.GetNullableString("methodology"),
                Sources = row.ReadObjectList<SourceRef>("sources"),
                SourceItemsJson = row.ReadRawJsonText("source_items", "[]"),
                GeneratedByUserId = generatedByUserId,
                GeneratedByName = row.GetNullableString("generated_by_name"),
                AiModel = row.GetNullableString("ai_model") ?? "gemini-2.5-flash",
                Status = SnakeCaseEnumParser.Parse<FinalMediaReportStatus>(
                    string.IsNullOrEmpty(row.GetNullableString("status")) ? "final" : row.GetString("status")),
                LockedAt = new DateTimeOffset(lockedAt, TimeSpan.Zero),
                ContentSha256 = row.GetString("content_sha256"),
                PdfStorageKey = row.GetNullableString("pdf_storage_key"),
                ViewCount = row.GetNullableInt32("view_count") ?? 0,
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.FinalMediaReports.Add(entity);
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
        return await destination.FinalMediaReports.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
