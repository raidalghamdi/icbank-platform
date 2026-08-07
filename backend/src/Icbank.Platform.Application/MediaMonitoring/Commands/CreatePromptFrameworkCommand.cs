using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Creates a prompt framework (<c>POST /prompts</c>). Closes DEFECT-LOG.md SEC-02: the Node
/// source allowed anyone to poison the shared prompt library with no authentication at all; this
/// port requires <c>media_monitoring:create</c>.
/// </summary>
/// <param name="ActorUserId">The id of the authenticated caller creating the framework.</param>
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
public sealed record CreatePromptFrameworkCommand(
    int ActorUserId,
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
    string? RecommendedModel) : IRequest<Result<PromptFrameworkDto>>;
