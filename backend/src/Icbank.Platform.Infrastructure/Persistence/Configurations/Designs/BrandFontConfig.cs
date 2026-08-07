using Icbank.Platform.Domain.Designs;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Designs;

/// <summary>EF Core mapping for <see cref="BrandFont"/> (DATA-MODEL.md section 3.4 <c>brand_fonts</c>).</summary>
public sealed class BrandFontConfig : IEntityTypeConfiguration<BrandFont>
{
    private const int FontNameMaxLength = 200;
    private const int FontFileUrlMaxLength = 500;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BrandFont> builder)
    {
        builder.ToTable("brand_fonts");
        builder.ConfigureAuditable();

        builder.Property(f => f.FontName).HasColumnName("font_name").HasMaxLength(FontNameMaxLength).IsRequired();
        builder.Property(f => f.FontFileUrl).HasColumnName("font_file_url").HasMaxLength(FontFileUrlMaxLength).IsRequired();
        builder.Property(f => f.IsDefault).HasColumnName("is_default").IsRequired();

        // Deviation: DATA-MODEL.md section 3.4 (DATA-01) flags that only application code
        // (an unconditional, non-transactional UPDATE) enforces "at most one default font" in
        // the source system. This port adds a filtered unique index so SQL Server itself
        // rejects a second default row, closing the race-condition gap called out there.
        builder.HasIndex(f => f.IsDefault)
            .IsUnique()
            .HasFilter("[is_default] = 1")
            .HasDatabaseName("ux_brand_fonts_single_default");
    }
}
