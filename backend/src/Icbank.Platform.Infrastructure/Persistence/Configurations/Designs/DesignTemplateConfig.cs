using Icbank.Platform.Domain.Designs;
using Icbank.Platform.Infrastructure.Persistence.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Icbank.Platform.Infrastructure.Persistence.Configurations.Designs;

/// <summary>EF Core mapping for <see cref="DesignTemplate"/> (DATA-MODEL.md section 3.4 <c>design_templates</c>).</summary>
public sealed class DesignTemplateConfig : IEntityTypeConfiguration<DesignTemplate>
{
    private const int TemplateNameArMaxLength = 200;
    private const int CategoryMaxLength = 100;
    private const int ThumbnailUrlMaxLength = 500;

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DesignTemplate> builder)
    {
        builder.ToTable("design_templates");
        builder.ConfigureAuditable();

        builder.Property(t => t.TemplateNameAr).HasColumnName("template_name_ar").HasMaxLength(TemplateNameArMaxLength).IsRequired();
        builder.Property(t => t.Category).HasColumnName("category").HasMaxLength(CategoryMaxLength).IsRequired();
        builder.Property(t => t.CanvasWidth).HasColumnName("canvas_width").IsRequired();
        builder.Property(t => t.CanvasHeight).HasColumnName("canvas_height").IsRequired();
        builder.Property(t => t.ThumbnailUrl).HasColumnName("thumbnail_url").HasMaxLength(ThumbnailUrlMaxLength);
        builder.Property(t => t.PromptHint).HasColumnName("prompt_hint").HasColumnType("nvarchar(max)");

        builder.Property(t => t.BackgroundPanelConfig)
            .HasColumnName("background_panel_config_json")
            .HasColumnType("nvarchar(max)")
            .HasConversion(JsonValueConverter.Create<BackgroundPanelConfig>())
            .Metadata.SetValueComparer(JsonValueConverter.CreateComparer<BackgroundPanelConfig>());

        builder.Property(t => t.TextSlots)
            .HasColumnName("text_slots_json")
            .HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<TextSlot>())
            .Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<TextSlot>());

        builder.Property(t => t.LogoSlots)
            .HasColumnName("logo_slots_json")
            .HasColumnType("nvarchar(max)")
            .HasConversion(JsonListValueConverter.Create<LogoSlot>())
            .Metadata.SetValueComparer(JsonListValueConverter.CreateComparer<LogoSlot>());

        builder.Property(t => t.Extras)
            .HasColumnName("extras_json")
            .HasColumnType("nvarchar(max)")
            .HasConversion(JsonValueConverter.Create<TemplateExtras>())
            .Metadata.SetValueComparer(JsonValueConverter.CreateComparer<TemplateExtras>());

        builder.HasIndex(t => t.Category).HasDatabaseName("ix_design_templates_category");
    }
}
