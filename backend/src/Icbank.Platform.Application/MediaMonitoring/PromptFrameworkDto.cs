namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Read model for a reusable AI prompt framework/template (<c>prompt_frameworks</c>).</summary>
/// <param name="Id">The framework id.</param>
/// <param name="NameAr">The Arabic name.</param>
/// <param name="NameEn">The optional English name.</param>
/// <param name="DescriptionAr">The optional Arabic description.</param>
/// <param name="Category">The prompt category.</param>
/// <param name="Kind">Whether this row is a framework or a template.</param>
/// <param name="PromptText">The prompt text, containing <c>{{variable}}</c> placeholders.</param>
/// <param name="Variables">The dynamic variable list.</param>
/// <param name="ExampleInput">An example input.</param>
/// <param name="ExampleOutput">An example output.</param>
/// <param name="Tags">The searchable tag list.</param>
/// <param name="RecommendedModel">The recommended AI model.</param>
/// <param name="IsApproved">Whether the framework is officially approved.</param>
/// <param name="UsageCount">The usage counter.</param>
/// <param name="Status">The lifecycle status.</param>
public sealed record PromptFrameworkDto(
    int Id,
    string NameAr,
    string? NameEn,
    string? DescriptionAr,
    string Category,
    string Kind,
    string PromptText,
    IReadOnlyList<PromptVariableDto> Variables,
    string? ExampleInput,
    string? ExampleOutput,
    IReadOnlyList<string> Tags,
    string? RecommendedModel,
    bool IsApproved,
    int UsageCount,
    string Status);
