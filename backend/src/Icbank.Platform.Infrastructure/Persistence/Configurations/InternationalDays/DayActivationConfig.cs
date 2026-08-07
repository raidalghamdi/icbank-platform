using Icbank.Platform.Domain.InternationalDays;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.InternationalDays;

/// <summary>EF Core mapping for <see cref="DayActivation"/> (DATA-MODEL.md section 3.6 <c>day_activations</c>).</summary>
public sealed class DayActivationConfig : IEntityTypeConfiguration<DayActivation>
{
    private const int NameMaxLength = 300;
    private const int TypeMaxLength = 100;
    private const int PlatformMaxLength = 100;
    private const int UrlMaxLength = 500;
    private const int CountryMaxLength = 100;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DayActivation> builder)
    {
        builder.ToTable("day_activations");
        builder.ConfigureAuditable();

        builder.Property(a => a.DayId).HasColumnName("day_id").IsRequired();
        builder.Property(a => a.Year).HasColumnName("year");
        builder.Property(a => a.EntityName).HasColumnName("entity_name").HasMaxLength(NameMaxLength);
        builder.Property(a => a.EntityType).HasColumnName("entity_type").HasMaxLength(TypeMaxLength);
        builder.Property(a => a.ActivationType).HasColumnName("activation_type").HasMaxLength(TypeMaxLength);
        builder.Property(a => a.Platform).HasColumnName("platform").HasMaxLength(PlatformMaxLength);
        builder.Property(a => a.Description).HasColumnName("description").HasColumnType("nvarchar(max)");
        builder.Property(a => a.MediaUrl).HasColumnName("media_url").HasMaxLength(UrlMaxLength);
        builder.Property(a => a.SourceUrl).HasColumnName("source_url").HasMaxLength(UrlMaxLength);
        builder.Property(a => a.Country).HasColumnName("country").HasMaxLength(CountryMaxLength);
        builder.Property(a => a.Verified).HasColumnName("verified").IsRequired();

        builder.HasIndex(a => a.DayId).HasDatabaseName("ix_day_activations_day_id");

        // Cascade: matches source .references(..., { onDelete: "cascade" }).
        builder.HasOne(a => a.Day).WithMany(d => d.Activations)
            .HasForeignKey(a => a.DayId).OnDelete(DeleteBehavior.Cascade);
    }
}
