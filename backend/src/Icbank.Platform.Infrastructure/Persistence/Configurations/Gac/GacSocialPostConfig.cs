using Icbank.Platform.Domain.Gac;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Gac;

/// <summary>EF Core mapping for <see cref="GacSocialPost"/> (DATA-MODEL.md section 3.5 <c>gac_social_posts</c>).</summary>
public sealed class GacSocialPostConfig : IEntityTypeConfiguration<GacSocialPost>
{
    private const int PlatformMaxLength = 20;
    private const int ExternalIdMaxLength = 200;
    private const int UrlMaxLength = 500;
    private const int MediaTypeMaxLength = 10;
    private const int AccountMaxLength = 100;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GacSocialPost> builder)
    {
        builder.ToTable("gac_social_posts");
        builder.ConfigureAuditable();

        builder.Property(p => p.Platform).HasColumnName("platform").HasMaxLength(PlatformMaxLength).HasConversion<string>().IsRequired();
        builder.Property(p => p.ExternalId).HasColumnName("external_id").HasMaxLength(ExternalIdMaxLength).IsRequired();
        builder.Property(p => p.ContentAr).HasColumnName("content_ar").HasColumnType("nvarchar(max)");
        builder.Property(p => p.ContentEn).HasColumnName("content_en").HasColumnType("nvarchar(max)");
        builder.Property(p => p.PostUrl).HasColumnName("post_url").HasMaxLength(UrlMaxLength).IsRequired();
        builder.Property(p => p.MediaUrl).HasColumnName("media_url").HasMaxLength(UrlMaxLength);
        builder.Property(p => p.MediaType).HasColumnName("media_type").HasMaxLength(MediaTypeMaxLength).HasConversion<string>().IsRequired();
        builder.Property(p => p.PostedAt).HasColumnName("posted_at").HasColumnType("datetimeoffset(3)");
        builder.Property(p => p.Account).HasColumnName("account").HasMaxLength(AccountMaxLength).IsRequired();

        builder.Property(p => p.Metrics)
            .HasColumnName("metrics_json")
            .HasColumnType("nvarchar(max)")
            .HasConversion(JsonValueConverter.Create<SocialMetrics>())
            .Metadata.SetValueComparer(JsonValueConverter.CreateComparer<SocialMetrics>());

        // Deviation: DATA-MODEL.md section 3.5 (AMBIGUOUS-7) flags a schema *comment* claiming
        // UNIQUE(platform, external_id) that was never actually implemented as a database
        // constraint. This port adds the real constraint the comment always claimed to have --
        // flagged for product confirmation in DOMAIN-PORT-NOTES.md in case duplicate ingestion
        // is silently relied upon today.
        builder.HasIndex(p => new { p.Platform, p.ExternalId }).IsUnique().HasDatabaseName("ux_gac_social_posts_platform_external_id");
        builder.HasIndex(p => p.PostedAt).HasDatabaseName("ix_gac_social_posts_posted_at");
    }
}
