using Icbank.Platform.Domain.Gac;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Gac;

/// <summary>EF Core mapping for <see cref="GacNewsItem"/> (DATA-MODEL.md section 3.5 <c>gac_news_items</c>).</summary>
public sealed class GacNewsItemConfig : IEntityTypeConfiguration<GacNewsItem>
{
    private const int KindMaxLength = 20;
    private const int TitleMaxLength = 400;
    private const int CategoryMaxLength = 30;

    // Google News RSS links are base64-encoded redirects that routinely run past 1,500
    // characters, so the 500-character cap used elsewhere rejected roughly a third of
    // real GAC coverage. 2,048 is the practical browser/URL ceiling.
    private const int UrlMaxLength = 2048;

    private const int ExternalRefMaxLength = 100;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GacNewsItem> builder)
    {
        builder.ToTable("gac_news_items");
        builder.ConfigureAuditable();

        builder.Property(n => n.Kind).HasColumnName("kind").HasMaxLength(KindMaxLength).HasConversion<string>().IsRequired();
        builder.Property(n => n.TitleAr).HasColumnName("title_ar").HasMaxLength(TitleMaxLength).IsRequired();
        builder.Property(n => n.TitleEn).HasColumnName("title_en").HasMaxLength(TitleMaxLength);
        builder.Property(n => n.BodyAr).HasColumnName("body_ar").HasColumnType("nvarchar(max)");
        builder.Property(n => n.BodyEn).HasColumnName("body_en").HasColumnType("nvarchar(max)");
        builder.Property(n => n.Category).HasColumnName("category").HasMaxLength(CategoryMaxLength).HasConversion<string?>();
        builder.Property(n => n.SourceUrl).HasColumnName("source_url").HasMaxLength(UrlMaxLength);
        builder.Property(n => n.ImageUrl).HasColumnName("image_url").HasMaxLength(UrlMaxLength);
        builder.Property(n => n.PublishedAt).HasColumnName("published_at").HasColumnType("datetimeoffset(3)");
        builder.Property(n => n.ExternalRef).HasColumnName("external_ref").HasMaxLength(ExternalRefMaxLength);

        builder.Property(n => n.Tags)
            .HasColumnName("tags_json")
            .HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<string>())
            .Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<string>());

        builder.HasIndex(n => n.PublishedAt).HasDatabaseName("ix_gac_news_items_published_at");
        builder.HasIndex(n => n.Kind).HasDatabaseName("ix_gac_news_items_kind");
    }
}
