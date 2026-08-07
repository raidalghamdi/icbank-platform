using Icbank.Platform.Domain.Weekend;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Weekend;

/// <summary>EF Core mapping for <see cref="StyleProfile"/> (DATA-MODEL.md section 3.9 <c>style_profile</c>).</summary>
public sealed class StyleProfileConfig : IEntityTypeConfiguration<StyleProfile>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<StyleProfile> builder)
    {
        builder.ToTable("style_profile");
        builder.ConfigureAuditable();

        builder.Property(p => p.ToneSummary).HasColumnName("tone_summary").HasColumnType("nvarchar(max)");
        builder.Property(p => p.AvgParagraphLength).HasColumnName("avg_paragraph_length").HasColumnType("real");

        // Source is unbounded text and the live rehearsal contains values longer than the
        // former 20-character limit; preserve the complete source expression.
        builder.Property(p => p.QuoteUsage).HasColumnName("quote_usage").HasColumnType("nvarchar(max)");

        builder.Property(p => p.OpenerPatterns).HasColumnName("opener_patterns_json").HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<string>()).Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<string>());
        builder.Property(p => p.CloserPatterns).HasColumnName("closer_patterns_json").HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<string>()).Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<string>());
        builder.Property(p => p.RecurringKeywords).HasColumnName("recurring_keywords_json").HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<string>()).Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<string>());
    }
}
