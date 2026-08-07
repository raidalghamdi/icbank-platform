using Icbank.Platform.Domain.MediaMonitoring;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.MediaMonitoring;

/// <summary>EF Core mapping for <see cref="ReportsQaQuery"/> (DATA-MODEL.md section 3.7 <c>reports_qa_queries</c>).</summary>
public sealed class ReportsQaQueryConfig : IEntityTypeConfiguration<ReportsQaQuery>
{
    private const int NameMaxLength = 200;
    private const int QueryTypeMaxLength = 20;
    private const int SearchQueryMaxLength = 500;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ReportsQaQuery> builder)
    {
        builder.ToTable("reports_qa_queries");
        builder.ConfigureAuditable();

        builder.Property(q => q.UserId).HasColumnName("user_id");
        builder.Property(q => q.UserName).HasColumnName("user_name").HasMaxLength(NameMaxLength);
        builder.Property(q => q.QueryType).HasColumnName("query_type").HasMaxLength(QueryTypeMaxLength).HasConversion<string>().IsRequired();
        builder.Property(q => q.SearchQuery).HasColumnName("search_query").HasMaxLength(SearchQueryMaxLength);
        builder.Property(q => q.FinalReportId).HasColumnName("final_report_id");
        builder.Property(q => q.ResultSummary).HasColumnName("result_summary").HasColumnType("nvarchar(max)");
        builder.Property(q => q.MetadataJson).HasColumnName("metadata_json").HasColumnType("nvarchar(max)");

        builder.Property(q => q.WizardAnswers)
            .HasColumnName("wizard_answers_json")
            .HasColumnType("nvarchar(max)")
            .HasConversion(JsonValueConverter.Create<WizardAnswers>())
            .Metadata.SetValueComparer(JsonValueConverter.CreateComparer<WizardAnswers>());

        builder.HasIndex(q => q.UserId).HasDatabaseName("ix_reports_qa_queries_user_id");
        builder.HasIndex(q => q.FinalReportId).HasDatabaseName("ix_reports_qa_queries_final_report_id");

        // Restrict on both: DATA-MODEL.md section 4 flags user_id and final_report_id as
        // unenforced implied FKs; both now enforced. Restrict keeps this audit log intact even
        // if the user or the (immutable) final report row is ever removed.
        builder.HasOne(q => q.User).WithMany()
            .HasForeignKey(q => q.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(q => q.FinalReport).WithMany(r => r.QaQueries)
            .HasForeignKey(q => q.FinalReportId).OnDelete(DeleteBehavior.Restrict);
    }
}
