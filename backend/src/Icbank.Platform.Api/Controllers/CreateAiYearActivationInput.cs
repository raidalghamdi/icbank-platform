namespace Icbank.Platform.Api.Controllers;

/// <summary>The activation portion of the legacy create request.</summary>
/// <param name="Title">The activation title.</param>
/// <param name="Month">The calendar month (1-12).</param>
/// <param name="Year">The calendar year, defaults to 2026 if omitted.</param>
/// <param name="ActivationDate">The free-text activation date, if known.</param>
/// <param name="Type">The activation type.</param>
/// <param name="Channels">The distribution channels.</param>
/// <param name="Description">A free-text description, if any.</param>
/// <param name="Tags">The tag list.</param>
/// <param name="Status">The lifecycle status.</param>
/// <param name="Reach">The reach metric, if any.</param>
/// <param name="Engagement">The engagement metric, if any.</param>
/// <param name="Notes">Free-text notes, if any.</param>
public sealed record CreateAiYearActivationInput(
    string Title,
    int Month,
    int? Year,
    string? ActivationDate,
    string Type,
    IReadOnlyList<string> Channels,
    string? Description,
    IReadOnlyList<string>? Tags,
    string? Status,
    int? Reach,
    int? Engagement,
    string? Notes);
