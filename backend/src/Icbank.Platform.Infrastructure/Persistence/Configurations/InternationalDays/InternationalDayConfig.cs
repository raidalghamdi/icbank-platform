using Icbank.Platform.Domain.InternationalDays;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.InternationalDays;

/// <summary>EF Core mapping for <see cref="InternationalDay"/> (DATA-MODEL.md section 3.6 <c>international_days</c>).</summary>
public sealed class InternationalDayConfig : IEntityTypeConfiguration<InternationalDay>
{
    private const int DayNameMaxLength = 300;
    private const int AnnualDateMaxLength = 50;
    private const int CategoryMaxLength = 100;
    private const int OrganizerMaxLength = 300;
    private const int UrlMaxLength = 500;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<InternationalDay> builder)
    {
        builder.ToTable("international_days");
        builder.ConfigureAuditable();

        builder.Property(d => d.DayNameAr).HasColumnName("day_name_ar").HasMaxLength(DayNameMaxLength).IsRequired();
        builder.Property(d => d.DayNameEn).HasColumnName("day_name_en").HasMaxLength(DayNameMaxLength);
        builder.Property(d => d.AnnualDate).HasColumnName("annual_date").HasMaxLength(AnnualDateMaxLength);
        builder.Property(d => d.Category).HasColumnName("category").HasMaxLength(CategoryMaxLength);
        builder.Property(d => d.OfficialOrganizer).HasColumnName("official_organizer").HasMaxLength(OrganizerMaxLength);
        builder.Property(d => d.OfficialOrganizerSource).HasColumnName("official_organizer_source").HasMaxLength(UrlMaxLength);
        builder.Property(d => d.HistorySummary).HasColumnName("history_summary").HasColumnType("nvarchar(max)");
        builder.Property(d => d.HistorySource).HasColumnName("history_source").HasMaxLength(UrlMaxLength);
        builder.Property(d => d.LastSearchedAt).HasColumnName("last_searched_at").HasColumnType("datetimeoffset(3)");

        builder.Property(d => d.Suggestions)
            .HasColumnName("suggestions_json")
            .HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<string>())
            .Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<string>());

        // Deviation: DATA-MODEL.md section 3.6 flags day_name_ar as heavily ILIKE-searched with
        // no index in the source (also called out in section 7's index recommendations).
        builder.HasIndex(d => d.DayNameAr).HasDatabaseName("ix_international_days_day_name_ar");
    }
}
