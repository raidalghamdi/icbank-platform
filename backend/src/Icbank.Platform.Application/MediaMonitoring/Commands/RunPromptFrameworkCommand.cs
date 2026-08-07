using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Executes a prompt framework with variable substitution (<c>POST /prompts/:id/run</c>). Closes
/// DEFECT-LOG.md SEC-02: the Node source ran this unauthenticated AI-cost endpoint with no
/// authentication at all; this port requires <c>media_monitoring:view</c> (running a prompt does
/// not mutate the shared library, so a view-tier grant is sufficient -- only the usage counter
/// increments).
/// </summary>
/// <param name="ActorUserId">The id of the authenticated caller running the prompt.</param>
/// <param name="FrameworkId">The framework id to run.</param>
/// <param name="Variables">The variable substitution map.</param>
public sealed record RunPromptFrameworkCommand(int ActorUserId, int FrameworkId, IReadOnlyDictionary<string, string> Variables)
    : IRequest<Result<RunPromptFrameworkResultDto>>;
