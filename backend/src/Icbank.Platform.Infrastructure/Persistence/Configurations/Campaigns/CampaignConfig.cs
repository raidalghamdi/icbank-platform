using Icbank.Platform.Domain.Campaigns;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Campaigns;

/// <summary>EF Core mapping for <see cref="Campaign"/>.</summary>
public sealed class CampaignConfig : IEntityTypeConfiguration<Campaign>
{
    private const int CodeMaxLength = 40;
    private const int NameMaxLength = 300;
    private const int DescriptionMaxLength = 600;
    private const int ObjectiveMaxLength = 600;
    private const int OwnerMaxLength = 150;
    private const int DepartmentMaxLength = 150;
    private const int LatestUpdateMaxLength = 600;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("campaigns");
        builder.ConfigureAuditable();

        builder.Property(c => c.Code).HasColumnName("code").HasMaxLength(CodeMaxLength).IsRequired();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(NameMaxLength).IsRequired();
        builder.Property(c => c.Description).HasColumnName("description").HasMaxLength(DescriptionMaxLength).IsRequired();
        builder.Property(c => c.Objective).HasColumnName("objective").HasMaxLength(ObjectiveMaxLength).IsRequired();
        builder.Property(c => c.Audience).HasColumnName("audience").HasConversion<int>().IsRequired();
        builder.Property(c => c.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(c => c.Owner).HasColumnName("owner").HasMaxLength(OwnerMaxLength).IsRequired();
        builder.Property(c => c.Department).HasColumnName("department").HasMaxLength(DepartmentMaxLength).IsRequired();
        builder.Property(c => c.ProgressPercent).HasColumnName("progress_percent").IsRequired();
        builder.Property(c => c.StartDate).HasColumnName("start_date").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(c => c.EndDate).HasColumnName("end_date").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(c => c.LatestUpdate).HasColumnName("latest_update").HasMaxLength(LatestUpdateMaxLength).IsRequired();
        builder.Property(c => c.ReachCount).HasColumnName("reach_count").IsRequired();
        builder.Property(c => c.ImpressionsCount).HasColumnName("impressions_count").IsRequired();
        builder.Property(c => c.EngagementCount).HasColumnName("engagement_count").IsRequired();
        builder.Property(c => c.PublishedItems).HasColumnName("published_items").IsRequired();
        builder.Property(c => c.SortOrder).HasColumnName("sort_order").IsRequired();
        builder.Property(c => c.IsActive).HasColumnName("is_active").IsRequired();

        builder.HasIndex(c => c.Code).HasDatabaseName("ix_campaigns_code");
        builder.HasIndex(c => new { c.Audience, c.Status, c.SortOrder }).HasDatabaseName("ix_campaigns_audience_status_sort");
    }
}
