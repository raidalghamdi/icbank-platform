namespace Icbank.Platform.Application.InternationalDays;

/// <summary>One activation row rendered by <see cref="InternationalDayHtmlExportBuilder"/>.</summary>
/// <param name="EntityName">The entity that ran the activation.</param>
/// <param name="EntityType">The entity type.</param>
/// <param name="ActivationType">The activation type.</param>
/// <param name="Description">A free-text description.</param>
/// <param name="Country">The country the activation took place in.</param>
/// <param name="Year">The activation year, if known.</param>
/// <param name="SourceUrl">The source URL, if any.</param>
/// <param name="Verified">Whether the activation was verified via a source URL.</param>
public sealed record InternationalDayExportActivation(
    string? EntityName, string? EntityType, string? ActivationType, string? Description, string? Country, int? Year, string? SourceUrl, bool Verified);
