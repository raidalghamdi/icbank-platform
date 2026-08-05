using Icbank.Platform.Domain.InternationalDays;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.InternationalDays;

/// <summary>EF Core mapping for <see cref="DayYearlyTheme"/> (DATA-MODEL.md section 3.6 <c>day_yearly_themes</c>).</summary>
public sealed class DayYearlyThemeConfig : IEntityTypeConfiguration<DayYearlyTheme>
{
    private const int ThemeMaxLength = 400;
    private const int UrlMaxLength = 500;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DayYearlyTheme> builder)
    {
        builder.ToTable("day_yearly_themes");
        builder.ConfigureAuditable();

        builder.Property(t => t.DayId).HasColumnName("day_id").IsRequired();
        builder.Property(t => t.Year).HasColumnName("year").IsRequired();
        builder.Property(t => t.ThemeAr).HasColumnName("theme_ar").HasMaxLength(ThemeMaxLength);
        builder.Property(t => t.ThemeEn).HasColumnName("theme_en").HasMaxLength(ThemeMaxLength);
        builder.Property(t => t.ThemeSourceUrl).HasColumnName("theme_source_url").HasMaxLength(UrlMaxLength);

        // Deviation: DATA-MODEL.md section 3.6 flags a missing unique index on (day_id, year)
        // despite it being a natural key used for select-then-upsert in the source application.
        // Added here to prevent duplicate year rows for the same day.
        builder.HasIndex(t => new { t.DayId, t.Year }).IsUnique().HasDatabaseName("ux_day_yearly_themes_day_year");

        // Cascade: matches source .references(..., { onDelete: "cascade" }).
        builder.HasOne(t => t.Day).WithMany(d => d.YearlyThemes)
            .HasForeignKey(t => t.DayId).OnDelete(DeleteBehavior.Cascade);
    }
}
