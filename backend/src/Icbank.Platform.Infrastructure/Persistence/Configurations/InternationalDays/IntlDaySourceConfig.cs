using Icbank.Platform.Domain.InternationalDays;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.InternationalDays;

/// <summary>EF Core mapping for <see cref="IntlDaySource"/> (DATA-MODEL.md section 3.6 <c>intl_day_sources</c>).</summary>
public sealed class IntlDaySourceConfig : IEntityTypeConfiguration<IntlDaySource>
{
    private const int RelatedTableMaxLength = 100;
    private const int UrlMaxLength = 500;
    private const int TitleMaxLength = 400;
    private const int PublisherMaxLength = 200;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IntlDaySource> builder)
    {
        builder.ToTable("intl_day_sources");
        builder.ConfigureAuditable();

        builder.Property(s => s.RelatedTable).HasColumnName("related_table").HasMaxLength(RelatedTableMaxLength).IsRequired();
        builder.Property(s => s.RelatedId).HasColumnName("related_id").IsRequired();
        builder.Property(s => s.DayId).HasColumnName("day_id");
        builder.Property(s => s.SourceUrl).HasColumnName("source_url").HasMaxLength(UrlMaxLength);
        builder.Property(s => s.SourceTitle).HasColumnName("source_title").HasMaxLength(TitleMaxLength);
        builder.Property(s => s.SourcePublisher).HasColumnName("source_publisher").HasMaxLength(PublisherMaxLength);
        builder.Property(s => s.AccessedAt).HasColumnName("accessed_at").HasColumnType("datetimeoffset(3)").IsRequired();

        builder.HasIndex(s => new { s.RelatedTable, s.RelatedId }).HasDatabaseName("ix_intl_day_sources_related");

        // Restrict + optional: DATA-MODEL.md section 4 recommends keeping this relationship soft
        // since RelatedTable/RelatedId is intentionally polymorphic (only ever targets
        // international_days today, but the discriminator exists to allow other targets later).
        // DayId is a convenience FK populated only when RelatedTable == "international_days";
        // Restrict avoids silently deleting citation history when a day is removed.
        builder.HasOne(s => s.Day).WithMany()
            .HasForeignKey(s => s.DayId).OnDelete(DeleteBehavior.Restrict);
    }
}
