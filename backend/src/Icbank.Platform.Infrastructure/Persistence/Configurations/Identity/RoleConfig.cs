using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Identity;

/// <summary>EF Core mapping for <see cref="Role"/> (DATA-MODEL.md section 3.1 <c>roles</c>).</summary>
public sealed class RoleConfig : IEntityTypeConfiguration<Role>
{
    private const int NameMaxLength = 100;
    private const int NameArMaxLength = 200;
    private const int DescriptionMaxLength = 1000;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.ConfigureAuditable();

        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(NameMaxLength).IsRequired();
        builder.Property(r => r.NameAr).HasColumnName("name_ar").HasMaxLength(NameArMaxLength).IsRequired();
        builder.Property(r => r.Description).HasColumnName("description").HasMaxLength(DescriptionMaxLength);
        builder.Property(r => r.IsSystem).HasColumnName("is_system").IsRequired();

        builder.HasIndex(r => r.Name).IsUnique().HasDatabaseName("ux_roles_name");
    }
}
