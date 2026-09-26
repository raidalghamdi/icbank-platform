using Icbank.Platform.Domain.Campaigns;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Campaigns;

/// <summary>EF Core mapping for <see cref="CampaignRequest"/> (table <c>campaign_requests</c>).</summary>
public sealed class CampaignRequestConfig : IEntityTypeConfiguration<CampaignRequest>
{
    private const int NameMaxLength = 300;
    private const int TextMaxLength = 600;
    private const int ListMaxLength = 4000;
    private const int ShortMaxLength = 150;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CampaignRequest> builder)
    {
        builder.ToTable("campaign_requests");
        builder.ConfigureAuditable();

        builder.Property(r => r.RequestingDepartment).HasColumnName("requesting_department").HasMaxLength(ShortMaxLength).IsRequired();
        builder.Property(r => r.Name).HasColumnName("name").HasMaxLength(NameMaxLength).IsRequired();
        builder.Property(r => r.Objective).HasColumnName("objective").HasMaxLength(TextMaxLength).IsRequired();
        builder.Property(r => r.TargetAudience).HasColumnName("target_audience").HasMaxLength(TextMaxLength).IsRequired();
        builder.Property(r => r.ProposedStart).HasColumnName("proposed_start").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(r => r.ProposedEnd).HasColumnName("proposed_end").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(r => r.KeyMessages).HasColumnName("key_messages").HasMaxLength(ListMaxLength).IsRequired();
        builder.Property(r => r.SupportTypes).HasColumnName("support_types").HasMaxLength(ShortMaxLength).IsRequired();
        builder.Property(r => r.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(r => r.SubmittedByUserId).HasColumnName("submitted_by_user_id").IsRequired();
        builder.Property(r => r.SubmittedByName).HasColumnName("submitted_by_name").HasMaxLength(ShortMaxLength).IsRequired();
        builder.Property(r => r.ReviewNote).HasColumnName("review_note").HasMaxLength(TextMaxLength).IsRequired();
        builder.Property(r => r.ReviewedByName).HasColumnName("reviewed_by_name").HasMaxLength(ShortMaxLength).IsRequired();
        builder.Property(r => r.ReviewedAt).HasColumnName("reviewed_at").HasColumnType("datetime2(3)");
        builder.Property(r => r.CampaignId).HasColumnName("campaign_id");

        builder.HasIndex(r => new { r.Status, r.CreatedAt }).HasDatabaseName("ix_campaign_requests_status_created");
        builder.HasIndex(r => r.SubmittedByUserId).HasDatabaseName("ix_campaign_requests_submitted_by");
    }
}
