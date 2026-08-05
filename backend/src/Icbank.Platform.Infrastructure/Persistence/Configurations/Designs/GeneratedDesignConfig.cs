using Icbank.Platform.Domain.Designs;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Designs;

/// <summary>EF Core mapping for <see cref="GeneratedDesign"/> (DATA-MODEL.md section 3.4 <c>generated_designs</c>).</summary>
public sealed class GeneratedDesignConfig : IEntityTypeConfiguration<GeneratedDesign>
{
    private const int UrlMaxLength = 500;
    private const int DepartmentMaxLength = 200;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GeneratedDesign> builder)
    {
        builder.ToTable("generated_designs");
        builder.ConfigureAuditable();

        builder.Property(d => d.TemplateId).HasColumnName("template_id");
        builder.Property(d => d.TitleText).HasColumnName("title_text").HasColumnType("nvarchar(max)");
        builder.Property(d => d.BodyText).HasColumnName("body_text").HasColumnType("nvarchar(max)");
        builder.Property(d => d.BackgroundImageUrl).HasColumnName("background_image_url").HasMaxLength(UrlMaxLength);
        builder.Property(d => d.FinalImageUrl).HasColumnName("final_image_url").HasMaxLength(UrlMaxLength);
        builder.Property(d => d.Department).HasColumnName("department").HasMaxLength(DepartmentMaxLength);
        builder.Property(d => d.CreatedByUserId).HasColumnName("created_by_user_id");

        builder.Property(d => d.SelectedLogoIds)
            .HasColumnName("selected_logo_ids_json")
            .HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<int>())
            .Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<int>());

        builder.HasIndex(d => d.TemplateId).HasDatabaseName("ix_generated_designs_template_id");
        builder.HasIndex(d => d.CreatedByUserId).HasDatabaseName("ix_generated_designs_created_by_user_id");

        // SetNull: matches source .references(..., { onDelete: "set null" }) -- a rendered
        // design remains valid history even if its source template is later removed.
        builder.HasOne(d => d.Template).WithMany(t => t.GeneratedDesigns)
            .HasForeignKey(d => d.TemplateId).OnDelete(DeleteBehavior.SetNull);

        // Restrict: DATA-MODEL.md section 4 flags created_by as an unenforced implied FK; now
        // enforced. Restrict (not Cascade) because a design render is a business record that
        // should survive user deletion for audit purposes -- the FK just becomes orphaned data
        // to be handled by a separate retention policy, not silently cascaded away.
        builder.HasOne(d => d.CreatedByUser).WithMany()
            .HasForeignKey(d => d.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
