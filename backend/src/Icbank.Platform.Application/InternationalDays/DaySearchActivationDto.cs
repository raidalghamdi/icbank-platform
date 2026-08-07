namespace Icbank.Platform.Application.InternationalDays;

/// <summary>
/// One AI-returned activation entry from the search prompt's <c>activations</c> array
/// (BUSINESS-RULES.md §4.2). Closes DEFECT-LOG.md DATA-04/H-2: this typed shape is what the AI
/// provider's JSON response is deserialized into and run through FluentValidation before any
/// value from it is persisted -- the Node source trusted the parsed JSON directly.
/// </summary>
/// <param name="EntityName">The Saudi entity name.</param>
/// <param name="EntityType">The entity type, free text (government/private).</param>
/// <param name="ActivationType">The activation type, free text (campaign/event/post/infographic).</param>
/// <param name="Platform">The platform the activation ran on.</param>
/// <param name="Description">A short description of the activation.</param>
/// <param name="SourceUrl">A direct source URL, or <c>null</c>.</param>
/// <param name="Country">The country, used for design samples; usually Saudi Arabia for activations.</param>
/// <param name="Year">The activation year.</param>
public sealed record DaySearchActivationDto(
    string? EntityName,
    string? EntityType,
    string? ActivationType,
    string? Platform,
    string? Description,
    string? SourceUrl,
    string? Country,
    int? Year);
