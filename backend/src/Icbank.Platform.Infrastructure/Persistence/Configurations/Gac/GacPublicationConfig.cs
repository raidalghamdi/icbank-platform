using Icbank.Platform.Domain.Gac;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Gac;

/// <summary>EF Core mapping for <see cref="GacPublication"/> (DATA-MODEL.md section 3.5 <c>gac_publications</c>).</summary>
public sealed class GacPublicationConfig : IEntityTypeConfiguration<GacPublication>
{
    private const int TitleMaxLength = 400;
    private const int VersionMaxLength = 50;
    private const int UrlMaxLength = 500;
    private const int CategoryMaxLength = 30;
    private const int LanguageMaxLength = 10;
    private const int SourceDomainMaxLength = 20;
    private const int StatusMaxLength = 20;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GacPublication> builder)
    {
        builder.ToTable("gac_publications");
        builder.ConfigureAuditable();

        builder.Property(p => p.TitleAr).HasColumnName("title_ar").HasMaxLength(TitleMaxLength).IsRequired();
        builder.Property(p => p.TitleEn).HasColumnName("title_en").HasMaxLength(TitleMaxLength);
        builder.Property(p => p.Category).HasColumnName("category").HasMaxLength(CategoryMaxLength).HasConversion<string>().IsRequired();
        builder.Property(p => p.Language).HasColumnName("language").HasMaxLength(LanguageMaxLength).HasConversion<string>().IsRequired();
        builder.Property(p => p.DescriptionAr).HasColumnName("description_ar").HasColumnType("nvarchar(max)");
        builder.Property(p => p.DescriptionEn).HasColumnName("description_en").HasColumnType("nvarchar(max)");
        builder.Property(p => p.Version).HasColumnName("version").HasMaxLength(VersionMaxLength);
        builder.Property(p => p.PublishedAt).HasColumnName("published_at").HasColumnType("datetimeoffset(3)");
        builder.Property(p => p.OriginalUrl).HasColumnName("original_url").HasMaxLength(UrlMaxLength);
        builder.Property(p => p.FileUrl).HasColumnName("file_url").HasMaxLength(UrlMaxLength).IsRequired();
        builder.Property(p => p.FileSizeBytes).HasColumnName("file_size_bytes");
        builder.Property(p => p.PageCount).HasColumnName("page_count");
        builder.Property(p => p.SourceDomain).HasColumnName("source_domain").HasMaxLength(SourceDomainMaxLength).HasConversion<string>().IsRequired();
        builder.Property(p => p.Status).HasColumnName("status").HasMaxLength(StatusMaxLength).HasConversion<string>().IsRequired();
        builder.Property(p => p.DisplayOrder).HasColumnName("display_order").IsRequired();

        builder.Property(p => p.Tags)
            .HasColumnName("tags_json")
            .HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<string>())
            .Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<string>());

        builder.HasIndex(p => p.Category).HasDatabaseName("ix_gac_publications_category");
        builder.HasIndex(p => p.Status).HasDatabaseName("ix_gac_publications_status");
        builder.HasIndex(p => p.DisplayOrder).HasDatabaseName("ix_gac_publications_display_order");
    }
}
