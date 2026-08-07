using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Identity;

/// <summary>EF Core mapping for <see cref="UserRole"/> (DATA-MODEL.md section 3.1 <c>user_roles</c>).</summary>
public sealed class UserRoleConfig : IEntityTypeConfiguration<UserRole>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");
        builder.ConfigureAuditable();

        builder.Property(ur => ur.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(ur => ur.RoleId).HasColumnName("role_id").IsRequired();
        builder.Property(ur => ur.AssignedById).HasColumnName("assigned_by");
        builder.Property(ur => ur.AssignedAt).HasColumnName("assigned_at").HasColumnType("datetime2(3)").IsRequired();

        // Source: user_role_idx UNIQUE(user_id, role_id).
        builder.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique().HasDatabaseName("user_role_idx");

        // Cascade on User/Role: matches source .references(..., { onDelete: "cascade" }).
        builder.HasOne(ur => ur.User).WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(ur => ur.Role).WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId).OnDelete(DeleteBehavior.Cascade);

        // Restrict on AssignedBy: source specifies no onDelete action (defaults to Postgres NO ACTION).
        // Restrict is the closest SQL Server equivalent and avoids the multiple-cascade-path error
        // that a second Cascade path to `users` would trigger via UserId above.
        builder.HasOne(ur => ur.AssignedBy).WithMany()
            .HasForeignKey(ur => ur.AssignedById).OnDelete(DeleteBehavior.Restrict);
    }
}
