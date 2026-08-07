using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>system_settings</c> → <see cref="SystemSetting"/>.</summary>
/// <remarks>
/// The source's <c>DOMAIN-PORT-NOTES.md</c>-documented concern that <c>azure_ad_client_secret</c>
/// is stored in plaintext in this table applies unchanged here: this migrator copies the
/// <c>value</c> column verbatim, including that secret if a row with that key exists. It does not
/// attempt to redact, rotate or move it to a secrets manager -- that is an operational follow-up
/// for whoever runs the cutover, called out again in docs/DATA-MIGRATION.md.
///
/// The source table has no <c>created_at</c> column (only <c>updated_at</c>), the same
/// synthetic-timestamp situation already handled for <c>shorfah_reminders</c> and
/// <c>style_profile</c>: <see cref="Icbank.Platform.Domain.Common.AuditableEntity.CreatedAt"/> is
/// backfilled from the source's <c>updated_at</c> value rather than left at a default, and this
/// is documented rather than silently approximated.
/// </remarks>
public sealed class SystemSettingTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "system_settings";

    /// <inheritdoc />
    public string DestinationTableName => "system_settings";

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

            DateTime updatedAt = row.GetRawTimestamp("updated_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;

            var entity = new SystemSetting
            {
                Key = row.GetString("key"),
                Value = row.GetString("value"),
                CreatedAt = updatedAt,
                CreatedBy = "data-migration-tool",
                UpdatedAt = updatedAt,
            };

            destination.SystemSettings.Add(entity);
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
        return await destination.SystemSettings.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
