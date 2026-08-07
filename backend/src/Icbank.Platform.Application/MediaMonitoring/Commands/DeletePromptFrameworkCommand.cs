using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Deletes a prompt framework (<c>DELETE /prompts/:id</c>).</summary>
/// <param name="ActorUserId">The id of the authenticated caller performing the deletion.</param>
/// <param name="FrameworkId">The framework id to delete.</param>
public sealed record DeletePromptFrameworkCommand(int ActorUserId, int FrameworkId) : IRequest<Result<bool>>;
