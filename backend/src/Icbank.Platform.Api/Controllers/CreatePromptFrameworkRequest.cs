using Icbank.Platform.Application.MediaMonitoring.Commands;

namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="MediaMonitoringController.CreatePromptAsync"/>.</summary>
/// <param name="NameAr">The Arabic name.</param>
/// <param name="NameEn">The optional English name.</param>
/// <param name="DescriptionAr">The optional Arabic description.</param>
/// <param name="Category">The prompt category key.</param>
/// <param name="Kind">Whether this row is a framework or a template.</param>
/// <param name="PromptText">The prompt text, containing <c>{{variable}}</c> placeholders.</param>
/// <param name="Variables">The dynamic variable list.</param>
/// <param name="ExampleInput">An example input.</param>
/// <param name="ExampleOutput">An example output.</param>
/// <param name="Tags">The searchable tag list.</param>
/// <param name="RecommendedModel">The recommended AI model.</param>
public sealed record CreatePromptFrameworkRequest(
    string NameAr,
    string? NameEn,
    string? DescriptionAr,
    string? Category,
    string? Kind,
    string PromptText,
    IReadOnlyList<PromptVariableItem>? Variables,
    string? ExampleInput,
    string? ExampleOutput,
    IReadOnlyList<string>? Tags,
    string? RecommendedModel);
