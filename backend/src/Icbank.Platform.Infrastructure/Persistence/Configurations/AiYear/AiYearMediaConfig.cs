using Icbank.Platform.Domain.AiYear;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.AiYear;

/// <summary>EF Core mapping for <see cref="AiYearMedia"/> (DATA-MODEL.md section 3.2 <c>ai_year_media</c>).</summary>
public sealed class AiYearMediaConfig : IEntityTypeConfiguration<AiYearMedia>
{
    private const int ObjectPathMaxLength = 500;
    private const int FileNameMaxLength = 260;
    private const int ContentTypeMaxLength = 100;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AiYearMedia> builder)
    {
        builder.ToTable("ai_year_media");
        builder.ConfigureAuditable();

        builder.Property(m => m.ActivationId).HasColumnName("activation_id").IsRequired();
        builder.Property(m => m.ObjectPath).HasColumnName("object_path").HasMaxLength(ObjectPathMaxLength).IsRequired();
        builder.Property(m => m.FileName).HasColumnName("file_name").HasMaxLength(FileNameMaxLength);
        builder.Property(m => m.ContentType).HasColumnName("content_type").HasMaxLength(ContentTypeMaxLength);
        builder.Property(m => m.SortOrder).HasColumnName("sort_order").IsRequired();

        builder.HasIndex(m => m.ActivationId).HasDatabaseName("ix_ai_year_media_activation_id");

        // Cascade: matches source .references(..., { onDelete: "cascade" }).
        builder.HasOne(m => m.Activation).WithMany(a => a.Media)
            .HasForeignKey(m => m.ActivationId).OnDelete(DeleteBehavior.Cascade);
    }
}
