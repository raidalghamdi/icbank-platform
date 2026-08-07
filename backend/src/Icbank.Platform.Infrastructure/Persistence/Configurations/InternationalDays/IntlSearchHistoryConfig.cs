using Icbank.Platform.Domain.InternationalDays;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.InternationalDays;

/// <summary>EF Core mapping for <see cref="IntlSearchHistory"/> (DATA-MODEL.md section 3.6 <c>intl_search_history</c>).</summary>
public sealed class IntlSearchHistoryConfig : IEntityTypeConfiguration<IntlSearchHistory>
{
    private const int QueryMaxLength = 500;
    private const int IpAddressMaxLength = 45;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<IntlSearchHistory> builder)
    {
        builder.ToTable("intl_search_history");
        builder.ConfigureAuditable();

        builder.Property(h => h.Query).HasColumnName("query").HasMaxLength(QueryMaxLength).IsRequired();
        builder.Property(h => h.DayId).HasColumnName("day_id");
        builder.Property(h => h.IpAddress).HasColumnName("ip_address").HasMaxLength(IpAddressMaxLength);
        builder.Property(h => h.SearchedAt).HasColumnName("searched_at").HasColumnType("datetimeoffset(3)").IsRequired();

        builder.HasIndex(h => h.DayId).HasDatabaseName("ix_intl_search_history_day_id");
        builder.HasIndex(h => h.SearchedAt).HasDatabaseName("ix_intl_search_history_searched_at");

        // Restrict: DATA-MODEL.md section 4 flags day_id as an unenforced implied FK; now
        // enforced. Restrict (not Cascade) preserves the audit/rate-limiting log even if the
        // referenced day is later deleted.
        builder.HasOne(h => h.Day).WithMany()
            .HasForeignKey(h => h.DayId).OnDelete(DeleteBehavior.Restrict);
    }
}
