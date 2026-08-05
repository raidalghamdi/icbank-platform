using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Designs;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>generated_designs</c> → <see cref="GeneratedDesign"/>.</summary>
/// <remarks>
/// <c>template_id</c> and <c>created_by</c> are re-pointed through the id-mapping store; a
/// source value with no corresponding migrated row is set to <see langword="null"/> (for
/// <c>template_id</c>, matching the source's own <c>onDelete: "set null"</c> behaviour; for
/// <c>created_by</c>, since it was an unenforced implied FK in the source and the port makes it
/// a real optional FK). <c>selected_logos</c> (jsonb number[] of implied <c>brand_logos</c> ids)
/// is re-pointed the same way, dropping unmapped ids rather than leaving them dangling.
/// </remarks>
public sealed class GeneratedDesignTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "generated_designs";

    /// <inheritdoc />
    public string DestinationTableName => "generated_designs";

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

            int? templateId = null;
            var sourceTemplateId = row.GetNullableInt32("template_id");
            if (sourceTemplateId.HasValue)
            {
                templateId = await context.IdMap.TryGetDestinationIdAsync("design_templates", sourceTemplateId.Value, cancellationToken);
            }

            int? createdByUserId = null;
            var sourceCreatedBy = row.GetNullableInt32("created_by");
            if (sourceCreatedBy.HasValue)
            {
                createdByUserId = await context.IdMap.TryGetDestinationIdAsync("users", sourceCreatedBy.Value, cancellationToken);
            }

            var selectedLogoIds = new List<int>();
            foreach (var sourceLogoId in row.GetInt32Array("selected_logos"))
            {
                var mappedLogoId = await context.IdMap.TryGetDestinationIdAsync("brand_logos", sourceLogoId, cancellationToken);
                if (mappedLogoId.HasValue)
                {
                    selectedLogoIds.Add(mappedLogoId.Value);
                }
            }

            var entity = new GeneratedDesign
            {
                TemplateId = templateId,
                TitleText = row.GetNullableString("title_text"),
                BodyText = row.GetNullableString("body_text"),
                BackgroundImageUrl = row.GetNullableString("background_image_url"),
                SelectedLogoIds = selectedLogoIds,
                FinalImageUrl = row.GetNullableString("final_image_url"),
                Department = row.GetNullableString("department"),
                CreatedByUserId = createdByUserId,
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.GeneratedDesigns.Add(entity);
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
        return await destination.GeneratedDesigns.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
