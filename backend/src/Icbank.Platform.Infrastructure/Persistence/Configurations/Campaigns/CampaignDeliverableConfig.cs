using Icbank.Platform.Domain.Campaigns;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Campaigns;

/// <summary>EF Core mapping for <see cref="CampaignDeliverable"/>.</summary>
public sealed class CampaignDeliverableConfig : IEntityTypeConfiguration<CampaignDeliverable>
{
    private const int TitleMaxLength = 300;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CampaignDeliverable> builder)
    {
        builder.ToTable("campaign_deliverables");
        builder.ConfigureAuditable();

        builder.Property(d => d.CampaignId).HasColumnName("campaign_id").IsRequired();
        builder.Property(d => d.Title).HasColumnName("title").HasMaxLength(TitleMaxLength).IsRequired();
        builder.Property(d => d.DueDate).HasColumnName("due_date").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(d => d.IsCompleted).HasColumnName("is_completed").IsRequired();
        builder.Property(d => d.SortOrder).HasColumnName("sort_order").IsRequired();

        builder.HasOne(d => d.Campaign)
            .WithMany(c => c.Deliverables)
            .HasForeignKey(d => d.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.CampaignId, d.SortOrder }).HasDatabaseName("ix_campaign_deliverables_campaign_sort");
    }
}
