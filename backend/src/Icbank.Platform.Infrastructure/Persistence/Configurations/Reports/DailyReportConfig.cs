using Icbank.Platform.Domain.Reports;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Reports;

/// <summary>EF Core mapping for <see cref="DailyReport"/> (DATA-MODEL.md section 3.3 <c>daily_reports</c>).</summary>
public sealed class DailyReportConfig : IEntityTypeConfiguration<DailyReport>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DailyReport> builder)
    {
        builder.ToTable("daily_reports");
        builder.ConfigureAuditable();

        builder.Property(r => r.ReportDate).HasColumnName("report_date").HasColumnType("date").IsRequired();
        builder.Property(r => r.ReportDataJson).HasColumnName("report_data_json").HasColumnType("nvarchar(max)").IsRequired();

        // Deviation: DATA-MODEL.md section 3.3 flags report_date as an "implied UNIQUE" with no
        // actual database constraint, allowing duplicate rows for the same date under concurrent
        // POSTs. This port adds a real unique index to close that data-integrity gap.
        builder.HasIndex(r => r.ReportDate).IsUnique().HasDatabaseName("ux_daily_reports_report_date");
    }
}
