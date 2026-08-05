using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Designs;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>design_templates</c> → <see cref="DesignTemplate"/>.</summary>
/// <remarks>
/// The nested JSON shapes (<c>background_panel_config</c>, <c>text_slots</c>, <c>logo_slots</c>,
/// <c>extras</c>) are deserialized case-insensitively into the same typed C# shapes the
/// destination's EF <c>HasConversion</c> JSON columns expect
/// (<see cref="Icbank.Platform.Domain.Designs.BackgroundPanelConfig"/>,
/// <see cref="Icbank.Platform.Domain.Designs.TextSlot"/>,
/// <see cref="Icbank.Platform.Domain.Designs.LogoSlot"/>,
/// <see cref="Icbank.Platform.Domain.Designs.TemplateExtras"/>), since the source's camelCase
/// JSON field names map directly to the same-named PascalCase C# properties.
/// </remarks>
public sealed class DesignTemplateTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "design_templates";

    /// <inheritdoc />
    public string DestinationTableName => "design_templates";

    /// <inheritdoc />
    public async Task<TableMigrationResult> MigrateAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        var result = new TableMigrationResult { SourceTableName = SourceTableName };
        DateTimeOffset startedAt = context.DateTimeProvider.UtcNow;

        await using AppDbContext destination = context.CreateDestinationContext();

        await foreach (SourceRow row in context.Source.ReadTableAsync(SourceTableName, cancellationToken))
        {
            result.RowsRead++;
            var sourceId = row.GetInt32("id");

            var existingId = await context.IdMap.TryGetDestinationIdAsync(SourceTableName, sourceId, cancellationToken);
            if (existingId.HasValue)
            {
                result.RowsSkippedAlreadyMigrated++;
                continue;
            }

            DateTime createdAt = row.GetRawTimestamp("created_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;

            var entity = new DesignTemplate
            {
                TemplateNameAr = row.GetString("template_name_ar"),
                Category = row.GetString("category"),
                CanvasWidth = row.GetNullableInt32("canvas_width") ?? 1920,
                CanvasHeight = row.GetNullableInt32("canvas_height") ?? 1080,
                BackgroundPanelConfig = row.ReadObject<BackgroundPanelConfig>("background_panel_config"),
                TextSlots = row.ReadObjectList<TextSlot>("text_slots"),
                LogoSlots = row.ReadObjectList<LogoSlot>("logo_slots"),
                ThumbnailUrl = row.GetNullableString("thumbnail_url"),
                PromptHint = row.GetNullableString("prompt_hint"),
                Extras = row.ReadObject<TemplateExtras>("extras"),
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.DesignTemplates.Add(entity);
            await destination.SaveChangesAsync(cancellationToken);

            await context.IdMap.RecordAsync(SourceTableName, sourceId, entity.Id, context.DateTimeProvider.UtcNow, cancellationToken);
            result.RowsInserted++;
        }

        result.Duration = context.DateTimeProvider.UtcNow - startedAt;
        return result;
    }

    /// <inheritdoc />
    public async Task<long> CountDestinationRowsAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        await using AppDbContext destination = context.CreateDestinationContext();
        return await destination.DesignTemplates.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
