using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Identity;

/// <summary>EF Core mapping for <see cref="UserPageOverride"/> (DATA-MODEL.md section 3.1 <c>user_page_overrides</c>).</summary>
public sealed class UserPageOverrideConfig : IEntityTypeConfiguration<UserPageOverride>
{
    private const int GrantTypeMaxLength = 10;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserPageOverride> builder)
    {
        builder.ToTable("user_page_overrides");
        builder.ConfigureAuditable();

        builder.Property(o => o.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(o => o.PageId).HasColumnName("page_id").IsRequired();
        builder.Property(o => o.PermissionId).HasColumnName("permission_id").IsRequired();
        builder.Property(o => o.GrantType).HasColumnName("grant_type").HasMaxLength(GrantTypeMaxLength)
            .HasConversion<string>().IsRequired();
        builder.Property(o => o.CreatedByUserId).HasColumnName("created_by_user_id");

        // Deviation: DATA-MODEL.md section 3.1 flags no unique index on (user_id, page_id,
        // permission_id) in the source, allowing duplicate override rows. This port adds one --
        // duplicate allow/deny grants for the same trio have no valid business meaning.
        builder.HasIndex(o => new { o.UserId, o.PageId, o.PermissionId })
            .IsUnique()
            .HasDatabaseName("ux_user_page_overrides_user_page_permission");

        builder.HasOne(o => o.User).WithMany(u => u.PageOverrides)
            .HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(o => o.Page).WithMany(p => p.UserPageOverrides)
            .HasForeignKey(o => o.PageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(o => o.Permission).WithMany(p => p.UserPageOverrides)
            .HasForeignKey(o => o.PermissionId).OnDelete(DeleteBehavior.Cascade);

        // Restrict on CreatedByUser: source specifies no onDelete action; avoids a second cascade
        // path to `users` (the primary UserId FK above already cascades).
        builder.HasOne(o => o.CreatedByUser).WithMany()
            .HasForeignKey(o => o.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
