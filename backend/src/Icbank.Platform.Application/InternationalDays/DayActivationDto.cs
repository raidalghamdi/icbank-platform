namespace Icbank.Platform.Application.InternationalDays;

/// <summary>Ports a single row of <c>day_activations</c> (API-SURFACE.md §14).</summary>
/// <param name="Id">The activation row id.</param>
/// <param name="Year">The campaign year, if known.</param>
/// <param name="EntityName">The entity that ran the activation.</param>
/// <param name="EntityType">The entity type, free text.</param>
/// <param name="ActivationType">The activation type, free text.</param>
/// <param name="Platform">The platform the activation ran on.</param>
/// <param name="Description">A free-text description.</param>
/// <param name="SourceUrl">The source URL used to verify the activation.</param>
/// <param name="Country">The country the activation took place in.</param>
/// <param name="Verified">Whether the activation was verified via a source URL.</param>
public sealed record DayActivationDto(
    int Id,
    int? Year,
    string? EntityName,
    string? EntityType,
    string? ActivationType,
    string? Platform,
    string? Description,
    string? SourceUrl,
    string? Country,
    bool Verified);
