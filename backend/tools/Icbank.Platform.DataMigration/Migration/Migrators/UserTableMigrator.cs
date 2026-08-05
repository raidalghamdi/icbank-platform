using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Mapping.Transformers;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>
/// Migrates <c>users</c> → <see cref="User"/>. First table in the FK-safe dependency order —
/// every other Identity/RBAC and feature table's <c>*_by</c>/<c>owner_*</c> columns reference it.
/// </summary>
/// <remarks>
/// Idempotency: uses the natural key (<c>email</c>, unique in both schemas —
/// <c>ux_users_email</c>) to detect a row that already exists in the destination even if the
/// id-mapping store itself was lost, in addition to the id-mapping fast path. This makes the
/// migrator safe to re-run even after a catastrophic failure of the id-map table itself.
/// </remarks>
public sealed class UserTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "users";

    /// <inheritdoc />
    public string DestinationTableName => "users";

    /// <inheritdoc />
    public async Task<TableMigrationResult> MigrateAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        var result = new TableMigrationResult { SourceTableName = SourceTableName };
        DateTimeOffset startedAt = context.DateTimeProvider.UtcNow;

        await using AppDbContext destination = context.CreateDestinationContext();

        await foreach (SourceRow row in context.Source.ReadTableAsync(SourceTableName, cancellationToken))
        {
            result.RowsRead++;
            MappedUser mapped = UserTransformer.Transform(row);

            var existingId = await context.IdMap.TryGetDestinationIdAsync(SourceTableName, mapped.SourceId, cancellationToken);
            if (existingId.HasValue)
            {
                result.RowsSkippedAlreadyMigrated++;
                continue;
            }

            User? existingByEmail = await destination.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == mapped.Email, cancellationToken);
            if (existingByEmail is not null)
            {
                result.RowsSkippedAlreadyMigrated++;
                await context.IdMap.RecordAsync(SourceTableName, mapped.SourceId, existingByEmail.Id, context.DateTimeProvider.UtcNow, cancellationToken);
                continue;
            }

            var entity = new User
            {
                Email = mapped.Email,
                Name = mapped.Name,
                Title = mapped.Title,
                Department = mapped.Department,
                PasswordHash = mapped.PasswordHash,
                MustChangePassword = mapped.MustChangePassword,
                AzureOid = mapped.AzureOid,
                IsActive = mapped.IsActive,
                IsLocked = mapped.IsLocked,
                FailedAttempts = mapped.FailedAttempts,
                LastLogin = mapped.LastLogin?.UtcDateTime,
                PasswordChangedAt = mapped.PasswordChangedAt?.UtcDateTime,
                CreatedAt = mapped.CreatedAtUtc,
                CreatedBy = "data-migration-tool",
            };

            destination.Users.Add(entity);
            await destination.SaveChangesAsync(cancellationToken);

            await context.IdMap.RecordAsync(SourceTableName, mapped.SourceId, entity.Id, context.DateTimeProvider.UtcNow, cancellationToken);
            result.RowsInserted++;

            if (mapped.PasswordOutcome == PasswordMigrationOutcome.BcryptHashNotPortableMustReset)
            {
                result.Notes.Add($"User source id {mapped.SourceId}: bcrypt password hash not portable — migrated with PasswordHash=null, MustChangePassword=true.");
            }
        }

        result.Duration = context.DateTimeProvider.UtcNow - startedAt;
        return result;
    }

    /// <inheritdoc />
    public async Task<long> CountDestinationRowsAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        await using AppDbContext destination = context.CreateDestinationContext();
        return await destination.Users.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
