using Icbank.Platform.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Common;

/// <summary>
/// Shared column mapping for the audit/concurrency columns carried by every
/// <see cref="AuditableEntity"/> (R-BE-022, R-BE-026) and the soft-delete query filter
/// (R-BE-023). Called from every entity's <c>IEntityTypeConfiguration&lt;T&gt;</c> so the
/// mapping stays identical across all 43+ tables without copy-pasted column definitions.
/// </summary>
public static class AuditableEntityConfigurationExtensions
{
    private const int ActorIdMaxLength = 100;

    /// <summary>Maps the shared <see cref="AuditableEntity"/> columns and registers the soft-delete query filter.</summary>
    /// <typeparam name="T">The concrete entity type being configured.</typeparam>
    /// <param name="builder">The entity type builder to configure.</param>
    public static void ConfigureAuditable<T>(this EntityTypeBuilder<T> builder)
        where T : AuditableEntity
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedOnAdd();

        builder.Property(entity => entity.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(entity => entity.CreatedBy).HasColumnName("created_by").HasMaxLength(ActorIdMaxLength).IsRequired();
        builder.Property(entity => entity.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime2(3)");
        builder.Property(entity => entity.UpdatedBy).HasColumnName("updated_by").HasMaxLength(ActorIdMaxLength);
        builder.Property(entity => entity.DeletedAt).HasColumnName("deleted_at").HasColumnType("datetime2(3)");

        // Optimistic concurrency -- maps to SQL Server's rowversion column type (conventions doc section 3.11).
        builder.Property(entity => entity.RowVersion).HasColumnName("row_version").IsRowVersion();

        // R-BE-023 -- hides soft-deleted rows automatically. Registered per-entity, not globally
        // (SCAFFOLD-NOTES.md section 1: soft-delete filter is per-entity-config, not global).
        builder.HasQueryFilter(entity => entity.DeletedAt == null);

        builder.HasIndex(entity => entity.DeletedAt).HasDatabaseName($"ix_{typeof(T).Name}_deleted_at".ToLowerInvariant());
    }
}
