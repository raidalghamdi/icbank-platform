using Icbank.Platform.Domain.Shorfah;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Shorfah;

/// <summary>
/// EF Core mapping for <see cref="ShorfahSectionSlaDefault"/> (DATA-MODEL.md section 3.8
/// <c>shorfah_section_sla_defaults</c>). Configured by hand rather than via
/// <c>ConfigureAuditable</c> because this entity intentionally keeps the source's natural-key
/// primary key (<see cref="ShorfahSectionType"/>) instead of an int surrogate -- see the entity's
/// remarks and DOMAIN-PORT-NOTES.md.
/// </summary>
public sealed class ShorfahSectionSlaDefaultConfig : IEntityTypeConfiguration<ShorfahSectionSlaDefault>
{
    private const int SectionTypeMaxLength = 30;
    private const int ActorIdMaxLength = 100;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ShorfahSectionSlaDefault> builder)
    {
        builder.ToTable("shorfah_section_sla_defaults");

        builder.HasKey(d => d.SectionType);
        builder.Property(d => d.SectionType).HasColumnName("section_type").HasMaxLength(SectionTypeMaxLength).HasConversion<string>();
        builder.Property(d => d.SlaDays).HasColumnName("sla_days").IsRequired();

        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasColumnType("datetime2(3)").IsRequired();
        builder.Property(d => d.CreatedBy).HasColumnName("created_by").HasMaxLength(ActorIdMaxLength).IsRequired();
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime2(3)");
        builder.Property(d => d.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.Property(d => d.RowVersion).HasColumnName("row_version").IsRowVersion();

        // Restrict: preserves the SLA-default config row if the updating user is later removed.
        builder.HasOne(d => d.UpdatedByUser).WithMany()
            .HasForeignKey(d => d.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
