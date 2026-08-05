using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Identity;

/// <summary>EF Core mapping for <see cref="RolePermission"/> (DATA-MODEL.md section 3.1 <c>role_permissions</c>).</summary>
public sealed class RolePermissionConfig : IEntityTypeConfiguration<RolePermission>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.ConfigureAuditable();

        builder.Property(rp => rp.RoleId).HasColumnName("role_id").IsRequired();
        builder.Property(rp => rp.PageId).HasColumnName("page_id").IsRequired();
        builder.Property(rp => rp.PermissionId).HasColumnName("permission_id").IsRequired();

        // Source: role_page_perm_idx UNIQUE(role_id, page_id, permission_id).
        builder.HasIndex(rp => new { rp.RoleId, rp.PageId, rp.PermissionId })
            .IsUnique()
            .HasDatabaseName("role_page_perm_idx");

        // Cascade: matches source .references(..., { onDelete: "cascade" }) exactly for all three FKs --
        // a grant row is meaningless once its role/page/permission is gone.
        builder.HasOne(rp => rp.Role).WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(rp => rp.Page).WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(rp => rp.Permission).WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId).OnDelete(DeleteBehavior.Cascade);
    }
}
