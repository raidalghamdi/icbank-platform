using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Shorfah;

/// <summary>EF Core mapping for <see cref="ShorfahSectionMedia"/> (DATA-MODEL.md section 3.8 <c>shorfah_section_media</c>).</summary>
public sealed class ShorfahSectionMediaConfig : IEntityTypeConfiguration<ShorfahSectionMedia>
{
    private const int MediaUrlMaxLength = 500;
    private const int MediaTypeMaxLength = 10;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ShorfahSectionMedia> builder)
    {
        builder.ToTable("shorfah_section_media");
        builder.ConfigureAuditable();

        builder.Property(m => m.SectionId).HasColumnName("section_id").IsRequired();
        builder.Property(m => m.MediaUrl).HasColumnName("media_url").HasMaxLength(MediaUrlMaxLength).IsRequired();
        builder.Property(m => m.MediaType).HasColumnName("media_type").HasMaxLength(MediaTypeMaxLength).HasConversion<string>().IsRequired();
        builder.Property(m => m.CaptionAr).HasColumnName("caption_ar").HasColumnType("nvarchar(max)");
        builder.Property(m => m.DisplayOrder).HasColumnName("display_order");

        builder.HasIndex(m => m.SectionId).HasDatabaseName("ix_shorfah_section_media_section_id");

        // Cascade: media rows have no meaning once their section is gone.
        builder.HasOne(m => m.Section).WithMany(s => s.Media)
            .HasForeignKey(m => m.SectionId).OnDelete(DeleteBehavior.Cascade);
    }
}
