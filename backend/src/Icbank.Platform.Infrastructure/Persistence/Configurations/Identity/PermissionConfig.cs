using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Identity;

/// <summary>EF Core mapping for <see cref="Permission"/> (DATA-MODEL.md section 3.1 <c>permissions</c>).</summary>
public sealed class PermissionConfig : IEntityTypeConfiguration<Permission>
{
    private const int NameMaxLength = 30;
    private const int NameArMaxLength = 100;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.ConfigureAuditable();

        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(NameMaxLength).IsRequired();
        builder.Property(p => p.NameAr).HasColumnName("name_ar").HasMaxLength(NameArMaxLength).IsRequired();

        builder.HasIndex(p => p.Name).IsUnique().HasDatabaseName("ux_permissions_name");
    }
}
