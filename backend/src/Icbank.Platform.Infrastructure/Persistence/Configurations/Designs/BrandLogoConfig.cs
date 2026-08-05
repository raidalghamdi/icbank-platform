using Icbank.Platform.Domain.Designs;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Designs;

/// <summary>EF Core mapping for <see cref="BrandLogo"/> (DATA-MODEL.md section 3.4 <c>brand_logos</c>).</summary>
public sealed class BrandLogoConfig : IEntityTypeConfiguration<BrandLogo>
{
    private const int LogoNameMaxLength = 200;
    private const int FileUrlMaxLength = 500;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BrandLogo> builder)
    {
        builder.ToTable("brand_logos");
        builder.ConfigureAuditable();

        builder.Property(l => l.LogoName).HasColumnName("logo_name").HasMaxLength(LogoNameMaxLength).IsRequired();
        builder.Property(l => l.FileUrl).HasColumnName("file_url").HasMaxLength(FileUrlMaxLength).IsRequired();
        builder.Property(l => l.Transparent).HasColumnName("transparent").IsRequired();
        builder.Property(l => l.DefaultWidth).HasColumnName("default_width");
    }
}
