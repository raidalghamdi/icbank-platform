using Icbank.Platform.Application.AiYear.Commands;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Request body for <see cref="AiYearController.CreateActivationAsync"/>. The activation fields
/// deliberately remain nested to preserve the historical browser contract.
/// </summary>
/// <param name="Activation">The activation fields.</param>
/// <param name="Media">The media to attach.</param>
/// <param name="Metrics">The metrics to attach.</param>
public sealed record CreateAiYearActivationRequest(
    CreateAiYearActivationInput Activation,
    IReadOnlyList<CreateAiYearActivationMediaItem>? Media,
    IReadOnlyList<CreateAiYearActivationMetricItem>? Metrics);
