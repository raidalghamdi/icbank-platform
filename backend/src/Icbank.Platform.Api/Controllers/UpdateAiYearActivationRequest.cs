using Icbank.Platform.Application.AiYear.Commands;

namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="AiYearController.UpdateActivationAsync"/>. Every field is optional (partial-update semantics).</summary>
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
/// <param name="Media">The full replacement media list, if changing.</param>
/// <param name="Metrics">The full replacement metrics list, if changing.</param>
public sealed record UpdateAiYearActivationRequest(
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
    IReadOnlyList<string>? Channels,
    IReadOnlyList<CreateAiYearActivationMediaItem>? Media,
    IReadOnlyList<CreateAiYearActivationMetricItem>? Metrics);
