namespace Icbank.Platform.Application.AiYear;

/// <summary>Ports a single row of <c>ai_year_activations</c> plus its media/metrics/channels (API-SURFACE.md §13).</summary>
/// <param name="Id">The activation id.</param>
/// <param name="Title">The activation title.</param>
/// <param name="Month">The calendar month (1-12).</param>
/// <param name="Year">The calendar year.</param>
/// <param name="ActivationDate">The free-text activation date, if known.</param>
/// <param name="Type">
/// The activation type. Free text -- DATA-MODEL.md AMBIGUOUS-4 notes no canonical value list
/// exists; kept as-is per task instruction, flagged for product sign-off (see WAVE2-PORT-NOTES.md).
/// </param>
/// <param name="Channels">The distribution channels.</param>
/// <param name="Description">A free-text description, if any.</param>
/// <param name="Tags">The tag list.</param>
/// <param name="Status">The lifecycle status.</param>
/// <param name="Reach">The reach metric, if recorded.</param>
/// <param name="Engagement">The engagement metric, if recorded.</param>
/// <param name="Notes">Free-text notes, if any.</param>
/// <param name="Media">The attached media.</param>
/// <param name="Metrics">The attached free-form metrics.</param>
public sealed record AiYearActivationDto(
    int Id,
    string Title,
    int Month,
    int Year,
    string? ActivationDate,
    string Type,
    IReadOnlyList<string> Channels,
    string? Description,
    IReadOnlyList<string> Tags,
    string Status,
    int? Reach,
    int? Engagement,
    string? Notes,
    IReadOnlyList<AiYearMediaDto> Media,
    IReadOnlyList<AiYearMetricDto> Metrics);
