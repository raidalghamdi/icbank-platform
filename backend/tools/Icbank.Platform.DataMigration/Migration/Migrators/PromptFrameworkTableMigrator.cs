using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.MediaMonitoring;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>prompt_frameworks</c> → <see cref="PromptFramework"/>.</summary>
public sealed class PromptFrameworkTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "prompt_frameworks";

    /// <inheritdoc />
    public string DestinationTableName => "prompt_frameworks";

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

            int? createdByUserId = null;
            var sourceCreatedBy = row.GetNullableInt32("created_by_user_id");
            if (sourceCreatedBy.HasValue)
            {
                createdByUserId = await context.IdMap.TryGetDestinationIdAsync("users", sourceCreatedBy.Value, cancellationToken);
            }

            var entity = new PromptFramework
            {
                NameAr = row.GetString("name_ar"),
                NameEn = row.GetNullableString("name_en"),
                DescriptionAr = row.GetNullableString("description_ar"),
                Category = SnakeCaseEnumParser.Parse<PromptFrameworkCategory>(
                    string.IsNullOrEmpty(row.GetNullableString("category")) ? "content-creation" : row.GetString("category")),
                Kind = SnakeCaseEnumParser.Parse<PromptFrameworkKind>(
                    string.IsNullOrEmpty(row.GetNullableString("kind")) ? "framework" : row.GetString("kind")),
                PromptText = row.GetString("prompt_text"),
                Variables = row.ReadObjectList<PromptVariable>("variables"),
                ExampleInput = row.GetNullableString("example_input"),
                ExampleOutput = row.GetNullableString("example_output"),
                Tags = row.GetStringArray("tags").ToList(),
                RecommendedModel = row.GetNullableString("recommended_model") ?? "gemini-2.5-flash",
                IsApproved = row.GetBoolean("is_approved"),
                UsageCount = row.GetNullableInt32("usage_count") ?? 0,
                CreatedByUserId = createdByUserId,
                CreatedByName = row.GetNullableString("created_by_name"),
                Status = SnakeCaseEnumParser.Parse<PromptFrameworkStatus>(
                    string.IsNullOrEmpty(row.GetNullableString("status")) ? "active" : row.GetString("status")),
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
                UpdatedAt = row.GetRawTimestamp("updated_at"),
            };

            destination.PromptFrameworks.Add(entity);
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
        return await destination.PromptFrameworks.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
