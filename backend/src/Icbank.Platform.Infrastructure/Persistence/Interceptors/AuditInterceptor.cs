using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Icbank.Platform.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core save-changes interceptor that stamps <c>CreatedAt</c>/<c>CreatedBy</c> on insert and
/// <c>UpdatedAt</c>/<c>UpdatedBy</c> on update for every <see cref="AuditableEntity"/> (R-BE-022).
/// </summary>
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes a new instance of the <see cref="AuditInterceptor"/> class.</summary>
    /// <param name="currentUser">The port used to resolve the acting user's identity.</param>
    public AuditInterceptor(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        StampAuditColumns(eventData.Context);
        return result;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StampAuditColumns(eventData.Context);
        return ValueTask.FromResult(result);
    }

    /// <summary>Walks the change tracker and stamps audit columns on every tracked <see cref="AuditableEntity"/>.</summary>
    /// <param name="context">The <see cref="DbContext"/> about to persist changes, or <c>null</c>.</param>
    private void StampAuditColumns(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        DateTime now = DateTime.UtcNow; // Why: R-BE-026 mandates UTC timestamps for every audit column.
        foreach (EntityEntry<AuditableEntity> entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = _currentUser.UserId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.UpdatedBy = _currentUser.UserId;
                    break;
                case EntityState.Detached:
                case EntityState.Unchanged:
                case EntityState.Deleted:
                default:
                    break;
            }
        }
    }
}
