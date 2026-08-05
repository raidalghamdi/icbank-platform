using Icbank.Platform.Domain.MediaMonitoring;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.MediaMonitoring;

/// <summary>EF Core mapping for <see cref="MediaReport"/> (DATA-MODEL.md section 3.7 <c>media_reports</c>).</summary>
public sealed class MediaReportConfig : IEntityTypeConfiguration<MediaReport>
{
    private const int TitleMaxLength = 300;
    private const int ReportTypeMaxLength = 20;
    private const int AudienceMaxLength = 20;
    private const int ToneMaxLength = 100;
    private const int NameMaxLength = 200;
    private const int AiModelMaxLength = 100;
    private const int StatusMaxLength = 20;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MediaReport> builder)
    {
        builder.ToTable("media_reports");
        builder.ConfigureAuditable();

        ConfigureColumns(builder);
        ConfigureJsonColumns(builder);
        ConfigureIndexes(builder);
        ConfigureRelationships(builder);
    }

    /// <summary>Maps the scalar columns of <see cref="MediaReport"/>.</summary>
    private static void ConfigureColumns(EntityTypeBuilder<MediaReport> builder)
    {
        builder.Property(r => r.Title).HasColumnName("title").HasMaxLength(TitleMaxLength).IsRequired();
        builder.Property(r => r.ReportType).HasColumnName("report_type").HasMaxLength(ReportTypeMaxLength).HasConversion<string>().IsRequired();
        builder.Property(r => r.Audience).HasColumnName("audience").HasMaxLength(AudienceMaxLength).HasConversion<string>().IsRequired();
        builder.Property(r => r.DateFrom).HasColumnName("date_from").HasColumnType("datetimeoffset(3)").IsRequired();
        builder.Property(r => r.DateTo).HasColumnName("date_to").HasColumnType("datetimeoffset(3)").IsRequired();
        builder.Property(r => r.ExecutiveSummary).HasColumnName("executive_summary").HasColumnType("nvarchar(max)");
        builder.Property(r => r.ContentMd).HasColumnName("content_md").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(r => r.OverallTone).HasColumnName("overall_tone").HasMaxLength(ToneMaxLength);
        builder.Property(r => r.SourceItemsJson).HasColumnName("source_items_json").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(r => r.GeneratedByUserId).HasColumnName("generated_by_user_id");
        builder.Property(r => r.GeneratedByName).HasColumnName("generated_by_name").HasMaxLength(NameMaxLength);
        builder.Property(r => r.AiModel).HasColumnName("ai_model").HasMaxLength(AiModelMaxLength);
        builder.Property(r => r.Status).HasColumnName("status").HasMaxLength(StatusMaxLength).HasConversion<string>().IsRequired();
    }

    /// <summary>Maps the JSON-backed columns of <see cref="MediaReport"/>.</summary>
    private static void ConfigureJsonColumns(EntityTypeBuilder<MediaReport> builder)
    {
        builder.Property(r => r.Sources)
            .HasColumnName("sources_json")
            .HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<string>())
            .Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<string>());

        builder.Property(r => r.Stats)
            .HasColumnName("stats_json")
            .HasColumnType("nvarchar(max)")
            .HasConversion(JsonValueConverter.Create<MediaReportStats>())
            .Metadata.SetValueComparer(JsonValueConverter.CreateComparer<MediaReportStats>());
    }

    /// <summary>Declares the secondary indexes for <see cref="MediaReport"/>.</summary>
    private static void ConfigureIndexes(EntityTypeBuilder<MediaReport> builder)
    {
        builder.HasIndex(r => r.GeneratedByUserId).HasDatabaseName("ix_media_reports_generated_by_user_id");
        builder.HasIndex(r => new { r.DateFrom, r.DateTo }).HasDatabaseName("ix_media_reports_date_range");
    }

    /// <summary>Declares the foreign-key relationships for <see cref="MediaReport"/>.</summary>
    private static void ConfigureRelationships(EntityTypeBuilder<MediaReport> builder)
    {
        // Restrict: DATA-MODEL.md section 4 flags generated_by_user_id as an unenforced implied
        // FK; now enforced. Restrict preserves the report (a business record) if the generating
        // user is later deleted -- the denormalized GeneratedByName already preserves the
        // display name independently, by design.
        builder.HasOne(r => r.GeneratedByUser).WithMany()
            .HasForeignKey(r => r.GeneratedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
