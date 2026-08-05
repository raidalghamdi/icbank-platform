using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Identity;

/// <summary>EF Core mapping for <see cref="Page"/> (DATA-MODEL.md section 3.1 <c>pages</c>).</summary>
public sealed class PageConfig : IEntityTypeConfiguration<Page>
{
    private const int SlugMaxLength = 100;
    private const int NameArMaxLength = 200;
    private const int IconMaxLength = 100;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("pages");
        builder.ConfigureAuditable();

        builder.Property(p => p.Slug).HasColumnName("slug").HasMaxLength(SlugMaxLength).IsRequired();
        builder.Property(p => p.NameAr).HasColumnName("name_ar").HasMaxLength(NameArMaxLength).IsRequired();
        builder.Property(p => p.Icon).HasColumnName("icon").HasMaxLength(IconMaxLength);
        builder.Property(p => p.SortOrder).HasColumnName("sort_order").IsRequired();
        builder.Property(p => p.IsActive).HasColumnName("is_active").IsRequired();

        builder.HasIndex(p => p.Slug).IsUnique().HasDatabaseName("ux_pages_slug");
    }
}
