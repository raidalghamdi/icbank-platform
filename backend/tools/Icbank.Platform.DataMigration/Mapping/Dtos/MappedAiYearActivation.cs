namespace Icbank.Platform.DataMigration.Mapping.Dtos;

/// <summary>Pure DTO produced by <see cref="Transformers.AiYearActivationTransformer"/>.</summary>
/// <param name="SourceId">The source Postgres <c>ai_year_activations.id</c>.</param>
/// <param name="Title">The activation title.</param>
/// <param name="Month">The calendar month (1-12).</param>
/// <param name="Year">The calendar year.</param>
/// <param name="ActivationDate">The free-text activation date as captured by source.</param>
/// <param name="Type">The free-text activation type (AMBIGUOUS-4: no fixed enum).</param>
/// <param name="Description">The optional description.</param>
/// <param name="Tags">The tag list (was <c>jsonb string[]</c>).</param>
/// <param name="Status">The publication status.</param>
/// <param name="Reach">The optional reach metric.</param>
/// <param name="Engagement">The optional engagement metric.</param>
/// <param name="Notes">Free-text notes.</param>
/// <param name="Channels">
/// Every element of the source native Postgres <c>text[]</c> array, fanned out into one child
/// <c>AiYearActivationChannel</c> row per element (AMBIGUOUS-2 decision — see
/// <see cref="Transformers.AiYearActivationTransformer"/>).
/// </param>
/// <param name="CreatedAtUtc">The original row-creation instant.</param>
public sealed record MappedAiYearActivation(
    int SourceId,
    string Title,
    int Month,
    int Year,
    string? ActivationDate,
    string Type,
    string? Description,
    IReadOnlyList<string> Tags,
    string Status,
    int? Reach,
    int? Engagement,
    string? Notes,
    IReadOnlyList<string> Channels,
    DateTime CreatedAtUtc);
