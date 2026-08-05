using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>
/// Migrates <c>shorfah_section_sla_defaults</c> → <see cref="ShorfahSectionSlaDefault"/>, keyed
/// on the natural key <c>section_type</c> (the source table's own primary key — the only table
/// in the schema without a surrogate <c>serial</c> id, DATA-MODEL.md section 2). Because the
/// destination primary key is itself the natural key (not a fresh identity value), no
/// source-id → destination-id mapping is needed or recorded for this table; idempotency is
/// achieved by checking for an existing row with the same <see cref="ShorfahSectionType"/>
/// directly. Depends on <see cref="UserTableMigrator"/> for the optional
/// <c>updated_by</c> FK.
/// </summary>
public sealed class ShorfahSectionSlaDefaultTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "shorfah_section_sla_defaults";

    /// <inheritdoc />
    public string DestinationTableName => "shorfah_section_sla_defaults";

    /// <inheritdoc />
    public async Task<TableMigrationResult> MigrateAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        var result = new TableMigrationResult { SourceTableName = SourceTableName };
        DateTimeOffset startedAt = context.DateTimeProvider.UtcNow;

        await using AppDbContext destination = context.CreateDestinationContext();

        await foreach (SourceRow row in context.Source.ReadTableAsync(SourceTableName, cancellationToken))
        {
            result.RowsRead++;
            ShorfahSectionType sectionType = SnakeCaseEnumParser.Parse<ShorfahSectionType>(row.GetString("section_type"));

            ShorfahSectionSlaDefault? existing = await destination.ShorfahSectionSlaDefaults.IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.SectionType == sectionType, cancellationToken);
            if (existing is not null)
            {
                result.RowsSkippedAlreadyMigrated++;
                continue;
            }

            var sourceUpdatedByUserId = row.GetNullableInt32("updated_by");
            var updatedByUserId = sourceUpdatedByUserId.HasValue
                ? await context.IdMap.TryGetDestinationIdAsync("users", sourceUpdatedByUserId.Value, cancellationToken)
                : null;
            if (sourceUpdatedByUserId.HasValue && updatedByUserId is null)
            {
                result.Notes.Add($"shorfah_section_sla_defaults section_type {sectionType}: updated_by {sourceUpdatedByUserId} not found in users id-map — left null.");
            }

            DateTime updatedAt = row.GetRawTimestamp("updated_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;

            var entity = new ShorfahSectionSlaDefault
            {
                SectionType = sectionType,
                SlaDays = row.GetNullableInt32("sla_days") ?? 7,
                CreatedAt = context.DateTimeProvider.UtcNow.UtcDateTime,
                CreatedBy = "data-migration-tool",
                UpdatedAt = updatedAt,
                UpdatedByUserId = updatedByUserId,
            };

            destination.ShorfahSectionSlaDefaults.Add(entity);
            await destination.SaveChangesAsync(cancellationToken);

            result.RowsInserted++;
        }

        result.Duration = context.DateTimeProvider.UtcNow - startedAt;
        return result;
    }

    /// <inheritdoc />
    public async Task<long> CountDestinationRowsAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        await using AppDbContext destination = context.CreateDestinationContext();
        return await destination.ShorfahSectionSlaDefaults.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
