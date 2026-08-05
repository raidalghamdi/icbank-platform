using Icbank.Platform.Domain.AiYear;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.AiYear;

/// <summary>EF Core mapping for <see cref="AiYearActivation"/> (DATA-MODEL.md section 3.2 <c>ai_year_activations</c>).</summary>
public sealed class AiYearActivationConfig : IEntityTypeConfiguration<AiYearActivation>
{
    private const int TitleMaxLength = 300;
    private const int ActivationDateMaxLength = 50;
    private const int TypeMaxLength = 50;
    private const int StatusMaxLength = 20;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AiYearActivation> builder)
    {
        builder.ToTable("ai_year_activations");
        builder.ConfigureAuditable();

        builder.Property(a => a.Title).HasColumnName("title").HasMaxLength(TitleMaxLength).IsRequired();
        builder.Property(a => a.Month).HasColumnName("month").IsRequired();
        builder.Property(a => a.Year).HasColumnName("year").IsRequired();
        builder.Property(a => a.ActivationDate).HasColumnName("activation_date").HasMaxLength(ActivationDateMaxLength);
        builder.Property(a => a.Type).HasColumnName("type").HasMaxLength(TypeMaxLength).IsRequired();
        builder.Property(a => a.Description).HasColumnName("description").HasColumnType("nvarchar(max)");
        builder.Property(a => a.Status).HasColumnName("status").HasMaxLength(StatusMaxLength).HasConversion<string>().IsRequired();
        builder.Property(a => a.Reach).HasColumnName("reach");
        builder.Property(a => a.Engagement).HasColumnName("engagement");
        builder.Property(a => a.Notes).HasColumnName("notes").HasColumnType("nvarchar(max)");

        builder.Property(a => a.Tags)
            .HasColumnName("tags_json")
            .HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<string>())
            .Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<string>());

        builder.HasIndex(a => new { a.Year, a.Month }).HasDatabaseName("ix_ai_year_activations_year_month");
    }
}
