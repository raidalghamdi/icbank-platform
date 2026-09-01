using Icbank.Platform.Domain.Campaigns;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Campaigns;

/// <summary>EF Core mapping for <see cref="CampaignChannel"/>.</summary>
public sealed class CampaignChannelConfig : IEntityTypeConfiguration<CampaignChannel>
{
    private const int NameMaxLength = 120;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CampaignChannel> builder)
    {
        builder.ToTable("campaign_channels");
        builder.ConfigureAuditable();

        builder.Property(c => c.CampaignId).HasColumnName("campaign_id").IsRequired();
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(NameMaxLength).IsRequired();
        builder.Property(c => c.PublishedItems).HasColumnName("published_items").IsRequired();
        builder.Property(c => c.ReachCount).HasColumnName("reach_count").IsRequired();
        builder.Property(c => c.EngagementCount).HasColumnName("engagement_count").IsRequired();
        builder.Property(c => c.SortOrder).HasColumnName("sort_order").IsRequired();

        builder.HasOne(c => c.Campaign)
            .WithMany(campaign => campaign.Channels)
            .HasForeignKey(c => c.CampaignId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.CampaignId, c.SortOrder }).HasDatabaseName("ix_campaign_channels_campaign_sort");
    }
}
