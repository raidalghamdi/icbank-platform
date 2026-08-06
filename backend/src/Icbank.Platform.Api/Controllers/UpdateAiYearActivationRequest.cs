using Icbank.Platform.Application.AiYear.Commands;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Request body for <see cref="AiYearController.UpdateActivationAsync"/>. The activation fields
/// remain nested to match the historical browser payload.
/// </summary>
/// <param name="Activation">The optional fields to change.</param>
/// <param name="Media">The full replacement media list, if changing.</param>
/// <param name="Metrics">The full replacement metric list, if changing.</param>
public sealed record UpdateAiYearActivationRequest(
    UpdateAiYearActivationInput? Activation,
    IReadOnlyList<CreateAiYearActivationMediaItem>? Media,
    IReadOnlyList<CreateAiYearActivationMetricItem>? Metrics);
