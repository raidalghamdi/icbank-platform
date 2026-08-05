using Icbank.Platform.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.Infrastructure.Persistence;

/// <summary>
/// Provides the mandated replacement for <see cref="DbSet{TEntity}.Remove(TEntity)"/> on business
/// tables (R-BE-023). Business entities are never physically deleted — only flagged — so the
/// global query filter configured in <see cref="AppDbContext"/> hides them going forward.
/// </summary>
public static class SoftDeleteExtensions
{
    /// <summary>Marks <paramref name="entity"/> as deleted without removing the row from the table.</summary>
    /// <typeparam name="T">An auditable, soft-deletable entity type.</typeparam>
    /// <param name="set">The set the entity belongs to (kept for a fluent, <c>DbSet</c>-like call site).</param>
    /// <param name="entity">The entity to soft-delete.</param>
    public static void SoftDelete<T>(this DbSet<T> set, T entity)
        where T : AuditableEntity
    {
        _ = set;
        entity.DeletedAt = DateTime.UtcNow; // Why: R-BE-023 forbids DbSet.Remove on business tables.
    }
}
