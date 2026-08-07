namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="DesignsController.GenerateBackgroundsAsync"/>.</summary>
/// <param name="Prompt">The base image prompt.</param>
/// <param name="TemplateId">The optional template id, used to derive the spatial-awareness hint.</param>
public sealed record GenerateBackgroundsRequest(string Prompt, int? TemplateId);
