using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Mapping.Transformers;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.AiYear;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>
/// Migrates <c>ai_year_activations</c> → <see cref="AiYearActivation"/>, fanning the source
/// native Postgres <c>channels text[]</c> array out into child <see cref="AiYearActivationChannel"/>
/// rows in the same write (AMBIGUOUS-2 — see <see cref="AiYearActivationTransformer"/>).
/// </summary>
public sealed class AiYearActivationTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "ai_year_activations";

    /// <inheritdoc />
    public string DestinationTableName => "ai_year_activations";

    /// <inheritdoc />
    public async Task<TableMigrationResult> MigrateAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        var result = new TableMigrationResult { SourceTableName = SourceTableName };
        DateTimeOffset startedAt = context.DateTimeProvider.UtcNow;

        await using AppDbContext destination = context.CreateDestinationContext();
        var totalChannelsCreated = 0;

        await foreach (SourceRow row in context.Source.ReadTableAsync(SourceTableName, cancellationToken))
        {
            result.RowsRead++;
            MappedAiYearActivation mapped = AiYearActivationTransformer.Transform(row);

            var existingId = await context.IdMap.TryGetDestinationIdAsync(SourceTableName, mapped.SourceId, cancellationToken);
            if (existingId.HasValue)
            {
                result.RowsSkippedAlreadyMigrated++;
                continue;
            }

            var entity = new AiYearActivation
            {
                Title = mapped.Title,
                Month = mapped.Month,
                Year = mapped.Year,
                ActivationDate = mapped.ActivationDate,
                Type = mapped.Type,
                Description = mapped.Description,
                Tags = mapped.Tags.ToList(),
                Status = Enum.Parse<AiYearActivationStatus>(mapped.Status, ignoreCase: true),
                Reach = mapped.Reach,
                Engagement = mapped.Engagement,
                Notes = mapped.Notes,
                CreatedAt = mapped.CreatedAtUtc,
                CreatedBy = "data-migration-tool",
            };

            foreach (var channel in mapped.Channels)
            {
                entity.Channels.Add(new AiYearActivationChannel
                {
                    Channel = channel,
                    CreatedAt = mapped.CreatedAtUtc,
                    CreatedBy = "data-migration-tool",
                });
                totalChannelsCreated++;
            }

            destination.AiYearActivations.Add(entity);
            await destination.SaveChangesAsync(cancellationToken);

            await context.IdMap.RecordAsync(SourceTableName, mapped.SourceId, entity.Id, context.DateTimeProvider.UtcNow, cancellationToken);
            result.RowsInserted++;
        }

        if (totalChannelsCreated > 0)
        {
            result.Notes.Add($"{totalChannelsCreated} ai_year_activation_channels rows created from source text[] arrays (AMBIGUOUS-2 fan-out).");
        }

        result.Duration = context.DateTimeProvider.UtcNow - startedAt;
        return result;
    }

    /// <inheritdoc />
    public async Task<long> CountDestinationRowsAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        await using AppDbContext destination = context.CreateDestinationContext();
        return await destination.AiYearActivations.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
