using Icbank.Platform.Domain.Weekend;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Weekend;

/// <summary>EF Core mapping for <see cref="GeneratedOutput"/> (DATA-MODEL.md section 3.9 <c>generated_outputs</c>).</summary>
public sealed class GeneratedOutputConfig : IEntityTypeConfiguration<GeneratedOutput>
{
    private const int TopicMaxLength = 300;
    private const int ModelNameMaxLength = 50;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GeneratedOutput> builder)
    {
        builder.ToTable("generated_outputs");
        builder.ConfigureAuditable();

        builder.Property(o => o.Topic).HasColumnName("topic").HasMaxLength(TopicMaxLength).IsRequired();
        builder.Property(o => o.ModelName).HasColumnName("model_name").HasMaxLength(ModelNameMaxLength).IsRequired();
        builder.Property(o => o.OutputText).HasColumnName("output_text").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(o => o.Selected).HasColumnName("selected").IsRequired();

        builder.Property(o => o.ArchiveRefIds)
            .HasColumnName("archive_ref_ids_json")
            .HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<int>())
            .Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<int>());

        builder.HasIndex(o => o.Selected).HasDatabaseName("ix_generated_outputs_selected");
    }
}
