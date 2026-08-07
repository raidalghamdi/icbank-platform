using Icbank.Platform.Application.MediaMonitoring.Commands;

namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="MediaMonitoringController.UpdatePromptAsync"/>. Every field is an optional partial update.</summary>
/// <param name="NameAr">The new Arabic name, if changing.</param>
/// <param name="NameEn">The new English name, if changing.</param>
/// <param name="DescriptionAr">The new Arabic description, if changing.</param>
/// <param name="PromptText">The new prompt text, if changing.</param>
/// <param name="Variables">The new dynamic variable list, if changing.</param>
/// <param name="ExampleInput">The new example input, if changing.</param>
/// <param name="ExampleOutput">The new example output, if changing.</param>
/// <param name="Tags">The new tag list, if changing.</param>
/// <param name="IsApproved">The new approval flag, if changing.</param>
public sealed record UpdatePromptFrameworkRequest(
    string? NameAr,
    string? NameEn,
    string? DescriptionAr,
    string? PromptText,
    IReadOnlyList<PromptVariableItem>? Variables,
    string? ExampleInput,
    string? ExampleOutput,
    IReadOnlyList<string>? Tags,
    bool? IsApproved);
