using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Designs;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>brand_logos</c> → <see cref="BrandLogo"/>.</summary>
public sealed class BrandLogoTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "brand_logos";

    /// <inheritdoc />
    public string DestinationTableName => "brand_logos";

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

            // uploaded_at is the source's only timestamp column; it is the closest equivalent
            // to a created_at for this AuditableEntity's required field.
            DateTime uploadedAt = row.GetRawTimestamp("uploaded_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;

            var entity = new BrandLogo
            {
                LogoName = row.GetString("logo_name"),
                FileUrl = row.GetString("file_url"),
                Transparent = row.GetBoolean("transparent"),
                DefaultWidth = row.GetNullableInt32("default_width"),
                CreatedAt = uploadedAt,
                CreatedBy = "data-migration-tool",
            };

            destination.BrandLogos.Add(entity);
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
        return await destination.BrandLogos.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
