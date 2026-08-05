using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Shorfah;

/// <summary>EF Core mapping for <see cref="ShorfahSectionPermission"/> (DATA-MODEL.md section 3.8 <c>shorfah_section_permissions</c>).</summary>
public sealed class ShorfahSectionPermissionConfig : IEntityTypeConfiguration<ShorfahSectionPermission>
{
    private const int RoleNameMaxLength = 100;
    private const int PermissionMaxLength = 20;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ShorfahSectionPermission> builder)
    {
        builder.ToTable("shorfah_section_permissions");
        builder.ConfigureAuditable();

        builder.Property(p => p.SectionId).HasColumnName("section_id").IsRequired();
        builder.Property(p => p.UserId).HasColumnName("user_id");
        builder.Property(p => p.RoleName).HasColumnName("role_name").HasMaxLength(RoleNameMaxLength);
        builder.Property(p => p.Permission).HasColumnName("permission").HasMaxLength(PermissionMaxLength).HasConversion<string>().IsRequired();

        builder.HasIndex(p => p.SectionId).HasDatabaseName("ix_shorfah_section_permissions_section_id");
        builder.HasIndex(p => p.UserId).HasDatabaseName("ix_shorfah_section_permissions_user_id");

        // Cascade: a permission grant has no meaning once its section is gone.
        builder.HasOne(p => p.Section).WithMany(s => s.Permissions)
            .HasForeignKey(p => p.SectionId).OnDelete(DeleteBehavior.Cascade);

        // Restrict: preserves grant history if the granted user is later removed (role-based
        // grants remain valid regardless), consistent with the Restrict policy used for other
        // user references in this domain.
        builder.HasOne(p => p.User).WithMany()
            .HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
