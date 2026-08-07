using Icbank.Platform.Domain.Weekend;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Weekend;

/// <summary>EF Core mapping for <see cref="WeekendPlace"/> (DATA-MODEL.md section 3.10 <c>weekend_places</c>).</summary>
public sealed class WeekendPlaceConfig : IEntityTypeConfiguration<WeekendPlace>
{
    private const int NameMaxLength = 300;
    private const int ImageUrlMaxLength = 500;
    private const int CityMaxLength = 100;
    private const int MapsQueryMaxLength = 500;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<WeekendPlace> builder)
    {
        builder.ToTable("weekend_places");
        builder.ConfigureAuditable();

        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(NameMaxLength).IsRequired();
        builder.Property(p => p.Description).HasColumnName("description").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(p => p.ImageUrl).HasColumnName("image_url").HasMaxLength(ImageUrlMaxLength);
        builder.Property(p => p.City).HasColumnName("city").HasMaxLength(CityMaxLength).IsRequired();
        builder.Property(p => p.MapsQuery).HasColumnName("maps_query").HasMaxLength(MapsQueryMaxLength);
        builder.Property(p => p.IsActive).HasColumnName("is_active").IsRequired();
        builder.Property(p => p.SortOrder).HasColumnName("sort_order").IsRequired();

        builder.HasIndex(p => p.IsActive).HasDatabaseName("ix_weekend_places_is_active");
    }
}
