using Icbank.Platform.Domain.MediaMonitoring;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.MediaMonitoring;

/// <summary>EF Core mapping for <see cref="FinalMediaReport"/> (DATA-MODEL.md section 3.7 <c>final_media_reports</c>).</summary>
public sealed class FinalMediaReportConfig : IEntityTypeConfiguration<FinalMediaReport>
{
    private const int ReportNumberMaxLength = 50;
    private const int TitleMaxLength = 300;
    private const int ReportTypeMaxLength = 20;
    private const int PeriodLabelMaxLength = 200;
    private const int NameMaxLength = 300;
    private const int ReferenceNumberMaxLength = 100;
    private const int ClassificationMaxLength = 200;
    private const int StatusMaxLength = 20;
    private const int Sha256MaxLength = 64;
    private const int StorageKeyMaxLength = 500;
    private const int AiModelMaxLength = 100;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FinalMediaReport> builder)
    {
        builder.ToTable("final_media_reports");
        builder.ConfigureAuditable();

        ConfigureColumns(builder);
        ConfigureJsonColumns(builder);
        ConfigureIndexes(builder);
        ConfigureRelationships(builder);

        // Note: ConfigureAuditable() still applies the standard DeletedAt column/query filter
        // for schema consistency (every entity gets the closed audit-column set per the task's
        // rulebook-compliance requirement), even though the application layer should never call
        // SoftDelete() on this table in practice -- immutability is the design intent here.
    }

    /// <summary>Maps the scalar columns of <see cref="FinalMediaReport"/>.</summary>
    private static void ConfigureColumns(EntityTypeBuilder<FinalMediaReport> builder)
    {
        builder.Property(r => r.ReportNumber).HasColumnName("report_number").HasMaxLength(ReportNumberMaxLength).IsRequired();
        builder.Property(r => r.Title).HasColumnName("title").HasMaxLength(TitleMaxLength).IsRequired();
        builder.Property(r => r.ReportType).HasColumnName("report_type").HasMaxLength(ReportTypeMaxLength).HasConversion<string>().IsRequired();
        builder.Property(r => r.PeriodLabel).HasColumnName("period_label").HasMaxLength(PeriodLabelMaxLength).IsRequired();
        builder.Property(r => r.DateFrom).HasColumnName("date_from").HasColumnType("datetimeoffset(3)").IsRequired();
        builder.Property(r => r.DateTo).HasColumnName("date_to").HasColumnType("datetimeoffset(3)").IsRequired();
        builder.Property(r => r.PreparedBy).HasColumnName("prepared_by").HasMaxLength(NameMaxLength);
        builder.Property(r => r.Beneficiary).HasColumnName("beneficiary").HasMaxLength(NameMaxLength);
        builder.Property(r => r.ReferenceNumber).HasColumnName("reference_number").HasMaxLength(ReferenceNumberMaxLength);
        builder.Property(r => r.Classification).HasColumnName("classification").HasMaxLength(ClassificationMaxLength);
        builder.Property(r => r.IssueDate).HasColumnName("issue_date").HasColumnType("datetimeoffset(3)").IsRequired();
        builder.Property(r => r.ExecutiveSummary).HasColumnName("executive_summary").HasColumnType("nvarchar(max)");
        builder.Property(r => r.Methodology).HasColumnName("methodology").HasColumnType("nvarchar(max)");
        builder.Property(r => r.SourceItemsJson).HasColumnName("source_items_json").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(r => r.GeneratedByUserId).HasColumnName("generated_by_user_id");
        builder.Property(r => r.GeneratedByName).HasColumnName("generated_by_name").HasMaxLength(NameMaxLength);
        builder.Property(r => r.AiModel).HasColumnName("ai_model").HasMaxLength(AiModelMaxLength);
        builder.Property(r => r.Status).HasColumnName("status").HasMaxLength(StatusMaxLength).HasConversion<string>().IsRequired();
        builder.Property(r => r.LockedAt).HasColumnName("locked_at").HasColumnType("datetimeoffset(3)").IsRequired();
        builder.Property(r => r.ContentSha256).HasColumnName("content_sha256").HasMaxLength(Sha256MaxLength).IsRequired();
        builder.Property(r => r.PdfStorageKey).HasColumnName("pdf_storage_key").HasMaxLength(StorageKeyMaxLength);
        builder.Property(r => r.ViewCount).HasColumnName("view_count").IsRequired();
    }

    /// <summary>Maps the JSON-backed columns of <see cref="FinalMediaReport"/>.</summary>
    private static void ConfigureJsonColumns(EntityTypeBuilder<FinalMediaReport> builder)
    {
        builder.Property(r => r.Kpis).HasColumnName("kpis_json").HasColumnType("nvarchar(max)")
            .HasConversion(JsonValueConverter.CreateRequired<ReportKpis>()).Metadata.SetValueComparer(JsonValueConverter.CreateRequiredComparer<ReportKpis>());
        builder.Property(r => r.TopNews).HasColumnName("top_news_json").HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<TopNewsItem>()).Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<TopNewsItem>());
        builder.Property(r => r.Timeline).HasColumnName("timeline_json").HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<TimelineEvent>()).Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<TimelineEvent>());
        builder.Property(r => r.DigitalPresence).HasColumnName("digital_presence_json").HasColumnType("nvarchar(max)")
            .HasConversion(JsonValueConverter.CreateRequired<DigitalPresence>()).Metadata.SetValueComparer(JsonValueConverter.CreateRequiredComparer<DigitalPresence>());
        builder.Property(r => r.EditorialTone).HasColumnName("editorial_tone_json").HasColumnType("nvarchar(max)")
            .HasConversion(JsonValueConverter.CreateRequired<EditorialTone>()).Metadata.SetValueComparer(JsonValueConverter.CreateRequiredComparer<EditorialTone>());
        builder.Property(r => r.DeepAnalysis).HasColumnName("deep_analysis_json").HasColumnType("nvarchar(max)")
            .HasConversion(JsonValueConverter.CreateRequired<DeepAnalysis>()).Metadata.SetValueComparer(JsonValueConverter.CreateRequiredComparer<DeepAnalysis>());
        builder.Property(r => r.RegionalComparison).HasColumnName("regional_comparison_json").HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<RegionalComparison>()).Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<RegionalComparison>());
        builder.Property(r => r.Recommendations).HasColumnName("recommendations_json").HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<Recommendation>()).Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<Recommendation>());
        builder.Property(r => r.Alerts).HasColumnName("alerts_json").HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<AlertItem>()).Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<AlertItem>());
        builder.Property(r => r.QuotesAppendix).HasColumnName("quotes_appendix_json").HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<QuoteAppendixItem>()).Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<QuoteAppendixItem>());
        builder.Property(r => r.Sources).HasColumnName("sources_json").HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<SourceRef>()).Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<SourceRef>());
    }

    /// <summary>Declares the secondary indexes for <see cref="FinalMediaReport"/>.</summary>
    private static void ConfigureIndexes(EntityTypeBuilder<FinalMediaReport> builder)
    {
        builder.HasIndex(r => r.ReportNumber).IsUnique().HasDatabaseName("ux_final_media_reports_report_number");
        builder.HasIndex(r => r.GeneratedByUserId).HasDatabaseName("ix_final_media_reports_generated_by_user_id");
    }

    /// <summary>Declares the foreign-key relationships for <see cref="FinalMediaReport"/>.</summary>
    private static void ConfigureRelationships(EntityTypeBuilder<FinalMediaReport> builder)
    {
        // Restrict: DATA-MODEL.md section 4 flags generated_by_user_id as an unenforced implied
        // FK; now enforced. Restrict because this table is immutable/append-only by design --
        // the generating user's removal must never mutate or cascade into a locked report.
        builder.HasOne(r => r.GeneratedByUser).WithMany()
            .HasForeignKey(r => r.GeneratedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
