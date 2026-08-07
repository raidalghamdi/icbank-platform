using Icbank.Platform.Domain.Weekend;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Weekend;

/// <summary>EF Core mapping for <see cref="WeekendDraft"/> (DATA-MODEL.md section 3.10 <c>weekend_drafts</c>).</summary>
public sealed class WeekendDraftConfig : IEntityTypeConfiguration<WeekendDraft>
{
    private const int WeekendDateMaxLength = 20;
    private const int CityMaxLength = 100;
    private const int StatusMaxLength = 20;
    private const int ModelNameMaxLength = 50;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<WeekendDraft> builder)
    {
        builder.ToTable("weekend_drafts");
        builder.ConfigureAuditable();

        builder.Property(d => d.WeekendDate).HasColumnName("weekend_date").HasMaxLength(WeekendDateMaxLength).IsRequired();
        builder.Property(d => d.City).HasColumnName("city").HasMaxLength(CityMaxLength).IsRequired();
        builder.Property(d => d.Status).HasColumnName("status").HasMaxLength(StatusMaxLength).HasConversion<string>().IsRequired();
        builder.Property(d => d.ModelName).HasColumnName("model_name").HasMaxLength(ModelNameMaxLength).IsRequired();
        builder.Property(d => d.ContentJson).HasColumnName("content_json").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(d => d.GeneratedByUserId).HasColumnName("generated_by_user_id");
        builder.Property(d => d.ApprovedByUserId).HasColumnName("approved_by_user_id");
        builder.Property(d => d.RejectedReason).HasColumnName("rejected_reason").HasColumnType("nvarchar(max)");
        builder.Property(d => d.ApprovedAt).HasColumnName("approved_at").HasColumnType("datetimeoffset(3)");
        builder.Property(d => d.PublishedAt).HasColumnName("published_at").HasColumnType("datetimeoffset(3)");

        builder.HasIndex(d => d.WeekendDate).HasDatabaseName("ix_weekend_drafts_weekend_date");
        builder.HasIndex(d => d.Status).HasDatabaseName("ix_weekend_drafts_status");

        // Restrict on both: preserves draft history if the generating/approving user is later
        // deleted -- these are historical workflow references, not live ownership.
        builder.HasOne(d => d.GeneratedByUser).WithMany()
            .HasForeignKey(d => d.GeneratedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(d => d.ApprovedByUser).WithMany()
            .HasForeignKey(d => d.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
