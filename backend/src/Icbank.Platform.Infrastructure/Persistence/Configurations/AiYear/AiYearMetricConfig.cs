using Icbank.Platform.Domain.AiYear;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.AiYear;

/// <summary>EF Core mapping for <see cref="AiYearMetric"/> (DATA-MODEL.md section 3.2 <c>ai_year_metrics</c>).</summary>
public sealed class AiYearMetricConfig : IEntityTypeConfiguration<AiYearMetric>
{
    private const int MetricKeyMaxLength = 100;
    private const int MetricValueMaxLength = 500;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AiYearMetric> builder)
    {
        builder.ToTable("ai_year_metrics");
        builder.ConfigureAuditable();

        builder.Property(m => m.ActivationId).HasColumnName("activation_id").IsRequired();
        builder.Property(m => m.MetricKey).HasColumnName("metric_key").HasMaxLength(MetricKeyMaxLength).IsRequired();
        builder.Property(m => m.MetricValue).HasColumnName("metric_value").HasMaxLength(MetricValueMaxLength);

        builder.HasIndex(m => m.ActivationId).HasDatabaseName("ix_ai_year_metrics_activation_id");

        // Cascade: matches source .references(..., { onDelete: "cascade" }).
        builder.HasOne(m => m.Activation).WithMany(a => a.Metrics)
            .HasForeignKey(m => m.ActivationId).OnDelete(DeleteBehavior.Cascade);
    }
}
