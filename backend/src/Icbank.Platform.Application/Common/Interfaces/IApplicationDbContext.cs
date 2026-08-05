using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>
/// Application-layer port onto the persistence context (R-BE-002: Application may not reference
/// EF Core directly — this interface exposes only <see cref="IQueryable{T}"/> for reads and
/// explicit Add/Remove/SaveChanges methods for writes, never <c>DbSet&lt;T&gt;</c> or
/// <c>DbContext</c> itself). Implemented by <c>AppDbContext</c> in Infrastructure. Only the
/// identity/RBAC sets needed by the auth and admin handlers are exposed here — other feature
/// areas will grow their own narrow slice of this interface as they're ported.
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>Gets a queryable over platform user accounts.</summary>
    IQueryable<User> Users { get; }

    /// <summary>Gets a queryable over RBAC roles.</summary>
    IQueryable<Role> Roles { get; }

    /// <summary>Gets a queryable over RBAC-gated app pages.</summary>
    IQueryable<Page> Pages { get; }

    /// <summary>Gets a queryable over the four action-verb permissions.</summary>
    IQueryable<Permission> Permissions { get; }

    /// <summary>Gets a queryable over the role × page × permission grant matrix.</summary>
    IQueryable<RolePermission> RolePermissions { get; }

    /// <summary>Gets a queryable over user → role assignments (many-to-many; union-of-permissions semantics).</summary>
    IQueryable<UserRole> UserRoles { get; }

    /// <summary>Gets a queryable over per-user allow/deny permission overrides.</summary>
    IQueryable<UserPageOverride> UserPageOverrides { get; }

    /// <summary>Gets a queryable over the audit trail of auth/admin events.</summary>
    IQueryable<ActivityLog> ActivityLogs { get; }

    /// <summary>Gets a queryable over the dedicated privileged-action audit log.</summary>
    IQueryable<AuditLogEntry> AuditLogEntries { get; }

    /// <summary>Gets a queryable over the key/value system settings store.</summary>
    IQueryable<SystemSetting> SystemSettings { get; }

    /// <summary>Gets a queryable over rotatable refresh tokens.</summary>
    IQueryable<RefreshToken> RefreshTokens { get; }

    /// <summary>Tracks a new entity for insertion.</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity to add.</param>
    void Add<TEntity>(TEntity entity)
        where TEntity : class;

    /// <summary>Tracks an existing entity for removal (hard delete — only used for lookup/reference tables, never business tables, per R-BE-023).</summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="entity">The entity to remove.</param>
    void Remove<TEntity>(TEntity entity)
        where TEntity : class;

    /// <summary>Persists all tracked changes.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
