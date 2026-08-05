using Icbank.Platform.Domain.MediaMonitoring;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.MediaMonitoring;

/// <summary>EF Core mapping for <see cref="PromptFramework"/> (DATA-MODEL.md section 3.7 <c>prompt_frameworks</c>).</summary>
public sealed class PromptFrameworkConfig : IEntityTypeConfiguration<PromptFramework>
{
    private const int NameMaxLength = 200;
    private const int CategoryMaxLength = 30;
    private const int KindMaxLength = 20;
    private const int ModelMaxLength = 100;
    private const int StatusMaxLength = 20;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PromptFramework> builder)
    {
        builder.ToTable("prompt_frameworks");
        builder.ConfigureAuditable();

        builder.Property(f => f.NameAr).HasColumnName("name_ar").HasMaxLength(NameMaxLength).IsRequired();
        builder.Property(f => f.NameEn).HasColumnName("name_en").HasMaxLength(NameMaxLength);
        builder.Property(f => f.DescriptionAr).HasColumnName("description_ar").HasColumnType("nvarchar(max)");
        builder.Property(f => f.Category).HasColumnName("category").HasMaxLength(CategoryMaxLength).HasConversion<string>().IsRequired();
        builder.Property(f => f.Kind).HasColumnName("kind").HasMaxLength(KindMaxLength).HasConversion<string>().IsRequired();
        builder.Property(f => f.PromptText).HasColumnName("prompt_text").HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(f => f.ExampleInput).HasColumnName("example_input").HasColumnType("nvarchar(max)");
        builder.Property(f => f.ExampleOutput).HasColumnName("example_output").HasColumnType("nvarchar(max)");
        builder.Property(f => f.RecommendedModel).HasColumnName("recommended_model").HasMaxLength(ModelMaxLength);
        builder.Property(f => f.IsApproved).HasColumnName("is_approved").IsRequired();
        builder.Property(f => f.UsageCount).HasColumnName("usage_count").IsRequired();
        builder.Property(f => f.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(f => f.CreatedByName).HasColumnName("created_by_name").HasMaxLength(NameMaxLength);
        builder.Property(f => f.Status).HasColumnName("status").HasMaxLength(StatusMaxLength).HasConversion<string>().IsRequired();

        builder.Property(f => f.Variables)
            .HasColumnName("variables_json")
            .HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<PromptVariable>())
            .Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<PromptVariable>());

        builder.Property(f => f.Tags)
            .HasColumnName("tags_json")
            .HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<string>())
            .Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<string>());

        builder.HasIndex(f => f.CreatedByUserId).HasDatabaseName("ix_prompt_frameworks_created_by_user_id");
        builder.HasIndex(f => f.Category).HasDatabaseName("ix_prompt_frameworks_category");

        // Restrict: DATA-MODEL.md section 4 flags created_by_user_id as an unenforced implied
        // FK; now enforced. Restrict preserves the framework (reusable product IP) if the
        // creating user is later deleted.
        builder.HasOne(f => f.CreatedByUser).WithMany()
            .HasForeignKey(f => f.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
