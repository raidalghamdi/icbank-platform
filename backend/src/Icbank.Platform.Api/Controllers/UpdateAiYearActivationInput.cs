namespace Icbank.Platform.Api.Controllers;

/// <summary>The optional activation fields in the legacy update request.</summary>
/// <param name="Title">The new title, if changing.</param>
/// <param name="Month">The new month, if changing.</param>
/// <param name="ActivationDate">The new free-text activation date, if changing.</param>
/// <param name="Type">The new activation type, if changing.</param>
/// <param name="Description">The new description, if changing.</param>
/// <param name="Tags">The new tag list, if changing.</param>
/// <param name="Status">The new status, if changing.</param>
/// <param name="Reach">The new reach metric, if changing.</param>
/// <param name="Engagement">The new engagement metric, if changing.</param>
/// <param name="Notes">The new notes, if changing.</param>
/// <param name="Channels">The full replacement channel list, if changing.</param>
public sealed record UpdateAiYearActivationInput(
    string? Title,
    int? Month,
    string? ActivationDate,
    string? Type,
    string? Description,
    IReadOnlyList<string>? Tags,
    string? Status,
    int? Reach,
    int? Engagement,
    string? Notes,
    IReadOnlyList<string>? Channels);
