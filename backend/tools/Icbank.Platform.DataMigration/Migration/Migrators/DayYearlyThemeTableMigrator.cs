using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.InternationalDays;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>day_yearly_themes</c> → <see cref="DayYearlyTheme"/>.</summary>
/// <remarks>
/// The source table has no timestamp columns at all; <see cref="Icbank.Platform.Domain.Common.AuditableEntity.CreatedAt"/>
/// is backfilled to the migration run's timestamp, the same synthetic-timestamp treatment already
/// applied to <c>shorfah_reminders</c>, <c>style_profile</c> and <c>system_settings</c>.
/// </remarks>
public sealed class DayYearlyThemeTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "day_yearly_themes";

    /// <inheritdoc />
    public string DestinationTableName => "day_yearly_themes";

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

            var dayId = await context.IdMap.TryGetDestinationIdAsync("international_days", row.GetInt32("day_id"), cancellationToken);
            if (!dayId.HasValue)
            {
                result.RowsSkippedDueToDataIssue++;
                result.Notes.Add($"Source row id={sourceId} references day_id that was not migrated; skipped.");
                continue;
            }

            var entity = new DayYearlyTheme
            {
                DayId = dayId.Value,
                Year = row.GetInt32("year"),
                ThemeAr = row.GetNullableString("theme_ar"),
                ThemeEn = row.GetNullableString("theme_en"),
                ThemeSourceUrl = row.GetNullableString("theme_source_url"),
                CreatedAt = context.DateTimeProvider.UtcNow.UtcDateTime,
                CreatedBy = "data-migration-tool",
            };

            destination.DayYearlyThemes.Add(entity);
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
        return await destination.DayYearlyThemes.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
