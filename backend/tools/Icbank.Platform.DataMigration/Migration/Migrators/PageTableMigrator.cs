using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>pages</c> → <see cref="Page"/>, keyed on the natural key <c>slug</c> (<c>ux_pages_slug</c>).</summary>
public sealed class PageTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "pages";

    /// <inheritdoc />
    public string DestinationTableName => "pages";

    /// <inheritdoc />
    public async Task<TableMigrationResult> MigrateAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        var result = new TableMigrationResult { SourceTableName = SourceTableName };
        DateTimeOffset startedAt = context.DateTimeProvider.UtcNow;

        await using AppDbContext destination = context.CreateDestinationContext();

        await foreach (SourceRow row in context.Source.ReadTableAsync(SourceTableName, cancellationToken))
        {
            result.RowsRead++;
            int sourceId = row.GetInt32("id");

            int? existingId = await context.IdMap.TryGetDestinationIdAsync(SourceTableName, sourceId, cancellationToken);
            if (existingId.HasValue)
            {
                result.RowsSkippedAlreadyMigrated++;
                continue;
            }

            string slug = row.GetString("slug");
            Page? existingBySlug = await destination.Pages.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
            if (existingBySlug is not null)
            {
                result.RowsSkippedAlreadyMigrated++;
                await context.IdMap.RecordAsync(SourceTableName, sourceId, existingBySlug.Id, context.DateTimeProvider.UtcNow, cancellationToken);
                continue;
            }

            DateTime createdAt = row.GetRawTimestamp("created_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;

            var entity = new Page
            {
                Slug = slug,
                NameAr = row.GetString("name_ar"),
                Icon = row.GetNullableString("icon"),
                SortOrder = row.GetNullableInt32("sort_order") ?? 0,
                IsActive = row.GetBoolean("is_active"),
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.Pages.Add(entity);
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
        return await destination.Pages.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
