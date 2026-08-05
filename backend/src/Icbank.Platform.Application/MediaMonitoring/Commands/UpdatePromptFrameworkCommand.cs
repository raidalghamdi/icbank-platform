using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Updates a prompt framework's fields (<c>PUT /prompts/:id</c>). Closes DEFECT-LOG.md SEC-02:
/// the Node source allowed anonymous edits; this port requires <c>media_monitoring:edit</c>.
/// All fields are optional partial updates, matching the Node source's partial-update semantics.
/// </summary>
/// <param name="ActorUserId">The id of the authenticated caller performing the update.</param>
/// <param name="FrameworkId">The framework id to update.</param>
/// <param name="NameAr">The new Arabic name, if changing.</param>
/// <param name="NameEn">The new English name, if changing.</param>
/// <param name="DescriptionAr">The new Arabic description, if changing.</param>
/// <param name="PromptText">The new prompt text, if changing.</param>
/// <param name="Variables">The new dynamic variable list, if changing.</param>
/// <param name="ExampleInput">The new example input, if changing.</param>
/// <param name="ExampleOutput">The new example output, if changing.</param>
/// <param name="Tags">The new tag list, if changing.</param>
/// <param name="IsApproved">The new approval flag, if changing.</param>
public sealed record UpdatePromptFrameworkCommand(
    int ActorUserId,
    int FrameworkId,
    string? NameAr,
    string? NameEn,
    string? DescriptionAr,
    string? PromptText,
    IReadOnlyList<PromptVariableItem>? Variables,
    string? ExampleInput,
    string? ExampleOutput,
    IReadOnlyList<string>? Tags,
    bool? IsApproved) : IRequest<Result<PromptFrameworkDto>>;
