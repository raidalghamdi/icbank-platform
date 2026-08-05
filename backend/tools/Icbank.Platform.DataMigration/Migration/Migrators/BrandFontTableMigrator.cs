using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Designs;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>brand_fonts</c> → <see cref="BrandFont"/>.</summary>
/// <remarks>
/// The source enforces "only one default font" only in application code; the destination adds a
/// filtered unique index (DATA-01 in DATA-MODEL.md) to enforce it at the database level. If the
/// source has more than one row with <c>is_default = true</c>, every one of them is still
/// migrated verbatim here -- the migrator itself does not decide which one "wins" -- but the
/// destination's filtered unique index means only the first insert will succeed and the rest
/// will fail with a constraint violation. This is flagged in the final migration notes as a case
/// requiring the source data to be checked before cutover, not addressed by disabling or
/// weakening the destination constraint.
/// </remarks>
public sealed class BrandFontTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "brand_fonts";

    /// <inheritdoc />
    public string DestinationTableName => "brand_fonts";

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

            DateTime uploadedAt = row.GetRawTimestamp("uploaded_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;

            var entity = new BrandFont
            {
                FontName = row.GetString("font_name"),
                FontFileUrl = row.GetString("font_file_url"),
                IsDefault = row.GetBoolean("is_default"),
                CreatedAt = uploadedAt,
                CreatedBy = "data-migration-tool",
            };

            destination.BrandFonts.Add(entity);
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
        return await destination.BrandFonts.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
